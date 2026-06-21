namespace MyPcGuard.Models;

public sealed record PerformanceBenchmarkResult
{
    public double DiskWriteMbPerSecond { get; init; }
    public double DiskReadMbPerSecond { get; init; }
    public double MemoryCopyMbPerSecond { get; init; }
    public string Summary { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}
