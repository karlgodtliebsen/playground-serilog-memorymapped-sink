using System.Buffers;

namespace Serilog.MemoryMapped.Queue;

sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[] _buffer;
    private int _written;

    public PooledBufferWriter(int initialCapacity = 16 * 1024)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _written = 0;
    }

    public void Advance(int count) => _written += count;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _buffer.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _buffer.AsSpan(_written);
    }

    private void Ensure(int sizeHint)
    {
        if (sizeHint < 1) sizeHint = 1;
        if (_written + sizeHint <= _buffer.Length) return;
        var newBuf = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, _written + sizeHint));
        Buffer.BlockCopy(_buffer, 0, newBuf, 0, _written);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuf;
    }

    public ReadOnlySpan<byte> WrittenSpan => new(_buffer, 0, _written);

    public void Dispose()
    {
        var buf = _buffer;
        _buffer = Array.Empty<byte>();
        ArrayPool<byte>.Shared.Return(buf);
    }
}