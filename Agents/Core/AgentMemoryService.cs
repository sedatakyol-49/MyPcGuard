using System.Text.Json;
using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Core;

public sealed class AgentMemoryService : IAgentMemoryService
{
    private static readonly string MemoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyPcGuard", "agent-memory.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<AgentMemory> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(MemoryPath))
            {
                return new AgentMemory();
            }

            await using var stream = File.OpenRead(MemoryPath);
            return await JsonSerializer.DeserializeAsync<AgentMemory>(stream, JsonOptions, cancellationToken) ?? new AgentMemory();
        }
        catch
        {
            return new AgentMemory();
        }
    }

    public async Task SaveResultsAsync(IReadOnlyList<AgentResult> results, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MemoryPath)!);
        var existing = await LoadAsync(cancellationToken);
        var memory = existing with
        {
            LastResults = results,
            UpdatedAt = DateTimeOffset.Now
        };

        await using var stream = File.Create(MemoryPath);
        await JsonSerializer.SerializeAsync(stream, memory, JsonOptions, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(MemoryPath))
        {
            File.Delete(MemoryPath);
        }

        return Task.CompletedTask;
    }
}
