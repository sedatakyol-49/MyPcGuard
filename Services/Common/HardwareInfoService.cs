using System.Diagnostics;
using System.Runtime.InteropServices;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class HardwareInfoService : IHardwareInfoService
{
    public Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var memory = GC.GetGCMemoryInfo();
            var totalMemory = memory.TotalAvailableMemoryBytes > 0 ? memory.TotalAvailableMemoryBytes : 0;
            var disks = DriveInfo.GetDrives()
                .Where(drive => drive.IsReady)
                .Select(CreateDiskInfo)
                .ToList();

            return new HardwareInfo
            {
                ComputerName = Environment.MachineName,
                UserName = Environment.UserName,
                OperatingSystem = RuntimeInformation.OSDescription,
                ProcessorName = RuntimeInformation.ProcessArchitecture.ToString(),
                LogicalProcessorCount = Environment.ProcessorCount,
                TotalMemoryText = FormatBytes(totalMemory),
                AvailableMemoryText = "-",
                Disks = disks
            };
        }, cancellationToken);
    }

    public async Task<PerformanceBenchmarkResult> RunQuickBenchmarkAsync(CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"mypcguard-benchmark-{Guid.NewGuid():N}.bin");
        var buffer = new byte[32 * 1024 * 1024];
        Random.Shared.NextBytes(buffer);

        try
        {
            var writeWatch = Stopwatch.StartNew();
            await File.WriteAllBytesAsync(tempFile, buffer, cancellationToken);
            writeWatch.Stop();

            var readWatch = Stopwatch.StartNew();
            _ = await File.ReadAllBytesAsync(tempFile, cancellationToken);
            readWatch.Stop();

            var target = new byte[buffer.Length];
            var memoryWatch = Stopwatch.StartNew();
            Buffer.BlockCopy(buffer, 0, target, 0, buffer.Length);
            memoryWatch.Stop();

            return new PerformanceBenchmarkResult
            {
                DiskWriteMbPerSecond = ToMbPerSecond(buffer.Length, writeWatch.Elapsed),
                DiskReadMbPerSecond = ToMbPerSecond(buffer.Length, readWatch.Elapsed),
                MemoryCopyMbPerSecond = ToMbPerSecond(buffer.Length, memoryWatch.Elapsed),
                Summary = "Quick local benchmark. Results vary with power mode, background load and disk cache."
            };
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
            }
        }
    }

    private static DiskInfoItem CreateDiskInfo(DriveInfo drive)
    {
        var usedPercent = drive.TotalSize > 0
            ? (drive.TotalSize - drive.AvailableFreeSpace) * 100d / drive.TotalSize
            : 0;

        var health = usedPercent >= 92 ? "Critical" : usedPercent >= 85 ? "Attention" : "Good";
        var recommendation = usedPercent >= 92
            ? "Free space is critically low. Back up important files and clean safely."
            : usedPercent >= 85
                ? "Free space is low. Review cleanup categories."
                : "Free space looks healthy.";

        return new DiskInfoItem
        {
            Name = drive.Name,
            DriveFormat = drive.DriveFormat,
            DriveType = drive.DriveType.ToString(),
            TotalSizeText = FormatBytes(drive.TotalSize),
            FreeSpaceText = FormatBytes(drive.AvailableFreeSpace),
            UsedPercent = usedPercent,
            HealthStatus = health,
            Recommendation = recommendation
        };
    }

    private static double ToMbPerSecond(long bytes, TimeSpan elapsed)
    {
        return elapsed.TotalSeconds <= 0 ? 0 : bytes / 1024d / 1024d / elapsed.TotalSeconds;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "-";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
