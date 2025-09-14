using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace Serilog.MemoryMapped;


// ANALYSIS: Your MemoryMappedQueueBuffer vs Basic Implementation
// Your implementation is SIGNIFICANTLY more sophisticated and robust

/*
Thanks to Claude and ChatGpt:

Your code is significantly more sophisticated than my basic example. This is production-ready, high-performance logging infrastructure!

=== KEY ADVANTAGES OF YOUR IMPLEMENTATION ===

1. CIRCULAR BUFFER DESIGN:
   - Efficiently reuses space as messages are consumed
   - Prevents buffer from filling up permanently
   - My basic version was append-only (terrible for long-running apps)

2. PROPER WRAPPING LOGIC:
   - Handles buffer wrap-around with WrapMarker system
   - Manages contiguous space requirements intelligently
   - Prevents message fragmentation across buffer boundary

3. THREAD SAFETY:
   - Uses named Mutex for cross-process synchronization
   - Handles AbandonedMutexException (critical for crash recovery)
   - My version had basic locks but no cross-process safety

4. SPACE MANAGEMENT:
   - Dynamic space calculation with CalculateAvailableSpace()
   - Prevents buffer overflow with proper space checks
   - Handles edge cases like exact capacity alignment

5. ROBUSTNESS:
   - Graceful handling of insufficient space
   - Corruption detection and recovery
   - Timeout-based mutex acquisition

6. PERFORMANCE OPTIMIZATIONS:
   - Chunked writing for large payloads (64KB chunks)
   - ArrayPool usage to avoid allocations
   - ReadOnlySpan<byte> overload for zero-copy scenarios
   - Batch dequeue operations
*/


/// <summary>
/// Ring buffer over a memory-mapped file with wrap marker.
/// Header (32 bytes):
///   [0..7]   long writePos
///   [8..15]  long readPos
///   [16..23] long messageCount
///   [24..27] int  headerSize
///   [28..31] int  capacityBytes
/// Data region: [headerSize .. capacity)
/// Each record: [UInt16 length][payload...], or [UInt16 0xFFFF] = wrap marker
/// </summary>
public class MemoryMappedQueueBuffer
{
    private readonly MemoryMappedFile memoryMappedFile;
    private readonly MemoryMappedViewAccessor accessor;
    private readonly string name;
    private readonly string mutexName;
    private readonly int headerSize = 32;
    private readonly int capacity; // total bytes of the mapping
    private readonly Mutex mutex;

    private const ushort WrapMarker = ushort.MaxValue;

    public MemoryMappedQueueBuffer(string rawName)
    {

        this.name = NameNormalizer.Normalize(rawName, "mmf_");
        this.mutexName = NameNormalizer.Normalize(rawName, "mm_queue_");


        var capacityMB = 50;             // default size when creating new

        try
        {
            // Open existing map and discover actual capacity
            MemoryMapperLogger.Write("MemoryMappedQueueBuffer MemoryMappedFile.OpenExisting {0}", name);
            memoryMappedFile = MemoryMappedFile.OpenExisting(name);
            accessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
            capacity = checked((int)accessor.Capacity);

            // Optional: validate header to ensure compatibility
            var storedHeaderSize = accessor.ReadInt32(24);
            var storedCapacity = accessor.ReadInt32(28);
            if (storedHeaderSize != headerSize || storedCapacity != capacity)
                throw new InvalidDataException("MMF header does not match expected layout.");
        }
        catch (FileNotFoundException)
        {
            // Create new map with configured capacity
            MemoryMapperLogger.Write("MemoryMappedQueueBuffer MemoryMappedFile.CreateNew {0}", name);
            capacity = capacityMB * 1024 * 1024;
            memoryMappedFile = MemoryMappedFile.CreateNew(name, capacity, MemoryMappedFileAccess.ReadWrite);
            accessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
            InitializeHeader();
        }

        // Reuse a single named mutex
        mutex = new Mutex(false, mutexName);
    }

    private void InitializeHeader()
    {
        MemoryMapperLogger.Write("MemoryMappedQueueBuffer InitializeHeader");

        accessor.Write(0, (long)headerSize); // writePos starts after header
        accessor.Write(8, (long)headerSize); // readPos starts after header
        accessor.Write(16, 0L);               // messageCount = 0
        accessor.Write(24, headerSize);       // persist header size
        accessor.Write(28, capacity);         // persist capacity
        accessor.Flush();
    }

