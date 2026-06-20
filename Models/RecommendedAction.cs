namespace MyPcGuard.Models;

public sealed record RecommendedAction
{
    public ActionType ActionType { get; init; }
    public ActionSafetyLevel SafetyLevel { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TargetPath { get; init; } = string.Empty;
}
