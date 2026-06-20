namespace MyPcGuard.Models;

public sealed record StartupItem
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
    public string Publisher { get; init; } = "Unknown";
    public string SignatureStatus { get; init; } = "Unknown";
    public string RegistryHive { get; init; } = string.Empty;
    public string RegistryKeyPath { get; init; } = string.Empty;
    public string RegistryValueName { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public bool IsCurrentlyRunning { get; init; }
    public IReadOnlyList<int> RelatedProcessIds { get; init; } = [];
    public StartupClassification StartupClassification { get; init; } = StartupClassification.Unknown;
    public string Recommendation { get; init; } = string.Empty;
    public bool CanDisable { get; init; }
    public bool CanEnable { get; init; }
    public bool CanStopProcess { get; init; }
    public string SafetyMessage { get; init; } = string.Empty;

    public string Location => string.IsNullOrWhiteSpace(RegistryHive) || string.IsNullOrWhiteSpace(RegistryKeyPath)
        ? string.Empty
        : $@"{RegistryHive}\{RegistryKeyPath}";
    public string StatusText => IsEnabled ? "Aktiv" : "Deaktiviert";
    public string RunningText => IsCurrentlyRunning ? "Ja" : "Nein";
}
