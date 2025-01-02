using MassTransit;
using Shared;

namespace MainService;

public class MainServiceSecondTestMessageConsumer: IConsumer<TestMessage>
{
    public Task Consume(ConsumeContext<TestMessage> context)
    {
        Console.WriteLine($"Service A second: {context.Message.Value}");
        return Task.CompletedTask;
    }
}