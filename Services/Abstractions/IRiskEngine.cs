using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IRiskEngine
{
    IReadOnlyList<RiskFinding> Evaluate(ScanResult scanResult);
    RiskLevel GetOverallRiskLevel(IEnumerable<RiskFinding> findings);
}
