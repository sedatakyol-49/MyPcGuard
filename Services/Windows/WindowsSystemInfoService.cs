using System.Diagnostics;
using System.Runtime.InteropServices;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

public sealed class WindowsSystemInfoService : ISystemInfoService
{
    public Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var memory = GetMemoryStatus();
            var diskUsage = GetSystemDiskUsage();
            var cpuUsage = await GetCpuUsageAsync(cancellationToken);

            return new SystemOverview
            {
                OperatingSystemType = OperatingSystemType.Windows,
                OperatingSystemName = RuntimeInformation.OSDescription,
                CpuUsagePercent = cpuUsage,
                MemoryUsagePercent = memory.total > 0 ? (memory.total - memory.available) * 100d / memory.total : 0,
                DiskUsagePercent = diskUsage,
                TotalMemoryBytes = (long)Math.Min(memory.total, long.MaxValue),
                AvailableMemoryBytes = (long)Math.Min(memory.available, long.MaxValue)
            };
        }, cancellationToken);
    }

    private static async Task<double> GetCpuUsageAsync(CancellationToken cancellationToken)
    {
        try
        {
            var before = GetTotalProcessTime();
            await Task.Delay(250, cancellationToken);
            var after = GetTotalProcessTime();
            return Math.Clamp((after - before).TotalMilliseconds / 250d / Environment.ProcessorCount * 100d, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    private static TimeSpan GetTotalProcessTime()
    {
        var total = TimeSpan.Zero;
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    total += process.TotalProcessorTime;
                }
                catch
                {
                }
            }
        }

        return total;
    }

    private static (ulong total, ulong available) GetMemoryStatus()
    {
        var status = new MemoryStatusEx();
        status.dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();
        return GlobalMemoryStatusEx(ref status)
            ? (status.ullTotalPhys, status.ullAvailPhys)
            : (0, 0);
    }

    private static double GetSystemDiskUsage()
    {
        try
        {
            var root = Path.GetPathRoot(Environment.SystemDirectory);
            var drive = DriveInfo.GetDrives().FirstOrDefault(drive => drive.IsReady && drive.RootDirectory.FullName.Equals(root, StringComparison.OrdinalIgnoreCase));
            return drive is { TotalSize: > 0 }
                ? (drive.TotalSize - drive.AvailableFreeSpace) * 100d / drive.TotalSize
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}
