namespace MyPcGuard.Agents.Models;

public sealed record ActionPlan
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<ActionPlanStep> Steps { get; init; } = [];
    public AgentSafetyLevel SafetyLevel { get; init; } = AgentSafetyLevel.RequiresConfirmation;
    public bool RequiresAdmin { get; init; }
    public bool IsReversible { get; init; }
    public bool BackupRequired { get; init; }
    public string EstimatedImpact { get; init; } = string.Empty;
    public string UserConfirmationText { get; init; } = string.Empty;
    public bool CanExecuteAutomaticallyAfterApproval { get; init; }
}
