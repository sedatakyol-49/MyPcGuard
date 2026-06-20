using System.Text.Json;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class UserSettingsService : IUserSettingsService
{
    private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyPcGuard", "user-settings.json");
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase) { "de", "en", "tr" };

    public string GetLanguage()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return "de";
            }

            using var stream = File.OpenRead(SettingsPath);
            var settings = JsonSerializer.Deserialize<UserSettings>(stream);
            return settings is not null && SupportedCultures.Contains(settings.Language) ? settings.Language : "de";
        }
        catch
        {
            return "de";
        }
    }

    public async Task<string> GetLanguageAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return "de";
            }

            await using var stream = File.OpenRead(SettingsPath);
            var settings = await JsonSerializer.DeserializeAsync<UserSettings>(stream, cancellationToken: cancellationToken);
            return settings is not null && SupportedCultures.Contains(settings.Language) ? settings.Language : "de";
        }
        catch
        {
            return "de";
        }
    }

    public async Task SaveLanguageAsync(string cultureCode, CancellationToken cancellationToken)
    {
        try
        {
            var language = SupportedCultures.Contains(cultureCode) ? cultureCode : "de";
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            await using var stream = File.Create(SettingsPath);
            await JsonSerializer.SerializeAsync(stream, new UserSettings { Language = language }, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
        }
        catch
        {
        }
    }

    private sealed record UserSettings
    {
        public string Language { get; init; } = "de";
    }
}
