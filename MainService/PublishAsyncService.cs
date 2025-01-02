using MassTransit;
using Shared;

namespace MainService;

public class PublishAsyncService : BackgroundService
{
    private readonly ILogger<PublishBatchService> _logger;
    private readonly IBusControl _busControl;

    public PublishAsyncService(ILogger<PublishBatchService> logger,
        IBusControl busControl)
    {
        _logger = logger;
        _busControl = busControl;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxMessages = 100;
        const int numberOfPartition = 5;

        var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };
        await Parallel.ForEachAsync(Enumerable.Range(0, maxMessages), options, async (i, token) =>
        {
            var partitionKey = Random.Shared.Next(1, numberOfPartition);
            await _busControl.Publish(
                new TestMessage($"Hello, World! {i}", partitionKey.ToString()), stoppingToken);
        });
    }
}