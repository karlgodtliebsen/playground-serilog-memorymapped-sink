# playground-serilog-memorymapped-sink


## Serilog Sink
- A Serilog Sink that uses a Memory Mapped File to offload LogEvents, for fast log production, cross OS compatible.
- Prevents losing Log Entries due to the usage of Named Memory Mapped File that will be processed again after a process crash
- The systems shows how to use IHost based background services to start one or more services to Consume the LogEntries and forward these to Repositories. At the moment only RDBMS - MSSql, PostgreSql and SqLite, is implemented, but it can easily be expanded to other systems as well

### Details
- A simple Serilog sink to serialize a Serilog.LogEvent into an object that is serialized and offloaded into a Memory Mapped File for persistent buffering, and then forwarded (to RDBMS or other style stores)
- Handling of the LogEvent is done in a structured manner, using a serialization friendly wrapper (LogEventWrapper), and using CompactJsonFormatter for the render message, while also apply Json serialization for the Properties collection, and including the Message Template Text as string for full fidelity
- This LogEventWrapper data is serialized into a byte array (readonly span) for the Memory Mapped File, using MemoryPack for fast serialization (an option for JSon serialization is available). 
- The Memory Mapped File ensures that crashes does not lead to loss of log entries, as the file i persisted and available to pick up after restart.
- The systems uses Serilog in the technical parts, it is after all based on a Serilog Sink, but uses Microsoft ILogger as basics in the Log Event Producer part combined with the Serilog Sink, (ie the code that emit log events for the Sink uses regular Microsoft ILogger).
- The various IHost configurators in the Console Host shows how to configure the Microsoft ILogger and Serilog on top of that, also setting the application wide Log.Logger to this logger instance, but builds other Serilog log instances using configuration settings, with the Serilog ILogger injected in IServiceCollection for the technical parts hosted in each background service.

When using PostgreSql, data is currently stored in TEXT fields, however it is possible to use JSONB fields and Search indexing and optimization can be applied.


### The system consists of several bricks:
  
  - Serilog.MemoryMapped.Queue
  - Serilog.MemoryMapped.Sink
  - Serilog.MemoryMapped.Sink.Forwarder
  - Serilog.MemoryMapped.Repository.MsSql
  - Serilog.MemoryMapped.Repository.SqLite
  - Serilog.MemoryMapped.Repository.PostgreSql

  - Serilog.MemoryMapped.Sink.Console

  - Serilog.MemoryMapped.Sink.Tests

- The Console project show how to wire it all of up using IHost for Log Producer (simulating an application that emits logs using Serilog/Microsoft ILogger), a Forwarder and a Queue Monitor running simultaneously.


### AI Assistance - Memory Mapped Queue Buffer

Thanks to Claude and ChatGpt and the ususal guided dialogues, which shows what a combination of AI and a skilled Human can achieve:

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

