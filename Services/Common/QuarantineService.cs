using System.Text.Json;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class QuarantineService : IQuarantineService
{
    private static readonly string BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyPcGuard");
    private static readonly string QuarantinePath = Path.Combine(BasePath, "Quarantine");
    private static readonly string IndexPath = Path.Combine(BasePath, "quarantine-index.json");

    public async Task<IReadOnlyList<QuarantineItem>> GetItemsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(IndexPath))
            {
                return [];
            }

            await using var stream = File.OpenRead(IndexPath);
            return await JsonSerializer.DeserializeAsync<List<QuarantineItem>>(stream, cancellationToken: cancellationToken) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ActionResult> MoveToQuarantineAsync(string path, string reason, CancellationToken cancellationToken)
    {
        try
        {
            if (IsProtectedPath(path))
            {
                return new ActionResult { Status = ActionResultStatus.NotAllowed, Message = "Action_NotAllowed" };
            }

            if (!File.Exists(path))
            {
                return new ActionResult { Status = ActionResultStatus.Failed, Message = "Common_Error" };
            }

            Directory.CreateDirectory(QuarantinePath);
            var item = new QuarantineItem
            {
                Name = Path.GetFileName(path),
                OriginalPath = path,
                QuarantinePath = Path.Combine(QuarantinePath, $"{Guid.NewGuid():N}_{Path.GetFileName(path)}"),
                Reason = reason
            };

            File.Move(path, item.QuarantinePath);
            var items = (await GetItemsAsync(cancellationToken)).ToList();
            items.Add(item);
            await WriteIndexAsync(items, cancellationToken);
            return new ActionResult { Status = ActionResultStatus.Success, Message = "Action_Quarantine_Success" };
        }
        catch (UnauthorizedAccessException)
        {
            return new ActionResult { Status = ActionResultStatus.RequiresAdmin, Message = "Action_RequiresAdmin" };
        }
        catch
        {
            return new ActionResult { Status = ActionResultStatus.Failed, Message = "Action_Quarantine_Failed" };
        }
    }

    public async Task<ActionResult> RestoreAsync(QuarantineItem item, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(item.QuarantinePath))
            {
                return new ActionResult { Status = ActionResultStatus.Failed, Message = "Common_Error" };
            }

            Directory.CreateDirectory(Path.GetDirectoryName(item.OriginalPath)!);
            File.Move(item.QuarantinePath, item.OriginalPath, overwrite: false);
            var items = (await GetItemsAsync(cancellationToken)).Where(existing => existing.Id != item.Id).ToList();
            await WriteIndexAsync(items, cancellationToken);
            return new ActionResult { Status = ActionResultStatus.Success, Message = "Action_Quarantine_Success" };
        }
        catch (UnauthorizedAccessException)
        {
            return new ActionResult { Status = ActionResultStatus.RequiresAdmin, Message = "Action_RequiresAdmin" };
        }
        catch
        {
            return new ActionResult { Status = ActionResultStatus.Failed, Message = "Action_Quarantine_Failed" };
        }
    }

    private static async Task WriteIndexAsync(List<QuarantineItem> items, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(BasePath);
        await using var stream = File.Create(IndexPath);
        await JsonSerializer.SerializeAsync(stream, items, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }

    private static bool IsProtectedPath(string path)
    {
        return path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), StringComparison.OrdinalIgnoreCase);
    }
}
