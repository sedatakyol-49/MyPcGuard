using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;
using MyPcGuard.Models;

namespace MyPcGuard.Agents.Windows;

public sealed class ProgramUninstallAgent(IActionPlanBuilder actionPlanBuilder) : IAgent
{
    private static readonly string[] SensitiveProgramMarkers =
    [
        "driver",
        "security",
        "vpn",
        "bank",
        "defender",
        "runtime",
        "redistributable"
    ];

    public string Name => "Program Uninstall Agent";
    public string Description => "Reviews installed programs and builds conservative uninstall plans.";
    public AgentCategory Category => AgentCategory.ProgramUninstall;

    public Task<AgentResult> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var candidates = context.InstalledPrograms
            .Where(program => program.CanUninstall && !IsSensitive(program.Name))
            .Where(program => string.IsNullOrWhiteSpace(program.Publisher) || program.Name.Contains("toolbar", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        var findings = candidates.Select(program => new AgentFinding
        {
            Title = $"Review program: {program.Name}",
            Description = "This program may be optional or has limited publisher information.",
            Category = Category,
            RiskLevel = RiskLevel.Info,
            Evidence = $"{program.Publisher} {program.InstallLocation}".Trim(),
            ConfidenceScore = 0.45,
            IsActionable = true
        }).ToList();

        var plans = candidates.Select(program => actionPlanBuilder.Build(
            $"Uninstall review: {program.Name}",
            "Run the normal Windows uninstaller first. Leftover cleanup must be reviewed separately and moved to backup, not permanently deleted.",
            [
                new ActionPlanStep { Order = 1, Description = "Run normal Windows uninstaller after approval.", ActionType = AgentActionType.RunNormalUninstaller, Target = program.Name, RequiresAdmin = false, IsReversible = false },
                new ActionPlanStep { Order = 2, Description = "Scan for clearly related leftovers.", ActionType = AgentActionType.ScanLeftovers, Target = program.InstallLocation, IsReversible = true },
                new ActionPlanStep { Order = 3, Description = "Move confirmed leftovers to backup area.", ActionType = AgentActionType.MoveLeftoversToBackup, Target = program.InstallLocation, IsReversible = true, BackupPath = "%LocalAppData%\\MyPcGuard\\Backups" }
            ],
            AgentSafetyLevel.RequiresConfirmation)).ToList();

        return Task.FromResult(new AgentResult
        {
            AgentName = Name,
            Summary = candidates.Count == 0 ? "No uninstall candidates need attention." : $"{candidates.Count} installed programs may deserve review.",
            Findings = findings,
            Recommendations = findings.Select(finding => new AgentRecommendation
            {
                Title = "Use official uninstaller only",
                Description = "Start with the normal Windows uninstall command and review side effects first.",
                Reason = "Aggressive cleanup can remove shared files or settings.",
                SafetyLevel = AgentSafetyLevel.RequiresConfirmation,
                ExpectedBenefit = "Remove unwanted software safely.",
                PossibleSideEffects = "The program and related integrations may stop working.",
                RelatedActionType = AgentActionType.RunNormalUninstaller
            }).ToList(),
            SuggestedActionPlans = plans,
            ConfidenceScore = candidates.Count == 0 ? 0.55 : 0.5,
            RiskLevel = RiskLevel.Info,
            RequiresUserApproval = true
        });
    }

    public Task<ActionPlan?> BuildActionPlanAsync(AgentContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionPlan?>(null);
    }

    private static bool IsSensitive(string name)
    {
        return SensitiveProgramMarkers.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
