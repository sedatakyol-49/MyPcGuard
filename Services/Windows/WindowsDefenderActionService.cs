using System.Diagnostics;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

public sealed class WindowsDefenderActionService : IDefenderActionService
{
    public Task<ActionResult> StartQuickScanAsync(CancellationToken cancellationToken)
    {
        return RunPowerShellAsync("Start-MpScan -ScanType QuickScan", cancellationToken);
    }

    public Task<ActionResult> StartFullScanAsync(CancellationToken cancellationToken)
    {
        return RunPowerShellAsync("Start-MpScan -ScanType FullScan", cancellationToken);
    }

    public async Task<ActionResult> OpenWindowsSecurityAsync(CancellationToken cancellationToken)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "windowsdefender:", UseShellExecute = true });
            await Task.CompletedTask;
            return new ActionResult { Status = ActionResultStatus.Success, Message = "Common_Success" };
        }
        catch
        {
            return new ActionResult { Status = ActionResultStatus.Failed, Message = "Common_Error" };
        }
    }

    private static async Task<ActionResult> RunPowerShellAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                ArgumentList = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command },
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return new ActionResult { Status = ActionResultStatus.Failed, Message = "Common_Error" };
            }

            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode == 0)
            {
                return new ActionResult { Status = ActionResultStatus.Success, Message = "Common_Success" };
            }

            return new ActionResult { Status = ActionResultStatus.Failed, Message = await process.StandardError.ReadToEndAsync(cancellationToken) };
        }
        catch (UnauthorizedAccessException)
        {
            return new ActionResult { Status = ActionResultStatus.RequiresAdmin, Message = "Action_RequiresAdmin" };
        }
        catch
        {
            return new ActionResult { Status = ActionResultStatus.Failed, Message = "Common_Error" };
        }
    }
}
