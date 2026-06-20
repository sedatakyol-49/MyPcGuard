using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class UnsupportedAutostartActionService : IAutostartActionService
{
    public Task<StartupActionResult> EnableAsync(StartupItem item, CancellationToken cancellationToken)
    {
        return NotSupported();
    }

    public Task<StartupActionResult> DisableAsync(StartupItem item, CancellationToken cancellationToken)
    {
        return NotSupported();
    }

    public Task<StartupActionResult> StopProcessAsync(StartupItem item, CancellationToken cancellationToken)
    {
        return NotSupported();
    }

    public Task<StartupActionResult> OpenFileLocationAsync(StartupItem item, CancellationToken cancellationToken)
    {
        return NotSupported();
    }

    public Task<StartupActionResult> StartDefenderQuickScanAsync(StartupItem item, CancellationToken cancellationToken)
    {
        return NotSupported();
    }

    private static Task<StartupActionResult> NotSupported()
    {
        return Task.FromResult(StartupActionResult.Fail("Diese Aktion wird auf dieser Plattform noch nicht unterstuetzt."));
    }
}
