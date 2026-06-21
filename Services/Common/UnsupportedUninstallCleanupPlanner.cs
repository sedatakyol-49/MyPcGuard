using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class UnsupportedUninstallCleanupPlanner : IUninstallCleanupPlanner
{
    public Task<UninstallCleanupPlan> AnalyzeLeftoversAsync(InstalledProgramItem program, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UninstallCleanupPlan
        {
            ProgramName = program.Name,
            Summary = "Uninstall cleanup planning is not supported on this platform yet."
        });
    }
}
