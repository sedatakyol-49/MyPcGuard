using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IAgentPolicyService
{
    IReadOnlySet<AgentPolicyRule> Rules { get; }
    bool IsActionAllowed(AgentActionType actionType, AgentCategory category, WebSourceCandidate? source = null);
    string ExplainPolicy(AgentActionType actionType, AgentCategory category);
}
