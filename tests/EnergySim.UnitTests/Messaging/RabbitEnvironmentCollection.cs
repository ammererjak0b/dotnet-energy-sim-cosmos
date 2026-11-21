namespace EnergySim.UnitTests.Messaging;

/// <summary>Tests that set RABBITMQ_USER / RABBITMQ_PASSWORD run one after another (process environment is shared).</summary>
[CollectionDefinition(NAME, DisableParallelization = true)]
public sealed class RabbitEnvironmentCollection
{
    public const string NAME = "RabbitEnvironment";
}
