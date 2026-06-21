using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Core;

public sealed class ActionApprovalService : IActionApprovalService
{
    public Task<bool> RequestApprovalAsync(ActionPlan actionPlan, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}
