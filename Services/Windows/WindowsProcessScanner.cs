using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

public sealed class WindowsProcessScanner : IProcessScanner
{
    public Task<IReadOnlyList<ProcessScanItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            var processes = Process.GetProcesses();
            var firstSample = processes.ToDictionary(process => process.Id, SafeTotalProcessorTime);
            await Task.Delay(250, cancellationToken);

            var items = new List<ProcessScanItem>();
            foreach (var process in processes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (process)
                {
                    var path = SafeGetPath(process);
                    var signature = GetSignature(path);
                    var cpu = CalculateCpuUsage(process, firstSample);

                    items.Add(new ProcessScanItem
                    {
                        ProcessId = SafeGetId(process),
                        Name = SafeGetName(process),
                        Path = path,
                        CpuUsagePercent = cpu,
                        MemoryBytes = SafeWorkingSet(process),
                        Publisher = GetPublisher(path),
                        IsMicrosoftSigned = signature.isMicrosoftSigned,
                        SignatureStatus = signature.status,
                        SignatureTrustStatus = signature.trustStatus
                    });
                }
            }

            return (IReadOnlyList<ProcessScanItem>)items.OrderByDescending(item => item.CpuUsagePercent).ThenBy(item => item.Name).ToList();
        }, cancellationToken);
    }

    private static int SafeGetId(Process process)
    {
        try { return process.Id; } catch { return 0; }
    }

    private static string SafeGetName(Process process)
    {
        try { return process.ProcessName; } catch { return "Unknown"; }
    }

    private static string? SafeGetPath(Process process)
    {
        try { return process.MainModule?.FileName; } catch { return null; }
    }

    private static TimeSpan SafeTotalProcessorTime(Process process)
    {
        try { return process.TotalProcessorTime; } catch { return TimeSpan.Zero; }
    }

    private static long SafeWorkingSet(Process process)
    {
        try { return process.WorkingSet64; } catch { return 0; }
    }

    private static double CalculateCpuUsage(Process process, IReadOnlyDictionary<int, TimeSpan> firstSample)
    {
        try
        {
            if (!firstSample.TryGetValue(process.Id, out var before))
            {
                return 0;
            }

            var delta = process.TotalProcessorTime - before;
            return Math.Clamp(delta.TotalMilliseconds / 250d / Environment.ProcessorCount * 100d, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    private static string GetPublisher(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return "Unknown";
            }

            return string.IsNullOrWhiteSpace(FileVersionInfo.GetVersionInfo(path).CompanyName)
                ? "Unknown"
                : FileVersionInfo.GetVersionInfo(path).CompanyName!;
        }
        catch
        {
            return "Unknown";
        }
    }

    private static (bool? isMicrosoftSigned, string status, SignatureTrustStatus trustStatus) GetSignature(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return (null, "Not accessible", SignatureTrustStatus.NotAccessible);
            }

            if (!File.Exists(path))
            {
                return (null, "Unknown signature", SignatureTrustStatus.Unknown);
            }

#pragma warning disable SYSLIB0057
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
            var subject = certificate.Subject ?? string.Empty;
            var issuer = certificate.Issuer ?? string.Empty;
            var microsoft = subject.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) || issuer.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
            return (microsoft, microsoft ? "Microsoft signed" : "Signed", microsoft ? SignatureTrustStatus.TrustedMicrosoft : SignatureTrustStatus.TrustedVendor);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return (false, "Unsigned", SignatureTrustStatus.Unsigned);
        }
        catch
        {
            return (null, "Unknown signature", SignatureTrustStatus.Unknown);
        }
    }
}
