using MassTransit;
using Shared;

namespace MainService;

public class PublishSynchronousService : BackgroundService
{
    private readonly ILogger<PublishBatchService> _logger;
    private readonly IBusControl _busControl;

    public PublishSynchronousService(ILogger<PublishBatchService> logger, IBusControl busControl)
    {
        _logger = logger;
        _busControl = busControl;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxMessages = 100;
        const int numberOfPartition = 5; 
        
        foreach (var i in Enumerable.Range(0, maxMessages))
        {
            var partitionKey = Random.Shared.Next(1, numberOfPartition);
            await _busControl.Publish(new TestMessage($"Hello, World! {i}", partitionKey.ToString()), stoppingToken);
        }
    }
}