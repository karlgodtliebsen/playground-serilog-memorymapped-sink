namespace Serilog.MemoryMapped;

public static class MemoryMapperLogger
{
    static Action<string>? action;


    public static void Enable(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        Enable(m =>
        {
            output.WriteLine(m);
            output.Flush();
        });
    }
    public static void Enable(Action<string> output)
    {
        ArgumentNullException.ThrowIfNull(output);
        action = output;
    }

    public static void Disable()
    {
        action = null;
    }

    public static void Write(string format, object? arg0 = null, object? arg1 = null, object? arg2 = null)
    {
        var o = action;
        o?.Invoke(string.Format($"{DateTime.UtcNow:o} {format}", arg0, arg1, arg2));
    }
}