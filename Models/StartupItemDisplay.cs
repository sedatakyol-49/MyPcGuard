namespace MyPcGuard.Models;

public sealed record StartupItemDisplay
{
    public StartupItem Item { get; init; } = new();
    public string DisplayName { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string ClassificationText { get; init; } = string.Empty;
    public string PublisherText { get; init; } = string.Empty;
    public string ExecutablePathText { get; init; } = string.Empty;
    public string SignatureStatusText { get; init; } = string.Empty;
    public string RecommendationText { get; init; } = string.Empty;
    public string RunningText { get; init; } = string.Empty;
    public string DetailsTooltip { get; init; } = string.Empty;
    public bool CanEnable => Item.CanEnable;
    public bool CanDisable => Item.CanDisable;
    public bool CanStopProcess => Item.CanStopProcess;
    public bool CanMoveToQuarantine => Item.CanMoveToQuarantine;
}
