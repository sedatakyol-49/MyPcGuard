namespace MyPcGuard.Models;

public sealed record DiskInfoItem
{
    public string Name { get; init; } = string.Empty;
    public string DriveFormat { get; init; } = string.Empty;
    public string DriveType { get; init; } = string.Empty;
    public string TotalSizeText { get; init; } = string.Empty;
    public string FreeSpaceText { get; init; } = string.Empty;
    public double UsedPercent { get; init; }
    public string HealthStatus { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}
