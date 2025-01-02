using MassTransit;
using Shared;

namespace SecondService;

public class SecondServiceTestMessageConsumer: IConsumer<TestMessage>
{
    public Task Consume(ConsumeContext<TestMessage> context)
    {
        Console.WriteLine($"Service B: {context.Message.Value}");
        return Task.CompletedTask;
    }
}