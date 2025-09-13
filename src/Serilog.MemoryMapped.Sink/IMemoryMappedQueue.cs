namespace Serilog.MemoryMapped.Sink;

public interface IMemoryMappedQueue<T> : IDisposable where T : class
{
    bool TryEnqueue(T entry);
    T? TryDequeue();
    IList<T> TryDequeueBatch(int maxCount = 100);
}


public interface IMemoryMappedQueue : IMemoryMappedQueue<LogEventWrapper>
{

}
