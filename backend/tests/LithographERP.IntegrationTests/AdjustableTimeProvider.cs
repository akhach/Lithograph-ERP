namespace LithographERP.IntegrationTests;

public sealed class AdjustableTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = LithographApiFactory.DefaultTestTime;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
