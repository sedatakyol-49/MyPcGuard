namespace MyPcGuard.Models;

public sealed record ActionHistoryEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public string Action { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public string TargetPath { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}