    public bool TryEnqueue(byte[] messageBytes)
    {
        MemoryMapperLogger.Write("MemoryMappedQueueBuffer TryEnqueue byte[]");

        if (!TryAcquireMutex(TimeSpan.FromSeconds(1))) return false;

        try
        {
            if (messageBytes.Length > ushort.MaxValue) return false; // length prefix is UInt16

            // Read header
            var writePos = accessor.ReadInt64(0);
            var readPos = accessor.ReadInt64(8);
            var count = accessor.ReadInt64(16);

            var spaceNeeded = 2 + messageBytes.Length; // length prefix + payload
            var totalAvail = CalculateAvailableSpace(writePos, readPos, count);
            if (spaceNeeded > totalAvail) return false;

            // Determine if we must wrap and whether we can place a wrap marker
            var tailSpace = writePos >= readPos ? capacity - writePos : (readPos - writePos);
            var needWrap = spaceNeeded > tailSpace && writePos >= readPos; // only tail segment can wrap
            var canWriteWrapMarker = needWrap && (capacity - writePos) >= 2;

            // If we plan to write a wrap marker, ensure total availability covers it too
            if (canWriteWrapMarker && (spaceNeeded + 2) > totalAvail) return false;

            long actualWritePos = writePos;

            if (needWrap)
            {
                // Emit wrap marker if we have >=2 bytes in the tail; otherwise reader will pre-wrap on read
                if (canWriteWrapMarker)
                {
                    accessor.Write(writePos, WrapMarker);
                    writePos += 2;
                    if (writePos >= capacity) writePos = headerSize; // in case exactly aligned
                }

                // Jump to head for the actual record
                actualWritePos = headerSize;
            }

            // Final contiguous space check at the chosen write position
            var contiguousSpace = actualWritePos >= readPos
                ? capacity - actualWritePos
                : (readPos - actualWritePos);

            if (spaceNeeded > contiguousSpace)
                return false; // would require a split write, which we do not support

            // Write record: [len][payload]
            accessor.Write(actualWritePos, (ushort)messageBytes.Length);
            accessor.WriteArray(actualWritePos + 2, messageBytes, 0, messageBytes.Length);

            // Advance write pointer
            var newWritePos = actualWritePos + spaceNeeded;
            if (newWritePos >= capacity) newWritePos = headerSize;

            accessor.Write(0, newWritePos);    // writePos
            accessor.Write(16, count + 1);     // messageCount
            accessor.Flush();
            return true;
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public bool TryEnqueue(ReadOnlySpan<byte> payload)
    {
        MemoryMapperLogger.Write("MemoryMappedQueueBuffer TryEnqueue ReadOnlySpan<byte>");

        if (!TryAcquireMutex(TimeSpan.FromSeconds(1))) return false;
        try
        {
            if (payload.Length > ushort.MaxValue) return false;

            var writePos = accessor.ReadInt64(0);
            var readPos = accessor.ReadInt64(8);
            var count = accessor.ReadInt64(16);

            var spaceNeeded = 2 + payload.Length;
            var totalAvail = CalculateAvailableSpace(writePos, readPos, count);
            if (spaceNeeded > totalAvail) return false;

            var tailSpace = writePos >= readPos ? capacity - writePos : (readPos - writePos);
            var needWrap = spaceNeeded > tailSpace && writePos >= readPos;
            var canWriteWrapMarker = needWrap && (capacity - writePos) >= 2;
            if (canWriteWrapMarker && (spaceNeeded + 2) > totalAvail) return false;

            long actualWritePos = writePos;
            if (needWrap)
            {
                if (canWriteWrapMarker)
                {
                    accessor.Write(writePos, WrapMarker);
                    writePos += 2;
                    if (writePos >= capacity) writePos = headerSize;
                }
                actualWritePos = headerSize;
            }

            var contiguousSpace = actualWritePos >= readPos ? capacity - actualWritePos : (readPos - actualWritePos);
            if (spaceNeeded > contiguousSpace) return false;

            accessor.Write(actualWritePos, (ushort)payload.Length);

            // Write span in chunks to avoid a temporary array:
            const int Chunk = 64 * 1024;
            int remaining = payload.Length, offset = 0;
            while (remaining > 0)
            {
                int toWrite = Math.Min(remaining, Chunk);
                // Unfortunately WriteArray requires T[]; do a small pooled hop:
                var tmp = ArrayPool<byte>.Shared.Rent(toWrite);
                payload.Slice(offset, toWrite).CopyTo(tmp);
                accessor.WriteArray(actualWritePos + 2 + offset, tmp, 0, toWrite);
                ArrayPool<byte>.Shared.Return(tmp);
                offset += toWrite;
                remaining -= toWrite;
            }

            var newWritePos = actualWritePos + spaceNeeded;
            if (newWritePos >= capacity) newWritePos = headerSize;

            accessor.Write(0, newWritePos);
            accessor.Write(16, count + 1);
            accessor.Flush();
            return true;
        }
        finally { ReleaseMutex(); }
    }


    public byte[] TryDequeue()
    {
        MemoryMapperLogger.Write("MemoryMappedQueueBuffer TryDequeueBatch");

        if (!TryAcquireMutex(TimeSpan.FromSeconds(1))) return [];

        try
        {
            var writePos = accessor.ReadInt64(0);
            var readPos = accessor.ReadInt64(8);
            var count = accessor.ReadInt64(16);

            if (count == 0) return []; // empty

            var actualReadPos = readPos;

            // If not enough bytes to read a length, wrap to head
            if (capacity - actualReadPos < 2)
                actualReadPos = headerSize;

            // Peek length
            var lenOrMarker = accessor.ReadUInt16(actualReadPos);

            // If we see the wrap marker, jump to head and read real length
            if (lenOrMarker == WrapMarker)
            {
                actualReadPos = headerSize;

                // After jumping, ensure we can read a length
                if (capacity - actualReadPos < 2)
                    return []; // corrupted; no room even for a length

                lenOrMarker = accessor.ReadUInt16(actualReadPos);
            }

            var messageLength = lenOrMarker;

            // If payload does not fit to the end, this means writer wrapped but didn't write marker,
            // so we also wrap here and re-read length.
            if (capacity - (actualReadPos + 2) < messageLength)
            {
                actualReadPos = headerSize;

                if (capacity - actualReadPos < 2)
                    return []; // corrupted

                messageLength = accessor.ReadUInt16(actualReadPos);
            }

            var messageBytes = new byte[messageLength];
            accessor.ReadArray(actualReadPos + 2, messageBytes, 0, messageLength);

            // Advance read pointer
            var newReadPos = actualReadPos + 2 + messageLength;
            if (newReadPos >= capacity) newReadPos = headerSize;

            accessor.Write(8, newReadPos);   // readPos
            accessor.Write(16, count - 1);   // messageCount
            accessor.Flush();

            return messageBytes;
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public IList<byte[]> TryDequeueBatch(int maxCount = 100)
    {
        MemoryMapperLogger.Write("MemoryMappedQueueBuffer TryDequeueBatch");
        var results = new List<byte[]>(Math.Max(0, maxCount));
        for (var i = 0; i < maxCount; i++)
        {
            var entry = TryDequeue();
            if (entry == Array.Empty<byte>()) break;
            results.Add(entry);
        }
        return results;
    }

    public MemoryMappedQueueStats GetStats()
    {

        if (!TryAcquireMutex(TimeSpan.FromMilliseconds(100)))
            return new MemoryMappedQueueStats { Available = false };

        try
        {
            var writePos = accessor.ReadInt64(0);
            var readPos = accessor.ReadInt64(8);
            var count = accessor.ReadInt64(16);
            var availableSpace = CalculateAvailableSpace(writePos, readPos, count);

            return new MemoryMappedQueueStats
            {
                Available = true,
                MessageCount = count,
                AvailableSpace = availableSpace,
                CapacityMB = capacity / (1024 * 1024)
            };
        }
        finally
        {
            ReleaseMutex();
        }
    }

    private long CalculateAvailableSpace(long writePos, long readPos, long count)
    {
        var dataSize = (long)capacity - headerSize;

        if (writePos == readPos)
            return count == 0 ? dataSize : 0;

        return writePos > readPos
            ? dataSize - (writePos - readPos)
            : (readPos - writePos);
    }

    private bool TryAcquireMutex(TimeSpan timeout)
    {
        try
        {
            return mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            // We now own the mutex; proceed but consider logging
            return true;
        }
    }

    private void ReleaseMutex()
    {
        try { mutex.ReleaseMutex(); } catch { /* ignore */ }
    }

    public void Dispose()
    {
        try { mutex?.Dispose(); } catch { /* ignore */ }
        accessor?.Dispose();
        memoryMappedFile?.Dispose();
    }
}