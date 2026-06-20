using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly IBrush SelectedNavigationBackground = new SolidColorBrush(Color.Parse("#1D2939"));
    private static readonly IBrush DefaultNavigationBackground = Brushes.Transparent;
    private static readonly IBrush SelectedNavigationForeground = Brushes.White;
    private static readonly IBrush DefaultNavigationForeground = new SolidColorBrush(Color.Parse("#D0D5DD"));

    private readonly ISystemInfoService systemInfoService;
    private readonly IScanOrchestrator scanOrchestrator;
    private readonly IReportGenerator reportGenerator;
    private readonly IAutostartActionService autostartActionService;
    private readonly IConfirmationDialogService confirmationDialogService;
    private readonly ILocalizationService localizationService;
    private readonly IDefenderActionService defenderActionService;
    private readonly IQuarantineService quarantineService;
    private readonly IActionHistoryService actionHistoryService;
    private bool isInitializingLanguage;

    public MainWindowViewModel(
        ISystemInfoService systemInfoService,
        IScanOrchestrator scanOrchestrator,
        IReportGenerator reportGenerator,
        IAutostartActionService autostartActionService,
        IConfirmationDialogService confirmationDialogService,
        ILocalizationService localizationService,
        IDefenderActionService defenderActionService,
        IQuarantineService quarantineService,
        IActionHistoryService actionHistoryService)
    {
        this.systemInfoService = systemInfoService;
        this.scanOrchestrator = scanOrchestrator;
        this.reportGenerator = reportGenerator;
        this.autostartActionService = autostartActionService;
        this.confirmationDialogService = confirmationDialogService;
        this.localizationService = localizationService;
        this.defenderActionService = defenderActionService;
        this.quarantineService = quarantineService;
        this.actionHistoryService = actionHistoryService;
        localizationService.CultureChanged += (_, _) => NotifyLocalizedProperties();
        isInitializingLanguage = true;
        SelectedLanguage = SupportedLanguages.FirstOrDefault(language => language.CultureCode == localizationService.CurrentCulture) ?? SupportedLanguages[0];
        isInitializingLanguage = false;
        SelectedFindingFilter = T("Filter_All");
        SelectedStartupFilter = T("Filter_All");

        _ = LoadInitialOverviewAsync();
        _ = LoadProductDataAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OperatingSystemText))]
    [NotifyPropertyChangedFor(nameof(CpuUsageText))]
    [NotifyPropertyChangedFor(nameof(MemoryUsageText))]
    [NotifyPropertyChangedFor(nameof(DiskUsageText))]
    private SystemOverview overview = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RiskLevelText))]
    [NotifyPropertyChangedFor(nameof(FindingsCountText))]
    [NotifyPropertyChangedFor(nameof(Findings))]
    [NotifyPropertyChangedFor(nameof(FilteredFindings))]
    [NotifyPropertyChangedFor(nameof(Processes))]
    [NotifyPropertyChangedFor(nameof(StartupItems))]
    [NotifyPropertyChangedFor(nameof(Services))]
    [NotifyPropertyChangedFor(nameof(NetworkConnections))]
    [NotifyPropertyChangedFor(nameof(DefenderStatus))]
    private ScanResult? scanResult;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartScanCommand))]
    private bool isScanning;

    [ObservableProperty]
    private string statusMessage = "Systemwerte werden geladen...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredFindings))]
    [NotifyPropertyChangedFor(nameof(FindingsCountText))]
    private string selectedFindingFilter = "All";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredStartupItems))]
    private string selectedStartupFilter = "All";

    [ObservableProperty]
    private SupportedLanguage selectedLanguage;

    [ObservableProperty]
    private IReadOnlyList<QuarantineItem> quarantineItems = [];

    [ObservableProperty]
    private IReadOnlyList<ActionHistoryItem> actionHistoryItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverviewVisible))]
    [NotifyPropertyChangedFor(nameof(IsFindingsVisible))]
    [NotifyPropertyChangedFor(nameof(IsProcessesVisible))]
    [NotifyPropertyChangedFor(nameof(IsStartupVisible))]
    [NotifyPropertyChangedFor(nameof(IsSecurityVisible))]
    [NotifyPropertyChangedFor(nameof(IsQuarantineVisible))]
    [NotifyPropertyChangedFor(nameof(IsHistoryVisible))]
    [NotifyPropertyChangedFor(nameof(IsReportsVisible))]
    [NotifyPropertyChangedFor(nameof(IsSettingsVisible))]
    [NotifyPropertyChangedFor(nameof(IsServicesVisible))]
    [NotifyPropertyChangedFor(nameof(IsNetworkVisible))]
    [NotifyPropertyChangedFor(nameof(IsDefenderVisible))]
    [NotifyPropertyChangedFor(nameof(OverviewNavBackground))]
    [NotifyPropertyChangedFor(nameof(FindingsNavBackground))]
    [NotifyPropertyChangedFor(nameof(ProcessesNavBackground))]
    [NotifyPropertyChangedFor(nameof(StartupNavBackground))]
    [NotifyPropertyChangedFor(nameof(ServicesNavBackground))]
    [NotifyPropertyChangedFor(nameof(NetworkNavBackground))]
    [NotifyPropertyChangedFor(nameof(DefenderNavBackground))]
    [NotifyPropertyChangedFor(nameof(DashboardNavBackground))]
    [NotifyPropertyChangedFor(nameof(AutostartNavBackground))]
    [NotifyPropertyChangedFor(nameof(SecurityNavBackground))]
    [NotifyPropertyChangedFor(nameof(QuarantineNavBackground))]
    [NotifyPropertyChangedFor(nameof(HistoryNavBackground))]
    [NotifyPropertyChangedFor(nameof(ReportsNavBackground))]
    [NotifyPropertyChangedFor(nameof(SettingsNavBackground))]
    [NotifyPropertyChangedFor(nameof(OverviewNavForeground))]
    [NotifyPropertyChangedFor(nameof(FindingsNavForeground))]
    [NotifyPropertyChangedFor(nameof(ProcessesNavForeground))]
    [NotifyPropertyChangedFor(nameof(StartupNavForeground))]
    [NotifyPropertyChangedFor(nameof(ServicesNavForeground))]
    [NotifyPropertyChangedFor(nameof(NetworkNavForeground))]
    [NotifyPropertyChangedFor(nameof(DefenderNavForeground))]
    [NotifyPropertyChangedFor(nameof(DashboardNavForeground))]
    [NotifyPropertyChangedFor(nameof(AutostartNavForeground))]
    [NotifyPropertyChangedFor(nameof(SecurityNavForeground))]
    [NotifyPropertyChangedFor(nameof(QuarantineNavForeground))]
    [NotifyPropertyChangedFor(nameof(HistoryNavForeground))]
    [NotifyPropertyChangedFor(nameof(ReportsNavForeground))]
    [NotifyPropertyChangedFor(nameof(SettingsNavForeground))]
    [NotifyPropertyChangedFor(nameof(SelectedSectionTitle))]
    private int selectedSectionIndex;

    public string OperatingSystemText => string.IsNullOrWhiteSpace(Overview.OperatingSystemName) ? "-" : Overview.OperatingSystemName;
    public string CpuUsageText => $"{Overview.CpuUsagePercent:0.0}%";
    public string MemoryUsageText => $"{Overview.MemoryUsagePercent:0.0}%";
    public string DiskUsageText => $"{Overview.DiskUsagePercent:0.0}%";
    public string RiskLevelText => ScanResult?.OverallRiskLevel.ToString() ?? T("Status_NotScanned");
    public string FindingsCountText => ScanResult is null ? "-" : FilteredFindings.Count.ToString();
    public IReadOnlyList<string> FindingFilters => [T("Filter_All"), T("Risk_Critical"), T("Risk_High"), T("Risk_Medium"), T("Risk_Low"), T("Risk_Info")];
    public IReadOnlyList<string> StartupFilters => [T("Filter_All"), T("Filter_Active"), T("Filter_Disabled"), T("Autostart_SystemCritical"), T("Autostart_Optional"), T("Autostart_Unnecessary"), T("Autostart_Suspicious"), T("Autostart_Unknown")];
    public IReadOnlyList<SupportedLanguage> SupportedLanguages => localizationService.GetSupportedLanguages();
    public ObservableCollection<RiskFinding> Findings => ScanResult?.Findings ?? [];
    public IReadOnlyList<RiskFinding> FilteredFindings => GetFilteredFindings();
    public ObservableCollection<ProcessScanItem> Processes => ScanResult?.Processes ?? [];
    public ObservableCollection<StartupItem> StartupItems => ScanResult?.StartupItems ?? [];
    public IReadOnlyList<StartupItem> FilteredStartupItems => GetFilteredStartupItems();
    public ObservableCollection<ServiceScanItem> Services => ScanResult?.Services ?? [];
    public ObservableCollection<NetworkConnectionItem> NetworkConnections => ScanResult?.NetworkConnections ?? [];
    public DefenderStatus DefenderStatus => ScanResult?.DefenderStatus ?? new DefenderStatus { StatusText = T("Status_NotChecked") };
    public bool IsOverviewVisible => SelectedSectionIndex == 0;
    public bool IsStartupVisible => SelectedSectionIndex == 1;
    public bool IsProcessesVisible => SelectedSectionIndex == 2;
    public bool IsSecurityVisible => SelectedSectionIndex == 3;
    public bool IsQuarantineVisible => SelectedSectionIndex == 4;
    public bool IsHistoryVisible => SelectedSectionIndex == 5;
    public bool IsReportsVisible => SelectedSectionIndex == 6;
    public bool IsSettingsVisible => SelectedSectionIndex == 7;
    public bool IsFindingsVisible => false;
    public bool IsServicesVisible => false;
    public bool IsNetworkVisible => false;
    public bool IsDefenderVisible => SelectedSectionIndex == 3;
    public IBrush OverviewNavBackground => GetNavigationBackground(0);
    public IBrush FindingsNavBackground => GetNavigationBackground(1);
    public IBrush ProcessesNavBackground => GetNavigationBackground(2);
    public IBrush StartupNavBackground => GetNavigationBackground(3);
    public IBrush ServicesNavBackground => GetNavigationBackground(4);
    public IBrush NetworkNavBackground => GetNavigationBackground(5);
    public IBrush DefenderNavBackground => GetNavigationBackground(6);
    public IBrush DashboardNavBackground => GetNavigationBackground(0);
    public IBrush AutostartNavBackground => GetNavigationBackground(1);
    public IBrush SecurityNavBackground => GetNavigationBackground(3);
    public IBrush QuarantineNavBackground => GetNavigationBackground(4);
    public IBrush HistoryNavBackground => GetNavigationBackground(5);
    public IBrush ReportsNavBackground => GetNavigationBackground(6);
    public IBrush SettingsNavBackground => GetNavigationBackground(7);
    public IBrush OverviewNavForeground => GetNavigationForeground(0);
    public IBrush FindingsNavForeground => GetNavigationForeground(1);
    public IBrush ProcessesNavForeground => GetNavigationForeground(2);
    public IBrush StartupNavForeground => GetNavigationForeground(3);
    public IBrush ServicesNavForeground => GetNavigationForeground(4);
    public IBrush NetworkNavForeground => GetNavigationForeground(5);
    public IBrush DefenderNavForeground => GetNavigationForeground(6);
    public IBrush DashboardNavForeground => GetNavigationForeground(0);
    public IBrush AutostartNavForeground => GetNavigationForeground(1);
    public IBrush SecurityNavForeground => GetNavigationForeground(3);
    public IBrush QuarantineNavForeground => GetNavigationForeground(4);
    public IBrush HistoryNavForeground => GetNavigationForeground(5);
    public IBrush ReportsNavForeground => GetNavigationForeground(6);
    public IBrush SettingsNavForeground => GetNavigationForeground(7);
    public string SelectedSectionTitle => SelectedSectionIndex switch
    {
        0 => T("Nav_Dashboard"),
        1 => T("Nav_Autostart"),
        2 => T("Nav_Processes"),
        3 => T("Nav_Security"),
        4 => T("Nav_Quarantine"),
        5 => T("Nav_History"),
        6 => T("Nav_Reports"),
        7 => T("Nav_Settings"),
        _ => T("Nav_Dashboard")
    };
    public string AppTitle => T("App_Title");
    public string AppSubtitle => T("App_Subtitle");
    public string SidebarSectionsText => T("Sidebar_Sections");
    public string LocalAnalysisText => T("Sidebar_LocalAnalysis");
    public string NoFilesChangedText => T("Sidebar_NoFilesChanged");
    public string NavDashboard => T("Nav_Dashboard");
    public string NavAutostart => T("Nav_Autostart");
    public string NavProcesses => T("Nav_Processes");
    public string NavSecurity => T("Nav_Security");
    public string NavQuarantine => T("Nav_Quarantine");
    public string NavHistory => T("Nav_History");
    public string NavReports => T("Nav_Reports");
    public string NavSettings => T("Nav_Settings");
    public string StartScanText => T("Dashboard_StartScan");
    public string StartScanSubtitleText => T("Dashboard_StartScanSubtitle");
    public string ExportReportText => T("Dashboard_ExportReport");
    public string ExportReportSubtitleText => T("Dashboard_ExportReportSubtitle");
    public string DisclaimerText => T("Disclaimer_NotAntivirus");
    public string OsText => T("Dashboard_OS");
    public string CpuUsageLabel => T("Dashboard_CpuUsage");
    public string RamUsageLabel => T("Dashboard_RamUsage");
    public string DiskUsageLabel => T("Dashboard_DiskUsage");
    public string FindingsCountLabel => T("Dashboard_FindingsCount");
    public string GeneralStatusText => T("Dashboard_TotalStatus");
    public string OperatingSystemLabel => T("Dashboard_OperatingSystem");
    public string DefenderLabel => T("Security_Defender");
    public string NotScannedText => T("Status_NotScanned");
    public string NotCheckedText => T("Status_NotChecked");
    public string SettingsLanguageText => T("Settings_Language");
    public string SettingsPrivacyText => T("Settings_Privacy");
    public string SettingsIntroText => T("Settings_Intro");
    public string SettingsLanguageHintText => T("Settings_LanguageHint");
    public string CopyPathHintText => T("Autostart_CopyPathHint");
    public string PrivacyLocalAnalysisText => T("Settings_PrivacyLocalAnalysis");
    public string PrivacyNoAutomaticUploadText => T("Settings_PrivacyNoAutomaticUpload");
    public string PrivacyOptionalHashCheckText => T("Settings_PrivacyOptionalHashCheck");
    public string UserFriendlyRiskText => ScanResult?.OverallRiskLevel switch
    {
        RiskLevel.High or RiskLevel.Critical => T("Risk_CriticalCheckRecommended"),
        RiskLevel.Medium => T("Risk_AttentionRequired"),
        _ => T("Risk_Good")
    };
    public string FilterText => T("Common_Filter");
    public string RiskFilterText => T("Findings_RiskFilter");
    public string RiskText => T("Common_Risk");
    public string CategoryText => T("Common_Category");
    public string NameText => T("Common_Name");
    public string PathText => T("Common_Path");
    public string PublisherText => T("Common_Publisher");
    public string StatusText => T("Common_Status");
    public string RecommendationText => T("Common_Recommendation");
    public string ActionsText => T("Common_Actions");
    public string RunningText => T("Common_Running");
    public string ReasonText => T("Common_Reason");
    public string StartupTotalLabel => T("Autostart_Total");
    public string StartupActiveLabel => T("Autostart_Active");
    public string StartupDisabledLabel => T("Autostart_Disabled");
    public string StartupOptionalLabel => T("Autostart_Optional");
    public string StartupSuspiciousLabel => T("Autostart_Suspicious");
    public string StartupEssentialLabel => T("Autostart_SystemCritical");
    public string StartupActivateText => T("Autostart_Activate");
    public string StartupDeactivateText => T("Autostart_Deactivate");
    public string StartupStopProcessText => T("Autostart_StopProcess");
    public string StartupOpenFileLocationText => T("Autostart_OpenFileLocationShort");
    public string StartupDefenderScanText => T("Autostart_StartDefenderScanShort");
    public string StartupActivateTooltipText => T("Autostart_ActivateTooltip");
    public string StartupDeactivateTooltipText => T("Autostart_DeactivateTooltip");
    public string StartupStopProcessTooltipText => T("Autostart_StopProcessTooltip");
    public string StartupOpenFileLocationTooltipText => T("Autostart_OpenFileLocationTooltip");
    public string StartupDefenderScanTooltipText => T("Autostart_StartDefenderScanTooltip");
    public string RealtimeProtectionText => T("Security_RealtimeProtection");
    public string AntivirusText => T("Security_Antivirus");
    public string NoteText => T("Common_Note");
    public string HistoryCountLabel => T("History_Count");
    public string QuarantineIntroText => T("Quarantine_Intro");
    public string QuarantineEmptyText => T("Quarantine_Empty");
    public string HistoryIntroText => T("History_Intro");
    public string HistoryEmptyText => T("History_Empty");
    public string ReportsIntroText => T("Reports_Intro");
    public string ReportsEmptyText => T("Reports_Empty");
    public string StartupTotalText => StartupItems.Count.ToString();
    public string StartupActiveText => StartupItems.Count(item => item.IsEnabled).ToString();
    public string StartupDisabledText => StartupItems.Count(item => !item.IsEnabled).ToString();
    public string StartupOptionalText => StartupItems.Count(item => item.StartupClassification == StartupClassification.Optional).ToString();
    public string StartupSuspiciousText => StartupItems.Count(item => item.StartupClassification == StartupClassification.Suspicious).ToString();
    public string StartupEssentialText => StartupItems.Count(item => item.StartupClassification == StartupClassification.Essential).ToString();
    public string QuarantineCountText => QuarantineItems.Count.ToString();
    public string HistoryCountText => ActionHistoryItems.Count.ToString();
    public bool IsQuarantineEmpty => QuarantineItems.Count == 0;
    public bool IsHistoryEmpty => ActionHistoryItems.Count == 0;

    private IReadOnlyList<RiskFinding> GetFilteredFindings()
    {
        var findings = ScanResult?.Findings ?? [];
        return SelectedFindingFilter switch
        {
            var value when value == T("Risk_Critical") => findings.Where(finding => finding.Level == RiskLevel.Critical).ToList(),
            var value when value == T("Risk_High") => findings.Where(finding => finding.Level == RiskLevel.High).ToList(),
            var value when value == T("Risk_Medium") => findings.Where(finding => finding.Level == RiskLevel.Medium).ToList(),
            var value when value == T("Risk_Low") => findings.Where(finding => finding.Level == RiskLevel.Low).ToList(),
            var value when value == T("Risk_Info") => findings.Where(finding => finding.Level == RiskLevel.Info || finding.Level == RiskLevel.None).ToList(),
            _ => findings.Where(finding => finding.Level is not RiskLevel.Info and not RiskLevel.None).ToList()
        };
    }

    private IReadOnlyList<StartupItem> GetFilteredStartupItems()
    {
        return SelectedStartupFilter switch
        {
            var value when value == T("Filter_Active") => StartupItems.Where(item => item.IsEnabled).ToList(),
            var value when value == T("Filter_Disabled") => StartupItems.Where(item => !item.IsEnabled).ToList(),
            var value when value == T("Autostart_SystemCritical") => StartupItems.Where(item => item.StartupClassification == StartupClassification.Essential).ToList(),
            var value when value == T("Autostart_Optional") => StartupItems.Where(item => item.StartupClassification == StartupClassification.Optional).ToList(),
            var value when value == T("Autostart_Unnecessary") => StartupItems.Where(item => item.StartupClassification == StartupClassification.Unnecessary).ToList(),
            var value when value == T("Autostart_Suspicious") => StartupItems.Where(item => item.StartupClassification == StartupClassification.Suspicious).ToList(),
            var value when value == T("Autostart_Unknown") => StartupItems.Where(item => item.StartupClassification == StartupClassification.Unknown).ToList(),
            _ => StartupItems.ToList()
        };
    }

    private IBrush GetNavigationBackground(int index)
    {
        return SelectedSectionIndex == index ? SelectedNavigationBackground : DefaultNavigationBackground;
    }

    private IBrush GetNavigationForeground(int index)
    {
        return SelectedSectionIndex == index ? SelectedNavigationForeground : DefaultNavigationForeground;
    }

    [RelayCommand]
    private void SelectSection(string sectionIndex)
    {
        if (int.TryParse(sectionIndex, out var index) && index is >= 0 and <= 7)
        {
            SelectedSectionIndex = index;
        }
    }

    partial void OnSelectedLanguageChanged(SupportedLanguage value)
    {
        if (value is null || isInitializingLanguage)
        {
            return;
        }

        try
        {
            localizationService.SetCulture(value.CultureCode);
            StatusMessage = T("Settings_LanguageChanged");
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
    }

    private string T(string key)
    {
        return localizationService.GetString(key);
    }

    private void NotifyLocalizedProperties()
    {
        OnPropertyChanged(nameof(AppTitle));
        OnPropertyChanged(nameof(AppSubtitle));
        OnPropertyChanged(nameof(SidebarSectionsText));
        OnPropertyChanged(nameof(LocalAnalysisText));
        OnPropertyChanged(nameof(NoFilesChangedText));
        OnPropertyChanged(nameof(NavDashboard));
        OnPropertyChanged(nameof(NavAutostart));
        OnPropertyChanged(nameof(NavProcesses));
        OnPropertyChanged(nameof(NavSecurity));
        OnPropertyChanged(nameof(NavQuarantine));
        OnPropertyChanged(nameof(NavHistory));
        OnPropertyChanged(nameof(NavReports));
        OnPropertyChanged(nameof(NavSettings));
        OnPropertyChanged(nameof(StartScanText));
        OnPropertyChanged(nameof(StartScanSubtitleText));
        OnPropertyChanged(nameof(ExportReportText));
        OnPropertyChanged(nameof(ExportReportSubtitleText));
        OnPropertyChanged(nameof(DisclaimerText));
        OnPropertyChanged(nameof(OsText));
        OnPropertyChanged(nameof(CpuUsageLabel));
        OnPropertyChanged(nameof(RamUsageLabel));
        OnPropertyChanged(nameof(DiskUsageLabel));
        OnPropertyChanged(nameof(FindingsCountLabel));
        OnPropertyChanged(nameof(GeneralStatusText));
        OnPropertyChanged(nameof(OperatingSystemLabel));
        OnPropertyChanged(nameof(DefenderLabel));
        OnPropertyChanged(nameof(NotScannedText));
        OnPropertyChanged(nameof(NotCheckedText));
        OnPropertyChanged(nameof(SettingsLanguageText));
        OnPropertyChanged(nameof(SettingsPrivacyText));
        OnPropertyChanged(nameof(SettingsIntroText));
        OnPropertyChanged(nameof(SettingsLanguageHintText));
        OnPropertyChanged(nameof(CopyPathHintText));
        OnPropertyChanged(nameof(PrivacyLocalAnalysisText));
        OnPropertyChanged(nameof(PrivacyNoAutomaticUploadText));
        OnPropertyChanged(nameof(PrivacyOptionalHashCheckText));
        OnPropertyChanged(nameof(SelectedSectionTitle));
        OnPropertyChanged(nameof(UserFriendlyRiskText));
        OnPropertyChanged(nameof(RiskLevelText));
        OnPropertyChanged(nameof(FindingFilters));
        OnPropertyChanged(nameof(StartupFilters));
        OnPropertyChanged(nameof(FilterText));
        OnPropertyChanged(nameof(RiskFilterText));
        OnPropertyChanged(nameof(RiskText));
        OnPropertyChanged(nameof(CategoryText));
        OnPropertyChanged(nameof(NameText));
        OnPropertyChanged(nameof(PathText));
        OnPropertyChanged(nameof(PublisherText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(RecommendationText));
        OnPropertyChanged(nameof(ActionsText));
        OnPropertyChanged(nameof(RunningText));
        OnPropertyChanged(nameof(ReasonText));
        OnPropertyChanged(nameof(StartupTotalLabel));
        OnPropertyChanged(nameof(StartupActiveLabel));
        OnPropertyChanged(nameof(StartupDisabledLabel));
        OnPropertyChanged(nameof(StartupOptionalLabel));
        OnPropertyChanged(nameof(StartupSuspiciousLabel));
        OnPropertyChanged(nameof(StartupEssentialLabel));
        OnPropertyChanged(nameof(StartupActivateText));
        OnPropertyChanged(nameof(StartupDeactivateText));
        OnPropertyChanged(nameof(StartupStopProcessText));
        OnPropertyChanged(nameof(StartupOpenFileLocationText));
        OnPropertyChanged(nameof(StartupDefenderScanText));
        OnPropertyChanged(nameof(StartupActivateTooltipText));
        OnPropertyChanged(nameof(StartupDeactivateTooltipText));
        OnPropertyChanged(nameof(StartupStopProcessTooltipText));
        OnPropertyChanged(nameof(StartupOpenFileLocationTooltipText));
        OnPropertyChanged(nameof(StartupDefenderScanTooltipText));
        OnPropertyChanged(nameof(RealtimeProtectionText));
        OnPropertyChanged(nameof(AntivirusText));
        OnPropertyChanged(nameof(NoteText));
        OnPropertyChanged(nameof(HistoryCountLabel));
        OnPropertyChanged(nameof(QuarantineIntroText));
        OnPropertyChanged(nameof(QuarantineEmptyText));
        OnPropertyChanged(nameof(HistoryIntroText));
        OnPropertyChanged(nameof(HistoryEmptyText));
        OnPropertyChanged(nameof(ReportsIntroText));
        OnPropertyChanged(nameof(ReportsEmptyText));
    }

    private async Task LoadProductDataAsync()
    {
        QuarantineItems = await quarantineService.GetItemsAsync(CancellationToken.None);
        ActionHistoryItems = await actionHistoryService.GetHistoryAsync(CancellationToken.None);
        OnPropertyChanged(nameof(QuarantineCountText));
        OnPropertyChanged(nameof(HistoryCountText));
        OnPropertyChanged(nameof(IsQuarantineEmpty));
        OnPropertyChanged(nameof(IsHistoryEmpty));
    }

    private async Task LoadInitialOverviewAsync()
    {
        try
        {
            Overview = await systemInfoService.GetOverviewAsync(CancellationToken.None);
            StatusMessage = T("Status_Ready");
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Status_SystemValuesFailed", ex.Message);
        }
    }

    partial void OnScanResultChanged(ScanResult? value)
    {
        if (value is not null)
        {
            Overview = value.Overview;
            NotifyStartupChanged();
        }
    }

    private void NotifyStartupChanged()
    {
        OnPropertyChanged(nameof(StartupItems));
        OnPropertyChanged(nameof(FilteredStartupItems));
        OnPropertyChanged(nameof(StartupTotalText));
        OnPropertyChanged(nameof(StartupActiveText));
        OnPropertyChanged(nameof(StartupDisabledText));
        OnPropertyChanged(nameof(StartupOptionalText));
        OnPropertyChanged(nameof(StartupSuspiciousText));
        OnPropertyChanged(nameof(StartupEssentialText));
    }

    private bool CanStartScan()
    {
        return !IsScanning;
    }

    [RelayCommand(CanExecute = nameof(CanStartScan))]
    private async Task StartScanAsync()
    {
        IsScanning = true;
        StatusMessage = T("Status_ScanRunning");

        try
        {
            ScanResult = await scanOrchestrator.RunScanAsync(CancellationToken.None);
            StatusMessage = localizationService.GetString("Status_ScanCompleted", ScanResult.Findings.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = T("Status_ScanCanceled");
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Status_ScanFailed", ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task ExportHtmlReportAsync()
    {
        if (ScanResult is null)
        {
            StatusMessage = T("Status_StartScanFirst");
            return;
        }

        try
        {
            var path = await reportGenerator.ExportHtmlAsync(ScanResult, CancellationToken.None);
            StatusMessage = localizationService.GetString("Reports_ReportCreated", path);
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Reports_ReportFailed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EnableStartupAsync(StartupItem item)
    {
        await ExecuteStartupActionAsync(
            item,
            T("Action_EnableStartup_Title"),
            localizationService.GetString("Action_EnableStartup_Confirm", item.Name),
            autostartActionService.EnableAsync);
    }

    [RelayCommand]
    private async Task DisableStartupAsync(StartupItem item)
    {
        await ExecuteStartupActionAsync(
            item,
            T("Action_DisableStartup_Title"),
            localizationService.GetString("Action_DisableStartup_Confirm", item.Name),
            autostartActionService.DisableAsync);
    }

    [RelayCommand]
    private async Task StopStartupProcessAsync(StartupItem item)
    {
        await ExecuteStartupActionAsync(
            item,
            T("Action_StopProcess_Title"),
            localizationService.GetString("Action_StopProcess_Confirm", item.Name),
            autostartActionService.StopProcessAsync);
    }

    [RelayCommand]
    private async Task OpenStartupLocationAsync(StartupItem item)
    {
        await ExecuteStartupActionAsync(
            item,
            T("Action_OpenLocation_Title"),
            localizationService.GetString("Action_OpenLocation_Confirm", item.Name),
            autostartActionService.OpenFileLocationAsync);
    }

    [RelayCommand]
    private async Task DefenderScanStartupAsync(StartupItem item)
    {
        await ExecuteStartupActionAsync(
            item,
            T("Action_DefenderScan_Title"),
            T("Action_DefenderScan_Confirm"),
            autostartActionService.StartDefenderQuickScanAsync);
    }

    private async Task ExecuteStartupActionAsync(
        StartupItem item,
        string title,
        string confirmation,
        Func<StartupItem, CancellationToken, Task<StartupActionResult>> action)
    {
        if (!await confirmationDialogService.ConfirmAsync(title, confirmation, CancellationToken.None))
        {
            return;
        }

        try
        {
            var result = await action(item, CancellationToken.None);
            StatusMessage = LocalizeResultMessage(result.Message);
            if (result.UpdatedItem is not null)
            {
                ReplaceStartupItem(result.UpdatedItem);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
    }

    private string LocalizeResultMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return T("Common_Error");
        }

        var localized = T(message);
        return localized == message ? message : localized;
    }

    private void ReplaceStartupItem(StartupItem updatedItem)
    {
        if (ScanResult is null)
        {
            return;
        }

        var index = ScanResult.StartupItems.ToList().FindIndex(item => item.Id.Equals(updatedItem.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            ScanResult.StartupItems[index] = updatedItem;
            NotifyStartupChanged();
        }
    }
}
