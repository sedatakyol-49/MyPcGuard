using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IUninstallCleanupPlanner
{
    Task<UninstallCleanupPlan> AnalyzeLeftoversAsync(InstalledProgramItem program, CancellationToken cancellationToken);
}
