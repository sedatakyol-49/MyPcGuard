using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;
using MyPcGuard.Models;

namespace MyPcGuard.Agents.Windows;

public sealed class StartupOptimizationAgent(IActionPlanBuilder actionPlanBuilder) : IAgent
{
    public string Name => "Startup Optimization Agent";
    public string Description => "Analyzes startup items and suggests safe startup optimization plans.";
    public AgentCategory Category => AgentCategory.StartupOptimization;

    public Task<AgentResult> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var startupItems = context.ScanResult?.StartupItems ?? [];
        var optionalItems = startupItems.Where(item => item.StartupClassification is StartupClassification.Optional or StartupClassification.Unnecessary).ToList();
        var suspiciousItems = startupItems.Where(item => item.StartupClassification == StartupClassification.Suspicious).ToList();
        var findings = new List<AgentFinding>();
        var recommendations = new List<AgentRecommendation>();
        var plans = new List<ActionPlan>();

        if (optionalItems.Count > 0)
        {
            findings.Add(new AgentFinding
            {
                Title = "Optional startup load",
                Description = $"{optionalItems.Count} startup items do not appear to be required at login.",
                Category = Category,
                RiskLevel = RiskLevel.Low,
                Evidence = string.Join(", ", optionalItems.Take(5).Select(item => item.Name)),
                ConfidenceScore = 0.72,
                IsActionable = true
            });

            recommendations.Add(new AgentRecommendation
            {
                Title = "Review optional startup apps",
                Description = "These programs do not have to start automatically. Disabling them can improve startup time.",
                Reason = "They are classified as optional or unnecessary and are not system-critical.",
                SafetyLevel = AgentSafetyLevel.RequiresConfirmation,
                ExpectedBenefit = "Faster sign-in and less background load.",
                PossibleSideEffects = "The app may need to be started manually when needed.",
                RelatedActionType = AgentActionType.DisableStartupEntry
            });
        }

        foreach (var item in suspiciousItems.Take(3))
        {
            findings.Add(new AgentFinding
            {
                Title = $"Suspicious startup item: {item.Name}",
                Description = "This startup item looks suspicious and should be checked before removal.",
                Category = Category,
                RiskLevel = RiskLevel.Medium,
                Evidence = item.Command,
                ConfidenceScore = 0.78,
                IsActionable = true
            });

            plans.Add(actionPlanBuilder.Build(
                $"Check suspicious startup item: {item.Name}",
                "Disable startup entry first, then run a Defender quick scan. No file is deleted automatically.",
                [
                    new ActionPlanStep { Order = 1, Description = "Open file location for manual inspection.", ActionType = AgentActionType.OpenFileLocation, Target = item.ExecutablePath, IsReversible = true },
                    new ActionPlanStep { Order = 2, Description = "Disable startup entry after user approval.", ActionType = AgentActionType.DisableStartupEntry, Target = item.Name, IsReversible = true, BackupPath = "Autostart backup" },
                    new ActionPlanStep { Order = 3, Description = "Start Defender quick scan.", ActionType = AgentActionType.StartDefenderQuickScan, Target = item.ExecutablePath, RequiresAdmin = false, IsReversible = true }
                ],
                AgentSafetyLevel.RequiresConfirmation));
        }

        return Task.FromResult(new AgentResult
        {
            AgentName = Name,
            Summary = optionalItems.Count == 0 && suspiciousItems.Count == 0
                ? "Startup items look reasonable."
                : $"{optionalItems.Count} optional and {suspiciousItems.Count} suspicious startup items found.",
            Findings = findings,
            Recommendations = recommendations,
            SuggestedActionPlans = plans,
            ConfidenceScore = findings.Count == 0 ? 0.6 : 0.76,
            RiskLevel = suspiciousItems.Count > 0 ? RiskLevel.Medium : optionalItems.Count > 0 ? RiskLevel.Low : RiskLevel.Info,
            RequiresUserApproval = true
        });
    }

    public Task<ActionPlan?> BuildActionPlanAsync(AgentContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionPlan?>(null);
    }
}
