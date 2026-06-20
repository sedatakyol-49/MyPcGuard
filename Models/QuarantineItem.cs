namespace MyPcGuard.Models;

public sealed record QuarantineItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public string OriginalPath { get; init; } = string.Empty;
    public string QuarantinePath { get; init; } = string.Empty;
    public DateTimeOffset Date { get; init; } = DateTimeOffset.Now;
    public string Reason { get; init; } = string.Empty;
}
