using MyPcGuard.Models;

namespace MyPcGuard.Agents.Models;

public sealed record DriverIssue
{
    public string DeviceName { get; init; } = string.Empty;
    public string DeviceClass { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public RiskLevel RiskLevel { get; init; } = RiskLevel.Info;
}
