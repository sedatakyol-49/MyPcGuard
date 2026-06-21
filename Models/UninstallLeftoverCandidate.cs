namespace MyPcGuard.Models;

public sealed record UninstallLeftoverCandidate
{
    public string Path { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string SizeText { get; init; } = string.Empty;
    public CleanupSafetyLevel SafetyLevel { get; init; } = CleanupSafetyLevel.RequiresConfirmation;
}
