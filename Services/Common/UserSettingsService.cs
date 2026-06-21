using System.Text.Json;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class UserSettingsService : IUserSettingsService
{
    private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyPcGuard", "user-settings.json");
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase) { "de", "en", "tr" };

    public string GetLanguage()
    {
        return NormalizeLanguage(LoadSettingsSafe().Language);
    }

    public async Task<string> GetLanguageAsync(CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return NormalizeLanguage(settings.Language);
    }

    public async Task SaveLanguageAsync(string cultureCode, CancellationToken cancellationToken)
    {
        try
        {
            var settings = await GetSettingsAsync(cancellationToken);
            await SaveSettingsAsync(settings with { Language = NormalizeLanguage(cultureCode) }, cancellationToken);
        }
        catch
        {
        }
    }

    public async Task<UserSettingsSnapshot> GetSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new UserSettingsSnapshot();
            }

            await using var stream = File.OpenRead(SettingsPath);
            var settings = await JsonSerializer.DeserializeAsync<UserSettingsSnapshot>(stream, cancellationToken: cancellationToken);
            return Normalize(settings);
        }
        catch
        {
            return new UserSettingsSnapshot();
        }
    }

    public async Task SaveSettingsAsync(UserSettingsSnapshot settings, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            await using var stream = File.Create(SettingsPath);
            await JsonSerializer.SerializeAsync(stream, Normalize(settings), new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
        }
        catch
        {
        }
    }

    private static UserSettingsSnapshot LoadSettingsSafe()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new UserSettingsSnapshot();
            }

            using var stream = File.OpenRead(SettingsPath);
            return Normalize(JsonSerializer.Deserialize<UserSettingsSnapshot>(stream));
        }
        catch
        {
            return new UserSettingsSnapshot();
        }
    }

    private static UserSettingsSnapshot Normalize(UserSettingsSnapshot? settings)
    {
        settings ??= new UserSettingsSnapshot();
        return settings with { Language = NormalizeLanguage(settings.Language) };
    }

    private static string NormalizeLanguage(string? cultureCode)
    {
        return !string.IsNullOrWhiteSpace(cultureCode) && SupportedCultures.Contains(cultureCode) ? cultureCode : "de";
    }
}
