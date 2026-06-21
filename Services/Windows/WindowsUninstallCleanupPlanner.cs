using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

public sealed class WindowsUninstallCleanupPlanner : IUninstallCleanupPlanner
{
    public Task<UninstallCleanupPlan> AnalyzeLeftoversAsync(InstalledProgramItem program, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var candidates = new List<UninstallLeftoverCandidate>();
            AddIfExists(candidates, program.InstallLocation, "Program install location from Windows registry.");

            foreach (var root in GetUserDataRoots())
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var path in FindLikelyProgramFolders(root, program.Name).Take(8))
                {
                    AddIfExists(candidates, path, "Folder name matches the selected program name.");
                }
            }

            return new UninstallCleanupPlan
            {
                ProgramName = program.Name,
                Summary = candidates.Count == 0
                    ? "No clearly related leftover folders were found. MyPcGuard will not guess or delete unrelated files."
                    : $"{candidates.Count} clearly related leftover candidates found. Review before moving anything to backup.",
                Candidates = candidates
                    .GroupBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList()
            };
        }, cancellationToken);
    }

    private static IEnumerable<string> GetUserDataRoots()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return new[] { local, roaming, common }.Where(Directory.Exists);
    }

    private static IEnumerable<string> FindLikelyProgramFolders(string root, string programName)
    {
        if (string.IsNullOrWhiteSpace(programName) || !Directory.Exists(root))
        {
            yield break;
        }

        var token = Normalize(programName);
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var name = Normalize(Path.GetFileName(directory));
            if (!string.IsNullOrWhiteSpace(name) && (name.Contains(token, StringComparison.OrdinalIgnoreCase) || token.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                yield return directory;
            }
        }
    }

    private static void AddIfExists(ICollection<UninstallLeftoverCandidate> candidates, string path, string reason)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        candidates.Add(new UninstallLeftoverCandidate
        {
            Path = path,
            Reason = reason,
            SizeText = FormatBytes(GetDirectorySize(path)),
            SafetyLevel = CleanupSafetyLevel.RequiresConfirmation
        });
    }

    private static long GetDirectorySize(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(file =>
            {
                try { return new FileInfo(file).Length; }
                catch { return 0L; }
            });
        }
        catch
        {
            return 0;
        }
    }

    private static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "-";
        }

        string[] units = ["B", "KB", "MB", "GB"];
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
