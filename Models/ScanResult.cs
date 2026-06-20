using System.Collections.ObjectModel;

namespace MyPcGuard.Models;

public sealed record ScanResult
{
    public DateTimeOffset ScannedAt { get; init; } = DateTimeOffset.Now;
    public SystemOverview Overview { get; init; } = new();
    public RiskLevel OverallRiskLevel { get; init; } = RiskLevel.None;
    public ObservableCollection<RiskFinding> Findings { get; init; } = [];
    public ObservableCollection<ProcessScanItem> Processes { get; init; } = [];
    public ObservableCollection<StartupItem> StartupItems { get; init; } = [];
    public ObservableCollection<ServiceScanItem> Services { get; init; } = [];
    public ObservableCollection<NetworkConnectionItem> NetworkConnections { get; init; } = [];
    public DefenderStatus DefenderStatus { get; init; } = new();
}
