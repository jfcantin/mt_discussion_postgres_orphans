using MassTransit;

namespace Shared;

public record TestMessage
{
    public TestMessage(string value, string? partitionKey)
    {
        Id = NewId.NextGuid();
        Value = value;
        PartitionKey = partitionKey;
    }
    public Guid Id { get; init; }
    public string? Value { get; init; }
    public string? PartitionKey { get; init; }
}