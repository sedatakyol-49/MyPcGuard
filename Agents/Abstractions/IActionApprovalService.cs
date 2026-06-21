using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IActionApprovalService
{
    Task<bool> RequestApprovalAsync(ActionPlan actionPlan, CancellationToken cancellationToken);
}
