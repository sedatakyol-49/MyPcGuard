namespace MyPcGuard.Agents.Models;

public sealed record AgentRecommendation
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public AgentSafetyLevel SafetyLevel { get; init; } = AgentSafetyLevel.RequiresConfirmation;
    public string ExpectedBenefit { get; init; } = string.Empty;
    public string PossibleSideEffects { get; init; } = string.Empty;
    public AgentActionType RelatedActionType { get; init; } = AgentActionType.None;
}
