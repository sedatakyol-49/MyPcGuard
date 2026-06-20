namespace MyPcGuard.Models;

public sealed record DefenderStatus
{
    public bool IsAvailable { get; init; }
    public bool? RealTimeProtectionEnabled { get; init; }
    public bool? AntivirusEnabled { get; init; }
    public string StatusText { get; init; } = "Unknown";
    public string? ErrorMessage { get; init; }
}
