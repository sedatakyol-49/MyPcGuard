namespace MyPcGuard.Models;

public sealed record InstalledProgramItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Publisher { get; init; } = string.Empty;
    public string InstallDate { get; init; } = string.Empty;
    public string InstallLocation { get; init; } = string.Empty;
    public long EstimatedSizeBytes { get; init; }
    public string UninstallCommand { get; init; } = string.Empty;
    public string QuietUninstallCommand { get; init; } = string.Empty;
    public bool CanUninstall => !string.IsNullOrWhiteSpace(UninstallCommand) || !string.IsNullOrWhiteSpace(QuietUninstallCommand);
    public string SizeText => EstimatedSizeBytes > 0 ? $"{EstimatedSizeBytes / 1024d / 1024d:0.0} MB" : "-";
}
