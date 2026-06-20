using System.Globalization;
using System.Resources;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class LocalizationService : ILocalizationService
{
    private readonly IUserSettingsService userSettingsService;
    private readonly ResourceManager resourceManager = new("MyPcGuard.Resources.Strings", typeof(LocalizationService).Assembly);
    private readonly IReadOnlyList<SupportedLanguage> supportedLanguages =
    [
        new("de", "Deutsch", "Deutsch"),
        new("en", "English", "English"),
        new("tr", "Turkish", "Türkçe")
    ];

    public LocalizationService(IUserSettingsService userSettingsService)
    {
        this.userSettingsService = userSettingsService;
        CurrentCulture = userSettingsService.GetLanguage();
        CultureInfo.CurrentUICulture = new CultureInfo(CurrentCulture);
    }

    public string CurrentCulture { get; private set; }
    public event EventHandler? CultureChanged;

    public string GetString(string key)
    {
        try
        {
            return resourceManager.GetString(key, new CultureInfo(CurrentCulture))
                ?? resourceManager.GetString(key, new CultureInfo("de"))
                ?? key;
        }
        catch
        {
            return key;
        }
    }

    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(CultureInfo.CurrentUICulture, format, args);
        }
        catch
        {
            return format;
        }
    }

    public void SetCulture(string cultureCode)
    {
        if (!supportedLanguages.Any(language => language.CultureCode.Equals(cultureCode, StringComparison.OrdinalIgnoreCase)))
        {
            cultureCode = "de";
        }

        if (CurrentCulture.Equals(cultureCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentCulture = cultureCode;
        CultureInfo.CurrentUICulture = new CultureInfo(cultureCode);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(cultureCode);

        _ = Task.Run(() => userSettingsService.SaveLanguageAsync(cultureCode, CancellationToken.None));

        try
        {
            CultureChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
        }
    }

    public IReadOnlyList<SupportedLanguage> GetSupportedLanguages()
    {
        return supportedLanguages;
    }
}
