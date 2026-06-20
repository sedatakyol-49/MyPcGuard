using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Mac;

public sealed class MacDefenderScanner : IDefenderScanner
{
    public Task<DefenderStatus> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DefenderStatus
        {
            IsAvailable = false,
            StatusText = "Not implemented for macOS yet",
            ErrorMessage = "Not implemented for macOS yet"
        });
    }
}
