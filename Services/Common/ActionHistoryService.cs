using System.Text.Json;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class ActionHistoryService : IActionHistoryService
{
    private static readonly string HistoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyPcGuard", "action-history.json");

    public async Task<IReadOnlyList<ActionHistoryItem>> GetHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(HistoryPath))
            {
                return [];
            }

            await using var stream = File.OpenRead(HistoryPath);
            return await JsonSerializer.DeserializeAsync<List<ActionHistoryItem>>(stream, cancellationToken: cancellationToken) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task AddAsync(ActionHistoryItem item, CancellationToken cancellationToken)
    {
        try
        {
            var items = (await GetHistoryAsync(cancellationToken)).ToList();
            items.Add(item);
            Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
            await using var stream = File.Create(HistoryPath);
            await JsonSerializer.SerializeAsync(stream, items, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
        }
        catch
        {
        }
    }
}
