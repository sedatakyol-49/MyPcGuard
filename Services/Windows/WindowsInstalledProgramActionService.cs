using System.Diagnostics;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

public sealed class WindowsInstalledProgramActionService(IActionHistoryService actionHistoryService) : IInstalledProgramActionService
{
    public async Task<ActionResult> StartUninstallAsync(InstalledProgramItem item, CancellationToken cancellationToken)
    {
        var command = string.IsNullOrWhiteSpace(item.UninstallCommand) ? item.QuietUninstallCommand : item.UninstallCommand;
        if (string.IsNullOrWhiteSpace(command))
        {
            return await CompleteAsync(item, ActionResultStatus.NotAllowed, "Action_Uninstall_NotAvailable", cancellationToken);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = true
            });

            return await CompleteAsync(item, ActionResultStatus.Success, "Action_Uninstall_Started", cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return await CompleteAsync(item, ActionResultStatus.RequiresAdmin, "Action_RequiresAdmin", cancellationToken);
        }
        catch
        {
            return await CompleteAsync(item, ActionResultStatus.Failed, "Action_Uninstall_Failed", cancellationToken);
        }
    }

    private async Task<ActionResult> CompleteAsync(InstalledProgramItem item, ActionResultStatus status, string message, CancellationToken cancellationToken)
    {
        await actionHistoryService.AddAsync(new ActionHistoryItem
        {
            ActionType = ActionType.UninstallProgram,
            Target = item.Name,
            Status = status,
            Message = message
        }, cancellationToken);

        return new ActionResult { Status = status, Message = message };
    }
}
