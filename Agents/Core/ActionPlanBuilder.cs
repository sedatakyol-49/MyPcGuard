using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Core;

public sealed class ActionPlanBuilder(IAgentPolicyService policyService) : IActionPlanBuilder
{
    public ActionPlan Build(string title, string description, IReadOnlyList<ActionPlanStep> steps, AgentSafetyLevel safetyLevel)
    {
        var allowedSteps = steps
            .Where(step => policyService.IsActionAllowed(step.ActionType, GuessCategory(step.ActionType)))
            .OrderBy(step => step.Order)
            .ToList();

        return new ActionPlan
        {
            Title = title,
            Description = description,
            Steps = allowedSteps,
            SafetyLevel = allowedSteps.Count == steps.Count ? safetyLevel : AgentSafetyLevel.NotAllowed,
            RequiresAdmin = allowedSteps.Any(step => step.RequiresAdmin),
            IsReversible = allowedSteps.All(step => step.IsReversible),
            BackupRequired = allowedSteps.Any(step => !string.IsNullOrWhiteSpace(step.BackupPath)),
            EstimatedImpact = description,
            UserConfirmationText = "Review this plan before running any action.",
            CanExecuteAutomaticallyAfterApproval = false
        };
    }

    private static AgentCategory GuessCategory(AgentActionType actionType)
    {
        return actionType switch
        {
            AgentActionType.OpenWindowsUpdate or AgentActionType.OpenDeviceManager or AgentActionType.ShowOfficialManufacturerPage or AgentActionType.ExportDriverReport => AgentCategory.DriverCheck,
            AgentActionType.DisableStartupEntry or AgentActionType.EnableStartupEntry or AgentActionType.StopProcess => AgentCategory.StartupOptimization,
            AgentActionType.StartDefenderQuickScan or AgentActionType.OpenWindowsSecurity => AgentCategory.Security,
            _ => AgentCategory.SystemHealth
        };
    }
}
