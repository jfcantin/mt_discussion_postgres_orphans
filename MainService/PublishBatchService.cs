using MassTransit;
using Shared;

namespace MainService;

public class PublishBatchService : BackgroundService
{
    private readonly ILogger<PublishBatchService> _logger;
    private readonly IBusControl _busControl;

    public PublishBatchService(ILogger<PublishBatchService> logger, IBusControl busControl)
    {
        _logger = logger;
        _busControl = busControl;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxMessages = 1000;
        const int numberOfPartition = 5; // even setting this to 1000 still works fine
        
        var messages = new List<TestMessage>();
        foreach (var i in Enumerable.Range(0, maxMessages))
        {
            var partitionKey = Random.Shared.Next(1, numberOfPartition);
            messages.Add(new TestMessage($"Hello, World! {i}", partitionKey.ToString()));
        }

        await _busControl.PublishBatch(messages, stoppingToken);
    }
}