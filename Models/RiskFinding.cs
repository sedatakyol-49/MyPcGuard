namespace MyPcGuard.Models;

public sealed record RiskFinding
{
    public RiskLevel Level { get; init; }
    public string Category { get; init; } = string.Empty;
    public string RuleId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? ProcessId { get; init; }
    public string? TargetPath { get; init; }
    public string Publisher { get; init; } = "Unknown";
    public string Reason { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public IReadOnlyList<FindingAction> SuggestedActions { get; init; } = [];

    public string Title => Name;
    public string Description => Reason;
    public string? Source => TargetPath;
    public string ActionsText => SuggestedActions.Count == 0 ? "-" : string.Join(", ", SuggestedActions);
    public string Key => $"{Category}|{TargetPath}|{ProcessId}|{RuleId}";
}
