using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IActionPlanBuilder
{
    ActionPlan Build(string title, string description, IReadOnlyList<ActionPlanStep> steps, AgentSafetyLevel safetyLevel);
}
