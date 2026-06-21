using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IHardwareInfoService
{
    Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken cancellationToken);
    Task<PerformanceBenchmarkResult> RunQuickBenchmarkAsync(CancellationToken cancellationToken);
}
