using MassTransit;
using Shared;

namespace MainService;

public class MainServiceTestMessageConsumer: IConsumer<TestMessage>
{
    public Task Consume(ConsumeContext<TestMessage> context)
    {
        Console.WriteLine($"Service A: {context.Message.Value}");
        return Task.CompletedTask;
    }
}