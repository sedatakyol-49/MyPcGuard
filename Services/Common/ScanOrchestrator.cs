using System.Collections.ObjectModel;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class ScanOrchestrator(
    ISystemInfoService systemInfoService,
    IProcessScanner processScanner,
    IStartupScanner startupScanner,
    IServiceScanner serviceScanner,
    INetworkScanner networkScanner,
    IDefenderScanner defenderScanner,
    IRiskEngine riskEngine) : IScanOrchestrator
{
    public async Task<ScanResult> RunScanAsync(CancellationToken cancellationToken)
    {
        var overviewTask = systemInfoService.GetOverviewAsync(cancellationToken);
        var processesTask = processScanner.ScanAsync(cancellationToken);
        var startupTask = startupScanner.ScanAsync(cancellationToken);
        var servicesTask = serviceScanner.ScanAsync(cancellationToken);
        var networkTask = networkScanner.ScanAsync(cancellationToken);
        var defenderTask = defenderScanner.ScanAsync(cancellationToken);

        await Task.WhenAll(overviewTask, processesTask, startupTask, servicesTask, networkTask, defenderTask);

        var partialResult = new ScanResult
        {
            ScannedAt = DateTimeOffset.Now,
            Overview = await overviewTask,
            Processes = new ObservableCollection<ProcessScanItem>(await processesTask),
            StartupItems = new ObservableCollection<StartupItem>(await startupTask),
            Services = new ObservableCollection<ServiceScanItem>(await servicesTask),
            NetworkConnections = new ObservableCollection<NetworkConnectionItem>(await networkTask),
            DefenderStatus = await defenderTask
        };

        var findings = riskEngine.Evaluate(partialResult);

        return partialResult with
        {
            Findings = new ObservableCollection<RiskFinding>(findings),
            OverallRiskLevel = riskEngine.GetOverallRiskLevel(findings)
        };
    }
}
