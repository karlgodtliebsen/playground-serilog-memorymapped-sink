using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Serilog.Events;
using Serilog.MemoryMapped.Sink.Configuration;
using Serilog.Parsing;

namespace Serilog.MemoryMapped.Sink.Tests;

public class TestOfMemoryMapper
{
    [Fact]
    public void VerifyMemoryMapperConfiguration()
    {

        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices("the name");

        var serviceProvider = services.BuildServiceProvider();

        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();
    }

    [Fact]
    public void VerifyMemoryMapperEnqueueAndDequeue()
    {

        IServiceCollection services = new ServiceCollection();
        services.AddMemoryMappedServices("the name");

        var serviceProvider = services.BuildServiceProvider();

        var memoryMappedQueue = serviceProvider.GetRequiredService<IMemoryMappedQueue>();
        memoryMappedQueue.Should().NotBeNull();

        var messageTemplateTokens = new List<MessageTemplateToken>
        {
            new PropertyToken( "UserId","RawText"),
            new TextToken( "Hello " ),
            new PropertyToken( "UserName","RawText"),
            new TextToken( "! " ),
        };


        var logEventProperties = new List<LogEventProperty>()
        {
            new LogEventProperty("the name",new ScalarValue(42))
        };
        var @event = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Debug, new Exception("exception message"),
           new MessageTemplate("the message template {t0} {t1} {t2} {t3} ", messageTemplateTokens), logEventProperties);

        memoryMappedQueue.TryEnqueue(new(@event)).Should().BeTrue();

        var @mappedEvent = memoryMappedQueue.TryDequeue();
        @mappedEvent.Should().NotBeNull();

        var logEvent = @mappedEvent!.CreateLogEvent();
    }

}
