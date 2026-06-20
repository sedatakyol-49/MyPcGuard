namespace MyPcGuard.Models;

public sealed record SystemOverview
{
    public OperatingSystemType OperatingSystemType { get; init; }
    public string OperatingSystemName { get; init; } = "Unknown";
    public string MachineName { get; init; } = Environment.MachineName;
    public string UserName { get; init; } = Environment.UserName;
    public double CpuUsagePercent { get; init; }
    public double MemoryUsagePercent { get; init; }
    public double DiskUsagePercent { get; init; }
    public long TotalMemoryBytes { get; init; }
    public long AvailableMemoryBytes { get; init; }
}
