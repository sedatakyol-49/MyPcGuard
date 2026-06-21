namespace MyPcGuard.Models;

public sealed record UninstallCleanupPlan
{
    public string ProgramName { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<UninstallLeftoverCandidate> Candidates { get; init; } = [];
    public bool CanExecuteAutomatically => false;
}
