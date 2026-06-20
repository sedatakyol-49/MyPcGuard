namespace MyPcGuard.Models;

public sealed record ActionHistoryItem
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public ActionType ActionType { get; init; }
    public ActionResultStatus Status { get; init; }
    public string Target { get; init; } = string.Empty;
    public int? ProcessId { get; init; }
    public string Message { get; init; } = string.Empty;
}
