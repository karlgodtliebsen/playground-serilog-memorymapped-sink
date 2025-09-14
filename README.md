# playground-serilog-memorymapped-sink


## Content
- A Serilog Sink that uses a memory Mapped File to offload LogEvents, for fast log production
- Prevnets loosing Log Entries due to a names memory Mapped File that will be processed again after a process crash
- Host Setup to start  or more background Services to Consume the LogEntries and forward these to Repositories (at the moment RDBMS - MSSql, PostgreSql and SqLite) but cna be expanded to other systems as well

- The system consists of several bricks:
  - Serilog.MemoryMapped
  - Serilog.MemoryMapped.Sink
  - Serilog.MemoryMapped.Sink.Forwarder
  - Serilog.MemoryMapped.Repository.MsSql
  - Serilog.MemoryMapped.Repository.SqLite
  - Serilog.MemoryMapped.Repository.PostgreSql

  - Serilog.MemoryMapped.Sink.Tests


- The Test project show how to use the bricks combined in different ways

### Serilog Sink
> A simple albeit enough Serilog sink to serialize a Serilog.LogEvent into something that can be forwarded in a strucutred manner, using CompactJsonFormatter for the render message,
> while also providing Properties and the Message Template for full fidelity

It should be a minor task to use either Protobuf or MessagePack for fast serialization

When using PostgreSql, data is stored in TEXT fields, however JSONB and Search indexing and optimization should be applied.



### Memory Mapped Queue Buffer


Thanks to Claude and ChatGpt:

ANALYSIS: 
> Your MemoryMappedQueueBuffer vs Basic Implementation
> 
> Your implementation is SIGNIFICANTLY more sophisticated and robust
> 
> Your code is significantly more sophisticated than my basic example. This is production-ready, high-performance logging infrastructure!


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




### Memory Mapped Queue Monitor

> Enhanced Memory Mapped Queue Monitor with comprehensive monitoring capabilities

=== WHAT THE MONITOR PROVIDES ===

1. REAL-TIME METRICS:
   - Buffer usage percentage
   - Message count and throughput
   - Available space tracking
   - Growth rate analysis

2. INTELLIGENT ALERTING:
   - High/critical usage warnings
   - Message backlog alerts
   - Low throughput detection (consumer issues)
   - Rapid growth rate alerts

3. HEALTH REPORTING:
   - Historical metrics tracking
   - Health status determination
   - Performance summaries
   - Integration with ASP.NET Core health checks

4. OPERATIONAL INSIGHTS:
   - Identifies when consumers are falling behind
   - Detects buffer capacity issues before they become critical
   - Provides data for capacity planning
   - Helps troubleshoot production issues

5. INTEGRATION READY:
   - Works with logging frameworks
   - Supports health check endpoints
   - Can trigger automated responses
   - Extensible for custom notifications

This monitoring is ESSENTIAL for production logging systems using memory-mapped buffers!

