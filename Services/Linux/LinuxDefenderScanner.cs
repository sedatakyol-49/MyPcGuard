using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Linux;

public sealed class LinuxDefenderScanner : IDefenderScanner
{
    public Task<DefenderStatus> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DefenderStatus
        {
            IsAvailable = false,
            StatusText = "Not implemented for Linux yet",
            ErrorMessage = "Not implemented for Linux yet"
        });
    }
}
