using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;
using MyPcGuard.Models;

namespace MyPcGuard.Agents.Windows;

public sealed class DriverCheckAgent(IAgentPolicyService policyService, IDeviceDriverScanner deviceDriverScanner) : IAgent
{
    public string Name => "Driver Check Agent";
    public string Description => "Detects driver issues locally and only suggests manual official-source actions.";
    public AgentCategory Category => AgentCategory.DriverCheck;

    public async Task<AgentResult> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var driverDevices = await deviceDriverScanner.ScanAsync(cancellationToken);
        var driverIssues = driverDevices.Where(device => device.IsProblematic).ToList();
        var recommendations = new List<AgentRecommendation>
        {
            new()
            {
                Title = "Open Windows Update",
                Description = "Use Windows Update as the first driver update source.",
                Reason = policyService.ExplainPolicy(AgentActionType.OpenWindowsUpdate, Category),
                SafetyLevel = AgentSafetyLevel.Safe,
                ExpectedBenefit = "Microsoft-provided driver updates can be reviewed manually.",
                PossibleSideEffects = "Windows Update may also offer OS updates.",
                RelatedActionType = AgentActionType.OpenWindowsUpdate
            },
            new()
            {
                Title = "Open Device Manager",
                Description = "Review missing or problematic devices locally in Device Manager.",
                Reason = "Device Manager shows device status without downloading third-party packages.",
                SafetyLevel = AgentSafetyLevel.Safe,
                ExpectedBenefit = "Identify device vendor and model before any download decision.",
                RelatedActionType = AgentActionType.OpenDeviceManager
            },
            new()
            {
                Title = "Show official manufacturer page only",
                Description = "If online research is enabled, only verified or likely official manufacturer pages may be shown.",
                Reason = "Third-party driver updater and mirror websites are rejected.",
                SafetyLevel = AgentSafetyLevel.Safe,
                ExpectedBenefit = "The user can manually decide whether to download from the official source.",
                RelatedActionType = AgentActionType.ShowOfficialManufacturerPage
            },
            new()
            {
                Title = "Export driver report",
                Description = "Create a local report of detected driver issues and possible official sources.",
                Reason = "Reports are local and do not install anything.",
                SafetyLevel = AgentSafetyLevel.Safe,
                ExpectedBenefit = "Keeps a reviewable record for support or manual follow-up.",
                RelatedActionType = AgentActionType.ExportDriverReport
            }
        };

        var plans = new[]
        {
            new ActionPlan
            {
                Title = "Manual driver review plan",
                Description = "Detect issues locally, review Windows Update or Device Manager, and open only verified official manufacturer pages manually.",
                Steps =
                [
                    new ActionPlanStep { Order = 1, Description = "Open Windows Update manually.", ActionType = AgentActionType.OpenWindowsUpdate, Target = "ms-settings:windowsupdate", IsReversible = true },
                    new ActionPlanStep { Order = 2, Description = "Open Device Manager manually.", ActionType = AgentActionType.OpenDeviceManager, Target = "devmgmt.msc", IsReversible = true },
                    new ActionPlanStep { Order = 3, Description = "Show verified official manufacturer page if available.", ActionType = AgentActionType.ShowOfficialManufacturerPage, IsReversible = true },
                    new ActionPlanStep { Order = 4, Description = "Export local driver report.", ActionType = AgentActionType.ExportDriverReport, IsReversible = true }
                ],
                SafetyLevel = AgentSafetyLevel.Safe,
                RequiresAdmin = false,
                IsReversible = true,
                BackupRequired = false,
                EstimatedImpact = "No driver is downloaded or installed by MyPcGuard.",
                UserConfirmationText = "You decide manually whether to download or install anything from an official source.",
                CanExecuteAutomaticallyAfterApproval = false
            }
        };

        var findings = driverIssues.Select(issue => new AgentFinding
        {
            Title = string.IsNullOrWhiteSpace(issue.DeviceName) ? "Problematic driver/device" : issue.DeviceName,
            Description = $"Local device status: {issue.Status}. Reason: {issue.Reason}",
            Category = Category,
            RiskLevel = issue.RiskLevel,
            Evidence = $"{issue.Manufacturer} {issue.DeviceClass} {issue.InstanceId}".Trim(),
            ConfidenceScore = 0.7,
            IsActionable = true
        }).ToList();

        findings.Insert(0, new AgentFinding
        {
            Title = "Driver downloads are manual only",
            Description = "MyPcGuard must never download or install driver installers automatically.",
            Category = Category,
            RiskLevel = RiskLevel.Info,
            Evidence = "Policy: NoAutomaticDriverDownloads, OfficialSourcesOnlyForDrivers",
            ConfidenceScore = 1,
            IsActionable = true
        });

        return new AgentResult
        {
            AgentName = Name,
            Summary = context.OnlineResearchAllowed
                ? $"{driverIssues.Count} local driver/device issues found. Official source research is allowed, but downloads and installs stay manual."
                : $"{driverIssues.Count} local driver/device issues found. Enable online research to search for official manufacturer pages.",
            Findings = findings,
            Recommendations = recommendations,
            SuggestedActionPlans = plans,
            ConfidenceScore = 1,
            RiskLevel = driverIssues.Select(issue => issue.RiskLevel).DefaultIfEmpty(RiskLevel.Info).OrderByDescending(RiskScore).First(),
            RequiresUserApproval = true
        };
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
