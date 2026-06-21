namespace MyPcGuard.Agents.Models;

public sealed record ActionPlanStep
{
    public int Order { get; init; }
    public string Description { get; init; } = string.Empty;
    public AgentActionType ActionType { get; init; } = AgentActionType.None;
    public string Target { get; init; } = string.Empty;
    public bool RequiresAdmin { get; init; }
    public bool IsReversible { get; init; }
    public string BackupPath { get; init; } = string.Empty;
    public string ValidationRule { get; init; } = string.Empty;
}
