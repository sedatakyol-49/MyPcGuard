namespace MyPcGuard.Models;

public sealed record NetworkConnectionItem
{
    public string Protocol { get; init; } = string.Empty;
    public string LocalEndpoint { get; init; } = string.Empty;
    public string RemoteEndpoint { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}
