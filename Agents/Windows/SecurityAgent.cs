using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;
using MyPcGuard.Models;

namespace MyPcGuard.Agents.Windows;

public sealed class SecurityAgent(IActionPlanBuilder actionPlanBuilder) : IAgent
{
    public string Name => "Security Agent";
    public string Description => "Explains Defender status and suspicious process/startup findings without claiming to be antivirus.";
    public AgentCategory Category => AgentCategory.Security;

    public Task<AgentResult> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var scan = context.ScanResult;
        if (scan is null)
        {
            return Task.FromResult(new AgentResult
            {
                AgentName = Name,
                Summary = "Run a scan to let the security agent review Defender, processes and startup items.",
                ConfidenceScore = 0.4,
                RiskLevel = RiskLevel.Info,
                RequiresUserApproval = true
            });
        }

        var findings = new List<AgentFinding>();
        var recommendations = new List<AgentRecommendation>();
        var plans = new List<ActionPlan>();

        if (scan.DefenderStatus.RealTimeProtectionEnabled == false || scan.DefenderStatus.AntivirusEnabled == false)
        {
            findings.Add(new AgentFinding
            {
                Title = "Defender protection needs attention",
                Description = "Windows Defender reports disabled antivirus or real-time protection.",
                Category = Category,
                RiskLevel = RiskLevel.High,
                Evidence = scan.DefenderStatus.StatusText,
                ConfidenceScore = 0.9,
                IsActionable = true
            });

            recommendations.Add(new AgentRecommendation
            {
                Title = "Open Windows Security",
                Description = "Open Windows Security and enable protection manually.",
                Reason = "MyPcGuard should not silently change security settings.",
                SafetyLevel = AgentSafetyLevel.Safe,
                ExpectedBenefit = "Restores built-in protection.",
                RelatedActionType = AgentActionType.OpenWindowsSecurity
            });
        }

        var relevantRiskFindings = scan.Findings
            .Where(finding => finding.Level is RiskLevel.Critical or RiskLevel.High or RiskLevel.Medium)
            .Take(5)
            .ToList();

        foreach (var riskFinding in relevantRiskFindings)
        {
            findings.Add(new AgentFinding
            {
                Title = riskFinding.Name,
                Description = riskFinding.Reason,
                Category = Category,
                RiskLevel = riskFinding.Level,
                Evidence = riskFinding.TargetPath ?? string.Empty,
                ConfidenceScore = 0.75,
                IsActionable = true
            });
        }

        if (findings.Count > 0)
        {
            plans.Add(actionPlanBuilder.Build(
                "Security review plan",
                "Review suspicious items, open locations, and use Defender scan. No aggressive antivirus action is performed automatically.",
                [
                    new ActionPlanStep { Order = 1, Description = "Open suspicious file locations for inspection.", ActionType = AgentActionType.OpenFileLocation, IsReversible = true },
                    new ActionPlanStep { Order = 2, Description = "Start Defender quick scan after approval.", ActionType = AgentActionType.StartDefenderQuickScan, IsReversible = true }
                ],
                AgentSafetyLevel.RequiresConfirmation));
        }

        return Task.FromResult(new AgentResult
        {
            AgentName = Name,
            Summary = findings.Count == 0 ? "No priority security findings were detected." : $"{findings.Count} security findings need review.",
            Findings = findings,
            Recommendations = recommendations,
            SuggestedActionPlans = plans,
            ConfidenceScore = findings.Count == 0 ? 0.7 : 0.82,
            RiskLevel = findings.Select(finding => finding.RiskLevel).DefaultIfEmpty(RiskLevel.Info).OrderByDescending(RiskScore).First(),
            RequiresUserApproval = true
        });
    }

    public Task<ActionPlan?> BuildActionPlanAsync(AgentContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionPlan?>(null);
    }

    private static int RiskScore(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Critical => 5,
            RiskLevel.High => 4,
            RiskLevel.Medium => 3,
            RiskLevel.Low => 2,
            _ => 1
        };
    }
}
