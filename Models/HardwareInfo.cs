namespace MyPcGuard.Models;

public sealed record HardwareInfo
{
    public string ComputerName { get; init; } = Environment.MachineName;
    public string UserName { get; init; } = Environment.UserName;
    public string OperatingSystem { get; init; } = string.Empty;
    public string ProcessorName { get; init; } = string.Empty;
    public int LogicalProcessorCount { get; init; } = Environment.ProcessorCount;
    public string TotalMemoryText { get; init; } = string.Empty;
    public string AvailableMemoryText { get; init; } = string.Empty;
    public IReadOnlyList<DiskInfoItem> Disks { get; init; } = [];
}
