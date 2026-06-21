using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;
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
    private readonly IInstalledProgramScanner installedProgramScanner;
    private readonly IInstalledProgramActionService installedProgramActionService;
    private readonly IUninstallCleanupPlanner uninstallCleanupPlanner;
    private readonly IProcessScanner processScanner;
    private readonly IHardwareInfoService hardwareInfoService;
    private readonly IDeviceDriverScanner deviceDriverScanner;
    private readonly IAgentOrchestrator agentOrchestrator;
    private readonly IAgentMemoryService agentMemoryService;
    private readonly IAgentPolicyService agentPolicyService;
    private readonly IUserSettingsService userSettingsService;
    private bool isInitializingLanguage;
    private bool isInitializingAgentSettings;
    private bool arePassiveListsLoaded;
    private bool areProgramsLoaded;
    private bool areProcessesLoaded;

    public MainWindowViewModel(
        ISystemInfoService systemInfoService,
        IScanOrchestrator scanOrchestrator,
        IReportGenerator reportGenerator,
        IAutostartActionService autostartActionService,
        IConfirmationDialogService confirmationDialogService,
        ILocalizationService localizationService,
        IDefenderActionService defenderActionService,
        IQuarantineService quarantineService,
        IActionHistoryService actionHistoryService,
        IInstalledProgramScanner installedProgramScanner,
        IInstalledProgramActionService installedProgramActionService,
        IUninstallCleanupPlanner uninstallCleanupPlanner,
        IProcessScanner processScanner,
        IHardwareInfoService hardwareInfoService,
        IDeviceDriverScanner deviceDriverScanner,
        IAgentOrchestrator agentOrchestrator,
        IAgentMemoryService agentMemoryService,
        IAgentPolicyService agentPolicyService,
        IUserSettingsService userSettingsService)
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
        this.installedProgramScanner = installedProgramScanner;
        this.installedProgramActionService = installedProgramActionService;
        this.uninstallCleanupPlanner = uninstallCleanupPlanner;
        this.processScanner = processScanner;
        this.hardwareInfoService = hardwareInfoService;
        this.deviceDriverScanner = deviceDriverScanner;
        this.agentOrchestrator = agentOrchestrator;
        this.agentMemoryService = agentMemoryService;
        this.agentPolicyService = agentPolicyService;
        this.userSettingsService = userSettingsService;
        localizationService.CultureChanged += (_, _) => NotifyLocalizedProperties();
        isInitializingLanguage = true;
        SelectedLanguage = SupportedLanguages.FirstOrDefault(language => language.CultureCode == localizationService.CurrentCulture) ?? SupportedLanguages[0];
        isInitializingLanguage = false;
        SelectedFindingFilter = T("Filter_All");
        SelectedStartupFilter = T("Filter_All");

        _ = LoadInitialOverviewAsync();
        _ = LoadPassiveListsAsync();
        _ = LoadAgentSettingsAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OperatingSystemText))]
    [NotifyPropertyChangedFor(nameof(CpuUsageText))]
    [NotifyPropertyChangedFor(nameof(MemoryUsageText))]
    [NotifyPropertyChangedFor(nameof(DiskUsageText))]
    [NotifyPropertyChangedFor(nameof(PerformanceHealthText))]
    [NotifyPropertyChangedFor(nameof(PerformanceHealthSummaryText))]
    [NotifyPropertyChangedFor(nameof(PerformanceHealthBrush))]
    [NotifyPropertyChangedFor(nameof(PerformanceCpuStatusText))]
    [NotifyPropertyChangedFor(nameof(PerformanceMemoryStatusText))]
    [NotifyPropertyChangedFor(nameof(PerformanceDiskStatusText))]
    private SystemOverview overview = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RiskLevelText))]
    [NotifyPropertyChangedFor(nameof(FindingsCountText))]
    [NotifyPropertyChangedFor(nameof(DashboardSecuritySummaryText))]
    [NotifyPropertyChangedFor(nameof(DashboardPerformanceSummaryText))]
    [NotifyPropertyChangedFor(nameof(SecurityHealthText))]
    [NotifyPropertyChangedFor(nameof(SecurityHealthSummaryText))]
    [NotifyPropertyChangedFor(nameof(SecurityHealthBrush))]
    [NotifyPropertyChangedFor(nameof(SecurityScanStateText))]
    [NotifyPropertyChangedFor(nameof(SecurityActionableFindingsText))]
    [NotifyPropertyChangedFor(nameof(SecurityHighFindingsText))]
    [NotifyPropertyChangedFor(nameof(SecurityTopFindings))]
    [NotifyPropertyChangedFor(nameof(HasSecurityTopFindings))]
    [NotifyPropertyChangedFor(nameof(SecurityNoFindingsVisible))]
    [NotifyPropertyChangedFor(nameof(SecurityStartScanHintVisible))]
    [NotifyPropertyChangedFor(nameof(SecurityDefenderSummaryText))]
    [NotifyPropertyChangedFor(nameof(PerformanceHealthText))]
    [NotifyPropertyChangedFor(nameof(PerformanceHealthSummaryText))]
    [NotifyPropertyChangedFor(nameof(PerformanceHealthBrush))]
    [NotifyPropertyChangedFor(nameof(PerformanceStartupImpactText))]
    [NotifyPropertyChangedFor(nameof(Findings))]
    [NotifyPropertyChangedFor(nameof(FilteredFindings))]
    [NotifyPropertyChangedFor(nameof(Processes))]
    [NotifyPropertyChangedFor(nameof(IsProcessesEmpty))]
    [NotifyPropertyChangedFor(nameof(StartupItems))]
    [NotifyPropertyChangedFor(nameof(FilteredStartupRows))]
    [NotifyPropertyChangedFor(nameof(Services))]
    [NotifyPropertyChangedFor(nameof(NetworkConnections))]
    [NotifyPropertyChangedFor(nameof(DefenderStatus))]
    [NotifyPropertyChangedFor(nameof(DefenderStatusText))]
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
    [NotifyPropertyChangedFor(nameof(FilteredStartupRows))]
    private string selectedStartupFilter = "All";

    [ObservableProperty]
    private SupportedLanguage selectedLanguage;

    [ObservableProperty]
    private IReadOnlyList<QuarantineItem> quarantineItems = [];

    [ObservableProperty]
    private IReadOnlyList<ActionHistoryItem> actionHistoryItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAgentResults))]
    private IReadOnlyList<AgentResult> agentResults = [];

    [ObservableProperty]
    private string agentStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActionPlanVisible))]
    private ActionPlan? selectedActionPlan;

    [ObservableProperty]
    private AgentResult? selectedAgentResult;

    [ObservableProperty]
    private bool agentRecommendationsEnabled = true;

    [ObservableProperty]
    private bool onlineResearchAllowed;

    [ObservableProperty]
    private bool officialSourcesOnly = true;

    [ObservableProperty]
    private bool rememberIgnoredRecommendations = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DashboardProgramsSummaryText))]
    private IReadOnlyList<InstalledProgramItem> installedPrograms = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUninstallCleanupPlan))]
    private UninstallCleanupPlan? uninstallCleanupPlan;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Processes))]
    [NotifyPropertyChangedFor(nameof(IsProcessesEmpty))]
    private ObservableCollection<ProcessScanItem> liveProcesses = [];

    [ObservableProperty]
    private HardwareInfo hardwareInfo = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDriverDevices))]
    [NotifyPropertyChangedFor(nameof(DriverProblemDevices))]
    [NotifyPropertyChangedFor(nameof(HasDriverProblemDevices))]
    private IReadOnlyList<DriverIssue> driverDevices = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BenchmarkDiskWriteText))]
    [NotifyPropertyChangedFor(nameof(BenchmarkDiskReadText))]
    [NotifyPropertyChangedFor(nameof(BenchmarkMemoryCopyText))]
    private PerformanceBenchmarkResult? benchmarkResult;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunQuickBenchmarkCommand))]
    private bool isBenchmarkRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverviewVisible))]
    [NotifyPropertyChangedFor(nameof(IsFindingsVisible))]
    [NotifyPropertyChangedFor(nameof(IsProcessesVisible))]
    [NotifyPropertyChangedFor(nameof(IsSystemVisible))]
    [NotifyPropertyChangedFor(nameof(IsStartupVisible))]
    [NotifyPropertyChangedFor(nameof(IsProgramsVisible))]
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
    [NotifyPropertyChangedFor(nameof(ProgramsNavBackground))]
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
    [NotifyPropertyChangedFor(nameof(ProgramsNavForeground))]
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
    public string RiskLevelText => ScanResult is null ? T("Status_NotScanned") : LocalizeRiskLevel(ScanResult.OverallRiskLevel);
    public string FindingsCountText => ScanResult is null ? "-" : CountActionableFindings().ToString();
    public IReadOnlyList<string> FindingFilters => [T("Filter_All"), T("Risk_Critical"), T("Risk_High"), T("Risk_Medium"), T("Risk_Low"), T("Risk_Info")];
    public IReadOnlyList<string> StartupFilters => [T("Filter_All"), T("Filter_Active"), T("Filter_Disabled"), T("Autostart_SystemCritical"), T("Autostart_Optional"), T("Autostart_Unnecessary"), T("Autostart_Suspicious"), T("Autostart_Unknown")];
    public IReadOnlyList<SupportedLanguage> SupportedLanguages => localizationService.GetSupportedLanguages();
    public ObservableCollection<RiskFinding> Findings => ScanResult?.Findings ?? [];
    public IReadOnlyList<RiskFinding> FilteredFindings => GetFilteredFindings();
    public ObservableCollection<ProcessScanItem> Processes => ScanResult?.Processes ?? LiveProcesses;
    public ObservableCollection<StartupItem> StartupItems => ScanResult?.StartupItems ?? [];
    public IReadOnlyList<StartupItem> FilteredStartupItems => GetFilteredStartupItems();
    public IReadOnlyList<StartupItemDisplay> FilteredStartupRows => GetFilteredStartupItems().Select(CreateStartupDisplay).ToList();
    public ObservableCollection<ServiceScanItem> Services => ScanResult?.Services ?? [];
    public ObservableCollection<NetworkConnectionItem> NetworkConnections => ScanResult?.NetworkConnections ?? [];
    public DefenderStatus DefenderStatus => ScanResult?.DefenderStatus ?? new DefenderStatus { StatusText = T("Status_NotChecked") };
    public bool IsOverviewVisible => SelectedSectionIndex == 0;
    public bool IsStartupVisible => SelectedSectionIndex == 1;
    public bool IsSystemVisible => SelectedSectionIndex == 2;
    public bool IsProcessesVisible => false;
    public bool IsProgramsVisible => SelectedSectionIndex == 3;
    public bool IsSecurityVisible => SelectedSectionIndex == 4;
    public bool IsQuarantineVisible => SelectedSectionIndex == 5;
    public bool IsHistoryVisible => SelectedSectionIndex == 6;
    public bool IsReportsVisible => SelectedSectionIndex == 7;
    public bool IsSettingsVisible => SelectedSectionIndex == 8;
    public bool IsFindingsVisible => false;
    public bool IsServicesVisible => false;
    public bool IsNetworkVisible => false;
    public bool IsDefenderVisible => SelectedSectionIndex == 4;
    public IBrush OverviewNavBackground => GetNavigationBackground(0);
    public IBrush FindingsNavBackground => GetNavigationBackground(1);
    public IBrush ProcessesNavBackground => GetNavigationBackground(2);
    public IBrush StartupNavBackground => GetNavigationBackground(3);
    public IBrush ServicesNavBackground => GetNavigationBackground(4);
    public IBrush NetworkNavBackground => GetNavigationBackground(5);
    public IBrush DefenderNavBackground => GetNavigationBackground(6);
    public IBrush DashboardNavBackground => GetNavigationBackground(0);
    public IBrush AutostartNavBackground => GetNavigationBackground(1);
    public IBrush ProgramsNavBackground => GetNavigationBackground(3);
    public IBrush SecurityNavBackground => GetNavigationBackground(4);
    public IBrush QuarantineNavBackground => GetNavigationBackground(5);
    public IBrush HistoryNavBackground => GetNavigationBackground(6);
    public IBrush ReportsNavBackground => GetNavigationBackground(7);
    public IBrush SettingsNavBackground => GetNavigationBackground(8);
    public IBrush OverviewNavForeground => GetNavigationForeground(0);
    public IBrush FindingsNavForeground => GetNavigationForeground(1);
    public IBrush ProcessesNavForeground => GetNavigationForeground(2);
    public IBrush StartupNavForeground => GetNavigationForeground(3);
    public IBrush ServicesNavForeground => GetNavigationForeground(4);
    public IBrush NetworkNavForeground => GetNavigationForeground(5);
    public IBrush DefenderNavForeground => GetNavigationForeground(6);
    public IBrush DashboardNavForeground => GetNavigationForeground(0);
    public IBrush AutostartNavForeground => GetNavigationForeground(1);
    public IBrush ProgramsNavForeground => GetNavigationForeground(3);
    public IBrush SecurityNavForeground => GetNavigationForeground(4);
    public IBrush QuarantineNavForeground => GetNavigationForeground(5);
    public IBrush HistoryNavForeground => GetNavigationForeground(6);
    public IBrush ReportsNavForeground => GetNavigationForeground(7);
    public IBrush SettingsNavForeground => GetNavigationForeground(8);
    public string SelectedSectionTitle => SelectedSectionIndex switch
    {
        0 => T("Nav_Dashboard"),
        1 => T("Nav_Cleanup"),
        2 => T("Nav_System"),
        3 => T("Nav_Programs"),
        4 => T("Nav_Security"),
        5 => T("Nav_Quarantine"),
        6 => T("Nav_History"),
        7 => T("Nav_Reports"),
        8 => T("Nav_Settings"),
        _ => T("Nav_Dashboard")
    };
    public string AppTitle => T("App_Title");
    public string AppSubtitle => T("App_Subtitle");
    public string SidebarSectionsText => T("Sidebar_Sections");
    public string LocalAnalysisText => T("Sidebar_LocalAnalysis");
    public string NoFilesChangedText => T("Sidebar_NoFilesChanged");
    public string NavDashboard => T("Nav_Dashboard");
    public string NavAutostart => T("Nav_Cleanup");
    public string NavProcesses => T("Nav_System");
    public string ProcessesTitleText => T("Nav_Processes");
    public string NavPrograms => T("Nav_Programs");
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
    public string DashboardSecurityTitleText => T("Dashboard_SecurityProtection");
    public string DashboardPerformanceTitleText => T("Dashboard_PerformanceOptimization");
    public string DashboardProgramsTitleText => T("Dashboard_ProgramsControl");
    public string DashboardSecuritySummaryText => GetDashboardSecuritySummary();
    public string DashboardPerformanceSummaryText => GetDashboardPerformanceSummary();
    public string DashboardProgramsSummaryText => localizationService.GetString("Dashboard_ProgramsSummary", InstalledPrograms.Count);
    public string AgentRecommendationsText => T("Agent_Recommendations");
    public string AgentConfidenceText => T("Agent_Confidence");
    public string AgentReasonText => T("Agent_Reason");
    public string AgentCreateActionPlanText => T("Agent_CreateActionPlan");
    public string AgentIgnoreText => T("Agent_Ignore");
    public string AgentRemindLaterText => T("Agent_RemindLater");
    public string AgentExecuteAfterApprovalText => T("Agent_ExecuteAfterApproval");
    public string AgentNoAutomaticChangesText => T("Agent_NoAutomaticChanges");
    public string AgentRequiresApprovalText => T("Agent_RequiresApproval");
    public string AgentEmptyText => T("Agent_Empty");
    public string AgentDriverPolicyText => T("WebResearch_NoAutomaticDownloads");
    public string AgentSettingsTitleText => T("AgentSettings_Title");
    public string AgentSettingsEnableRecommendationsText => T("AgentSettings_EnableRecommendations");
    public string AgentSettingsAllowOnlineResearchText => T("WebResearch_AllowOnlineResearch");
    public string AgentSettingsOfficialSourcesOnlyText => T("WebResearch_OfficialSourcesOnly");
    public string AgentSettingsRememberIgnoredText => T("AgentSettings_RememberIgnored");
    public string AgentSettingsClearMemoryText => T("AgentSettings_ClearMemory");
    public string AgentSettingsClearMemoryHintText => T("AgentSettings_ClearMemoryHint");
    public string LanguageSelectorText => T("Settings_Language");
    public string ActionPlanTitleText => T("ActionPlan_Title");
    public string ActionPlanStepsText => T("ActionPlan_Steps");
    public string ActionPlanSafetyLevelText => T("ActionPlan_SafetyLevel");
    public string ActionPlanRequiresAdminText => T("ActionPlan_RequiresAdmin");
    public string ActionPlanReversibleText => T("ActionPlan_Reversible");
    public string ActionPlanBackupLocationText => T("ActionPlan_BackupLocation");
    public string ActionPlanPossibleSideEffectsText => T("ActionPlan_PossibleSideEffects");
    public string ActionPlanConfirmExecutionText => T("ActionPlan_ConfirmExecution");
    public string ActionPlanCancelText => T("Common_Cancel");
    public string ActionPlanNoAutomaticDriverText => T("WebResearch_NoAutomaticDownloads");
    public string SecurityHealthLabelText => T("Security_HealthLabel");
    public string SecurityHealthText => GetSecurityHealthText();
    public string SecurityHealthSummaryText => GetSecurityHealthSummaryText();
    public IBrush SecurityHealthBrush => GetSecurityHealthBrush();
    public string SecurityScanStateLabelText => T("Security_ScanStateLabel");
    public string SecurityScanStateText => ScanResult is null ? T("Security_NotScannedState") : localizationService.GetString("Security_ScannedState", ScanResult.ScannedAt.LocalDateTime.ToString("g"));
    public string SecurityActionableFindingsLabelText => T("Security_ActionableFindingsLabel");
    public string SecurityActionableFindingsText => ScanResult is null ? "-" : CountActionableFindings().ToString();
    public string SecurityHighFindingsLabelText => T("Security_HighFindingsLabel");
    public string SecurityHighFindingsText => ScanResult is null ? "-" : CountHighFindings().ToString();
    public string SecurityTopFindingsTitleText => T("Security_TopFindingsTitle");
    public string SecurityNoFindingsText => T("Security_NoFindings");
    public string SecurityStartScanHintText => T("Security_StartScanHint");
    public IReadOnlyList<RiskFinding> SecurityTopFindings => GetSecurityTopFindings();
    public bool HasSecurityTopFindings => SecurityTopFindings.Count > 0;
    public bool SecurityNoFindingsVisible => ScanResult is not null && !HasSecurityTopFindings;
    public bool SecurityStartScanHintVisible => ScanResult is null;
    public string SecurityDefenderSummaryText => GetSecurityDefenderSummaryText();
    public string PerformanceHealthLabelText => T("Performance_HealthLabel");
    public string PerformanceHealthText => GetPerformanceHealthText();
    public string PerformanceHealthSummaryText => GetPerformanceHealthSummaryText();
    public IBrush PerformanceHealthBrush => GetPerformanceHealthBrush();
    public string PerformanceCpuStatusText => GetUsageStatusText(Overview.CpuUsagePercent);
    public string PerformanceMemoryStatusText => GetUsageStatusText(Overview.MemoryUsagePercent);
    public string PerformanceDiskStatusText => GetUsageStatusText(Overview.DiskUsagePercent);
    public string PerformanceStartupImpactText => GetPerformanceStartupImpactText();
    public string OperatingSystemLabel => T("Dashboard_OperatingSystem");
    public string DefenderLabel => T("Security_Defender");
    public string DefenderStatusText => LocalizeDefenderStatus(DefenderStatus);
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
    public string SignatureText => T("Common_Signature");
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
    public string StartupQuarantineText => T("Autostart_QuarantineShort");
    public string StartupActivateTooltipText => T("Autostart_ActivateTooltip");
    public string StartupDeactivateTooltipText => T("Autostart_DeactivateTooltip");
    public string StartupStopProcessTooltipText => T("Autostart_StopProcessTooltip");
    public string StartupOpenFileLocationTooltipText => T("Autostart_OpenFileLocationTooltip");
    public string StartupDefenderScanTooltipText => T("Autostart_StartDefenderScanTooltip");
    public string StartupQuarantineTooltipText => T("Autostart_QuarantineTooltip");
    public string RealtimeProtectionText => T("Security_RealtimeProtection");
    public string AntivirusText => T("Security_Antivirus");
    public string NoteText => T("Common_Note");
    public string HistoryCountLabel => T("History_Count");
    public string QuarantineIntroText => T("Quarantine_Intro");
    public string QuarantineEmptyText => T("Quarantine_Empty");
    public string QuarantineHowToText => T("Quarantine_HowTo");
    public string HistoryIntroText => T("History_Intro");
    public string HistoryEmptyText => T("History_Empty");
    public string ReportsIntroText => T("Reports_Intro");
    public string ReportsEmptyText => T("Reports_Empty");
    public string ProcessesIntroText => T("Processes_Intro");
    public string ProcessesEmptyText => T("Processes_Empty");
    public string ProgramsIntroText => T("Programs_Intro");
    public string ProgramsCountText => localizationService.GetString("Programs_Count", InstalledPrograms.Count);
    public string ProgramsAnalyzeLeftoversText => T("Programs_AnalyzeLeftovers");
    public string ProgramsLeftoverPlanText => T("Programs_LeftoverPlan");
    public string ProgramsLeftoverSafetyText => T("Programs_LeftoverSafety");
    public bool HasUninstallCleanupPlan => UninstallCleanupPlan is not null;
    public string SystemIntroText => T("System_Intro");
    public string SystemHardwareTitleText => T("System_Hardware");
    public string SystemDiskHealthTitleText => T("System_DiskHealth");
    public string SystemBenchmarkTitleText => T("System_Benchmark");
    public string SystemDriversTitleText => T("System_Drivers");
    public string SystemDriverInventoryText => T("System_DriverInventory");
    public string SystemDriverInventoryEmptyText => T("System_DriverInventoryEmpty");
    public string SystemDriverIssuesText => T("System_DriverIssues");
    public string SystemDriverIssuesEmptyText => T("System_DriverIssuesEmpty");
    public string SystemDriverDeviceText => T("System_DriverDevice");
    public string SystemDriverStatusText => T("System_DriverStatus");
    public string SystemDriverManufacturerText => T("System_DriverManufacturer");
    public string SystemDriverReasonText => T("System_DriverReason");
    public string SystemComputerNameText => T("System_ComputerName");
    public string SystemUserNameText => T("System_UserName");
    public string SystemProcessorText => T("System_Processor");
    public string SystemLogicalProcessorsText => T("System_LogicalProcessors");
    public string SystemTotalMemoryText => T("System_TotalMemory");
    public string SystemDiskNameText => T("System_DiskName");
    public string SystemDiskFormatText => T("System_DiskFormat");
    public string SystemDiskSizeText => T("System_DiskSize");
    public string SystemDiskFreeText => T("System_DiskFree");
    public string SystemDiskUsedText => T("System_DiskUsed");
    public string SystemDiskRecommendationText => T("System_DiskRecommendation");
    public string SystemRunBenchmarkText => T("System_RunBenchmark");
    public string SystemBenchmarkDisclaimerText => T("System_BenchmarkDisclaimer");
    public string SystemDiskWriteText => T("System_DiskWrite");
    public string SystemDiskReadText => T("System_DiskRead");
    public string SystemMemoryCopyText => T("System_MemoryCopy");
    public string BenchmarkDiskWriteText => BenchmarkResult is null ? "-" : $"{BenchmarkResult.DiskWriteMbPerSecond:0.0} MB/s";
    public string BenchmarkDiskReadText => BenchmarkResult is null ? "-" : $"{BenchmarkResult.DiskReadMbPerSecond:0.0} MB/s";
    public string BenchmarkMemoryCopyText => BenchmarkResult is null ? "-" : $"{BenchmarkResult.MemoryCopyMbPerSecond:0.0} MB/s";
    public IReadOnlyList<DriverIssue> DriverProblemDevices => DriverDevices.Where(device => device.IsProblematic).ToList();
    public bool HasDriverDevices => DriverDevices.Count > 0;
    public bool HasDriverProblemDevices => DriverProblemDevices.Count > 0;
    public string VersionText => T("Common_Version");
    public string InstallDateText => T("Programs_InstallDate");
    public string SizeText => T("Programs_Size");
    public string UninstallText => T("Programs_Uninstall");
    public string UninstallTooltipText => T("Programs_UninstallTooltip");
    public string StartupTotalText => StartupItems.Count.ToString();
    public string StartupActiveText => StartupItems.Count(item => item.IsEnabled).ToString();
    public string StartupDisabledText => StartupItems.Count(item => !item.IsEnabled).ToString();
    public string StartupOptionalText => StartupItems.Count(item => item.StartupClassification == StartupClassification.Optional).ToString();
    public string StartupSuspiciousText => StartupItems.Count(item => item.StartupClassification == StartupClassification.Suspicious).ToString();
    public string StartupEssentialText => StartupItems.Count(item => item.StartupClassification == StartupClassification.Essential).ToString();
    public string QuarantineCountText => QuarantineItems.Count.ToString();
    public string HistoryCountText => ActionHistoryItems.Count.ToString();
    public bool IsProcessesEmpty => Processes.Count == 0;
    public bool IsQuarantineEmpty => QuarantineItems.Count == 0;
    public bool IsHistoryEmpty => ActionHistoryItems.Count == 0;
    public bool HasAgentResults => AgentResults.Count > 0;
    public bool IsActionPlanVisible => SelectedActionPlan is not null;

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

    private StartupItemDisplay CreateStartupDisplay(StartupItem item)
    {
        var displayName = item.Name;
        if (string.IsNullOrWhiteSpace(displayName) || displayName.StartsWith("Zuerst ", StringComparison.OrdinalIgnoreCase))
        {
            displayName = !string.IsNullOrWhiteSpace(item.ExecutablePath)
                ? Path.GetFileNameWithoutExtension(item.ExecutablePath)
                : T("Autostart_UnnamedItem");
        }

        var publisher = LocalizeUnknown(item.Publisher);
        var path = string.IsNullOrWhiteSpace(item.ExecutablePath) ? T("Common_NotAvailable") : item.ExecutablePath;
        var recommendation = LocalizeStartupRecommendation(item.StartupClassification);
        var signature = LocalizeSignatureStatus(item.SignatureStatus);
        var status = item.IsEnabled ? T("Autostart_Active") : T("Autostart_Disabled");
        var running = item.IsCurrentlyRunning ? T("Common_Yes") : T("Common_No");
        var classification = LocalizeStartupClassification(item.StartupClassification);

        return new StartupItemDisplay
        {
            Item = item,
            DisplayName = displayName,
            StatusText = status,
            ClassificationText = classification,
            PublisherText = publisher,
            ExecutablePathText = path,
            SignatureStatusText = signature,
            RecommendationText = recommendation,
            RunningText = running,
            DetailsTooltip = string.Join(Environment.NewLine,
                $"{T("Common_Name")}: {displayName}",
                $"{T("Common_Status")}: {status}",
                $"{T("Common_Category")}: {classification}",
                $"{T("Common_Publisher")}: {publisher}",
                $"{T("Common_Path")}: {path}",
                $"{T("Common_Signature")}: {signature}",
                $"{T("Common_Recommendation")}: {recommendation}",
                $"{T("Common_Running")}: {running}")
        };
    }

    private string LocalizeStartupClassification(StartupClassification classification)
    {
        return classification switch
        {
            StartupClassification.Essential => T("Autostart_SystemCritical"),
            StartupClassification.Recommended => T("Autostart_Recommended"),
            StartupClassification.Optional => T("Autostart_Optional"),
            StartupClassification.Unnecessary => T("Autostart_Unnecessary"),
            StartupClassification.Suspicious => T("Autostart_Suspicious"),
            _ => T("Autostart_Unknown")
        };
    }

    private string LocalizeStartupRecommendation(StartupClassification classification)
    {
        return classification switch
        {
            StartupClassification.Essential => T("Autostart_RecommendationEssential"),
            StartupClassification.Recommended => T("Autostart_RecommendationRecommended"),
            StartupClassification.Optional => T("Autostart_RecommendationOptional"),
            StartupClassification.Unnecessary => T("Autostart_RecommendationUnnecessary"),
            StartupClassification.Suspicious => T("Autostart_RecommendationSuspicious"),
            _ => T("Autostart_RecommendationUnknown")
        };
    }

    private string LocalizeSignatureStatus(string status)
    {
        return status switch
        {
            var value when value.Equals("Microsoft signed", StringComparison.OrdinalIgnoreCase) => T("Signature_MicrosoftSigned"),
            var value when value.Equals("Signed", StringComparison.OrdinalIgnoreCase) => T("Signature_Signed"),
            var value when value.Equals("Unsigned", StringComparison.OrdinalIgnoreCase) => T("Signature_Unsigned"),
            var value when value.Equals("Not accessible", StringComparison.OrdinalIgnoreCase) => T("Signature_NotAccessible"),
            var value when value.Equals("Not checked", StringComparison.OrdinalIgnoreCase) => T("Signature_NotChecked"),
            var value when value.Equals("Not applicable", StringComparison.OrdinalIgnoreCase) => T("Signature_NotApplicable"),
            _ => T("Signature_Unknown")
        };
    }

    private string LocalizeRiskLevel(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Critical => T("Risk_Critical"),
            RiskLevel.High => T("Risk_High"),
            RiskLevel.Medium => T("Risk_Medium"),
            RiskLevel.Low => T("Risk_Low"),
            RiskLevel.Info => T("Risk_Info"),
            RiskLevel.None => T("Risk_None"),
            _ => T("Risk_Info")
        };
    }

    private string LocalizeDefenderStatus(DefenderStatus status)
    {
        if (!status.IsAvailable)
        {
            return T("Status_NotChecked");
        }

        if (status.RealTimeProtectionEnabled == true && status.AntivirusEnabled == true)
        {
            return T("Security_DefenderActive");
        }

        if (status.RealTimeProtectionEnabled == false || status.AntivirusEnabled == false)
        {
            return T("Security_DefenderNeedsAttention");
        }

        return LocalizeUnknown(status.StatusText);
    }

    private string LocalizeUnknown(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? T("Common_Unknown")
            : value;
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
        if (int.TryParse(sectionIndex, out var index) && index is >= 0 and <= 8)
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
        OnPropertyChanged(nameof(NavPrograms));
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
        OnPropertyChanged(nameof(DashboardSecurityTitleText));
        OnPropertyChanged(nameof(DashboardPerformanceTitleText));
        OnPropertyChanged(nameof(DashboardProgramsTitleText));
        OnPropertyChanged(nameof(DashboardSecuritySummaryText));
        OnPropertyChanged(nameof(DashboardPerformanceSummaryText));
        OnPropertyChanged(nameof(DashboardProgramsSummaryText));
        OnPropertyChanged(nameof(AgentRecommendationsText));
        OnPropertyChanged(nameof(AgentConfidenceText));
        OnPropertyChanged(nameof(AgentReasonText));
        OnPropertyChanged(nameof(AgentCreateActionPlanText));
        OnPropertyChanged(nameof(AgentIgnoreText));
        OnPropertyChanged(nameof(AgentRemindLaterText));
        OnPropertyChanged(nameof(AgentExecuteAfterApprovalText));
        OnPropertyChanged(nameof(AgentNoAutomaticChangesText));
        OnPropertyChanged(nameof(AgentRequiresApprovalText));
        OnPropertyChanged(nameof(AgentEmptyText));
        OnPropertyChanged(nameof(AgentDriverPolicyText));
        OnPropertyChanged(nameof(AgentSettingsTitleText));
        OnPropertyChanged(nameof(AgentSettingsEnableRecommendationsText));
        OnPropertyChanged(nameof(AgentSettingsAllowOnlineResearchText));
        OnPropertyChanged(nameof(AgentSettingsOfficialSourcesOnlyText));
        OnPropertyChanged(nameof(AgentSettingsRememberIgnoredText));
        OnPropertyChanged(nameof(AgentSettingsClearMemoryText));
        OnPropertyChanged(nameof(AgentSettingsClearMemoryHintText));
        OnPropertyChanged(nameof(LanguageSelectorText));
        OnPropertyChanged(nameof(ActionPlanTitleText));
        OnPropertyChanged(nameof(ActionPlanStepsText));
        OnPropertyChanged(nameof(ActionPlanSafetyLevelText));
        OnPropertyChanged(nameof(ActionPlanRequiresAdminText));
        OnPropertyChanged(nameof(ActionPlanReversibleText));
        OnPropertyChanged(nameof(ActionPlanBackupLocationText));
        OnPropertyChanged(nameof(ActionPlanPossibleSideEffectsText));
        OnPropertyChanged(nameof(ActionPlanConfirmExecutionText));
        OnPropertyChanged(nameof(ActionPlanCancelText));
        OnPropertyChanged(nameof(ActionPlanNoAutomaticDriverText));
        OnPropertyChanged(nameof(SecurityHealthLabelText));
        OnPropertyChanged(nameof(SecurityHealthText));
        OnPropertyChanged(nameof(SecurityHealthSummaryText));
        OnPropertyChanged(nameof(SecurityScanStateLabelText));
        OnPropertyChanged(nameof(SecurityScanStateText));
        OnPropertyChanged(nameof(SecurityActionableFindingsLabelText));
        OnPropertyChanged(nameof(SecurityActionableFindingsText));
        OnPropertyChanged(nameof(SecurityHighFindingsLabelText));
        OnPropertyChanged(nameof(SecurityHighFindingsText));
        OnPropertyChanged(nameof(SecurityTopFindingsTitleText));
        OnPropertyChanged(nameof(SecurityNoFindingsText));
        OnPropertyChanged(nameof(SecurityStartScanHintText));
        OnPropertyChanged(nameof(SecurityDefenderSummaryText));
        OnPropertyChanged(nameof(PerformanceHealthLabelText));
        OnPropertyChanged(nameof(PerformanceHealthText));
        OnPropertyChanged(nameof(PerformanceHealthSummaryText));
        OnPropertyChanged(nameof(PerformanceCpuStatusText));
        OnPropertyChanged(nameof(PerformanceMemoryStatusText));
        OnPropertyChanged(nameof(PerformanceDiskStatusText));
        OnPropertyChanged(nameof(PerformanceStartupImpactText));
        OnPropertyChanged(nameof(OperatingSystemLabel));
        OnPropertyChanged(nameof(DefenderLabel));
        OnPropertyChanged(nameof(DefenderStatusText));
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
        OnPropertyChanged(nameof(FilteredStartupRows));
        OnPropertyChanged(nameof(FilterText));
        OnPropertyChanged(nameof(RiskFilterText));
        OnPropertyChanged(nameof(RiskText));
        OnPropertyChanged(nameof(CategoryText));
        OnPropertyChanged(nameof(NameText));
        OnPropertyChanged(nameof(PathText));
        OnPropertyChanged(nameof(PublisherText));
        OnPropertyChanged(nameof(SignatureText));
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
        OnPropertyChanged(nameof(StartupQuarantineText));
        OnPropertyChanged(nameof(StartupActivateTooltipText));
        OnPropertyChanged(nameof(StartupDeactivateTooltipText));
        OnPropertyChanged(nameof(StartupStopProcessTooltipText));
        OnPropertyChanged(nameof(StartupOpenFileLocationTooltipText));
        OnPropertyChanged(nameof(StartupDefenderScanTooltipText));
        OnPropertyChanged(nameof(StartupQuarantineTooltipText));
        OnPropertyChanged(nameof(RealtimeProtectionText));
        OnPropertyChanged(nameof(AntivirusText));
        OnPropertyChanged(nameof(NoteText));
        OnPropertyChanged(nameof(HistoryCountLabel));
        OnPropertyChanged(nameof(QuarantineIntroText));
        OnPropertyChanged(nameof(QuarantineEmptyText));
        OnPropertyChanged(nameof(QuarantineHowToText));
        OnPropertyChanged(nameof(HistoryIntroText));
        OnPropertyChanged(nameof(HistoryEmptyText));
        OnPropertyChanged(nameof(ReportsIntroText));
        OnPropertyChanged(nameof(ReportsEmptyText));
        OnPropertyChanged(nameof(ProcessesIntroText));
        OnPropertyChanged(nameof(ProcessesEmptyText));
        OnPropertyChanged(nameof(ProgramsIntroText));
        OnPropertyChanged(nameof(ProgramsCountText));
        OnPropertyChanged(nameof(SystemIntroText));
        OnPropertyChanged(nameof(SystemHardwareTitleText));
        OnPropertyChanged(nameof(SystemDiskHealthTitleText));
        OnPropertyChanged(nameof(SystemBenchmarkTitleText));
        OnPropertyChanged(nameof(SystemDriversTitleText));
        OnPropertyChanged(nameof(SystemDriverInventoryText));
        OnPropertyChanged(nameof(SystemDriverInventoryEmptyText));
        OnPropertyChanged(nameof(SystemDriverIssuesText));
        OnPropertyChanged(nameof(SystemDriverIssuesEmptyText));
        OnPropertyChanged(nameof(SystemDriverDeviceText));
        OnPropertyChanged(nameof(SystemDriverStatusText));
        OnPropertyChanged(nameof(SystemDriverManufacturerText));
        OnPropertyChanged(nameof(SystemDriverReasonText));
        OnPropertyChanged(nameof(SystemComputerNameText));
        OnPropertyChanged(nameof(SystemUserNameText));
        OnPropertyChanged(nameof(SystemProcessorText));
        OnPropertyChanged(nameof(SystemLogicalProcessorsText));
        OnPropertyChanged(nameof(SystemTotalMemoryText));
        OnPropertyChanged(nameof(SystemDiskNameText));
        OnPropertyChanged(nameof(SystemDiskFormatText));
        OnPropertyChanged(nameof(SystemDiskSizeText));
        OnPropertyChanged(nameof(SystemDiskFreeText));
        OnPropertyChanged(nameof(SystemDiskUsedText));
        OnPropertyChanged(nameof(SystemDiskRecommendationText));
        OnPropertyChanged(nameof(SystemRunBenchmarkText));
        OnPropertyChanged(nameof(SystemBenchmarkDisclaimerText));
        OnPropertyChanged(nameof(SystemDiskWriteText));
        OnPropertyChanged(nameof(SystemDiskReadText));
        OnPropertyChanged(nameof(SystemMemoryCopyText));
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(nameof(InstallDateText));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(UninstallText));
        OnPropertyChanged(nameof(UninstallTooltipText));
    }

    private async Task LoadPassiveListsAsync()
    {
        if (arePassiveListsLoaded)
        {
            return;
        }

        try
        {
            QuarantineItems = await quarantineService.GetItemsAsync(CancellationToken.None);
            ActionHistoryItems = await actionHistoryService.GetHistoryAsync(CancellationToken.None);
            arePassiveListsLoaded = true;
            OnPropertyChanged(nameof(QuarantineCountText));
            OnPropertyChanged(nameof(HistoryCountText));
            OnPropertyChanged(nameof(IsQuarantineEmpty));
            OnPropertyChanged(nameof(IsHistoryEmpty));
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
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
            _ = RunAgentAnalysisAsync();
        }
    }

    partial void OnAgentRecommendationsEnabledChanged(bool value)
    {
        _ = SaveAgentSettingsAsync();
    }

    partial void OnOnlineResearchAllowedChanged(bool value)
    {
        _ = SaveAgentSettingsAsync();
    }

    partial void OnOfficialSourcesOnlyChanged(bool value)
    {
        _ = SaveAgentSettingsAsync();
    }

    partial void OnRememberIgnoredRecommendationsChanged(bool value)
    {
        _ = SaveAgentSettingsAsync();
    }

    partial void OnSelectedSectionIndexChanged(int value)
    {
        _ = LoadSectionDataAsync(value);
    }

    private async Task LoadSectionDataAsync(int sectionIndex)
    {
        try
        {
            if (sectionIndex == 2)
            {
                await LoadSystemDataAsync();
                if (!areProcessesLoaded)
                {
                    areProcessesLoaded = true;
                    LiveProcesses = new ObservableCollection<ProcessScanItem>(await processScanner.ScanAsync(CancellationToken.None));
                }
            }
            else if (sectionIndex == 20 && !areProcessesLoaded)
            {
                areProcessesLoaded = true;
                LiveProcesses = new ObservableCollection<ProcessScanItem>(await processScanner.ScanAsync(CancellationToken.None));
            }
            else if (sectionIndex == 3 && !areProgramsLoaded)
            {
                areProgramsLoaded = true;
                InstalledPrograms = await installedProgramScanner.ScanAsync(CancellationToken.None);
                OnPropertyChanged(nameof(ProgramsCountText));
                OnPropertyChanged(nameof(DashboardProgramsSummaryText));
                await RunAgentAnalysisAsync();
            }
            else if (sectionIndex is 5 or 6)
            {
                arePassiveListsLoaded = false;
                await LoadPassiveListsAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
    }

    private async Task LoadSystemDataAsync()
    {
        var hardwareTask = hardwareInfoService.GetHardwareInfoAsync(CancellationToken.None);
        var driverTask = deviceDriverScanner.ScanAsync(CancellationToken.None);
        await Task.WhenAll(hardwareTask, driverTask);
        HardwareInfo = await hardwareTask;
        DriverDevices = await driverTask;
    }

    private bool CanRunQuickBenchmark()
    {
        return !IsBenchmarkRunning;
    }

    [RelayCommand(CanExecute = nameof(CanRunQuickBenchmark))]
    private async Task RunQuickBenchmarkAsync()
    {
        IsBenchmarkRunning = true;
        try
        {
            BenchmarkResult = await hardwareInfoService.RunQuickBenchmarkAsync(CancellationToken.None);
            StatusMessage = T("System_BenchmarkCompleted");
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
        finally
        {
            IsBenchmarkRunning = false;
        }
    }

    private async Task LoadAgentSettingsAsync()
    {
        try
        {
            isInitializingAgentSettings = true;
            var settings = await userSettingsService.GetSettingsAsync(CancellationToken.None);
            AgentRecommendationsEnabled = settings.AgentRecommendationsEnabled;
            OnlineResearchAllowed = settings.OnlineResearchAllowed;
            OfficialSourcesOnly = settings.OfficialSourcesOnly;
            RememberIgnoredRecommendations = settings.RememberIgnoredRecommendations;
        }
        finally
        {
            isInitializingAgentSettings = false;
        }
    }

    private async Task SaveAgentSettingsAsync()
    {
        if (isInitializingAgentSettings)
        {
            return;
        }

        await userSettingsService.SaveSettingsAsync(new UserSettingsSnapshot
        {
            Language = SelectedLanguage?.CultureCode ?? localizationService.CurrentCulture,
            AgentRecommendationsEnabled = AgentRecommendationsEnabled,
            OnlineResearchAllowed = OnlineResearchAllowed,
            OfficialSourcesOnly = OfficialSourcesOnly,
            RememberIgnoredRecommendations = RememberIgnoredRecommendations
        }, CancellationToken.None);
    }

    private async Task RunAgentAnalysisAsync()
    {
        if (ScanResult is null || !AgentRecommendationsEnabled)
        {
            AgentResults = [];
            return;
        }

        try
        {
            AgentStatusMessage = T("Agent_Analyzing");
            var report = await agentOrchestrator.AnalyzeAsync(new AgentContext
            {
                ScanResult = ScanResult,
                InstalledPrograms = InstalledPrograms,
                AgentRecommendationsEnabled = AgentRecommendationsEnabled,
                OnlineResearchAllowed = OnlineResearchAllowed,
                OfficialSourcesOnly = OfficialSourcesOnly,
                RememberIgnoredRecommendations = RememberIgnoredRecommendations
            }, CancellationToken.None);
            AgentResults = report.AgentResults;
            AgentStatusMessage = localizationService.GetString("Agent_Completed", AgentResults.Count);
        }
        catch (Exception ex)
        {
            AgentStatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
    }

    private void NotifyStartupChanged()
    {
        OnPropertyChanged(nameof(StartupItems));
        OnPropertyChanged(nameof(FilteredStartupItems));
        OnPropertyChanged(nameof(FilteredStartupRows));
        OnPropertyChanged(nameof(StartupTotalText));
        OnPropertyChanged(nameof(StartupActiveText));
        OnPropertyChanged(nameof(StartupDisabledText));
        OnPropertyChanged(nameof(StartupOptionalText));
        OnPropertyChanged(nameof(StartupSuspiciousText));
        OnPropertyChanged(nameof(StartupEssentialText));
        OnPropertyChanged(nameof(DashboardPerformanceSummaryText));
        OnPropertyChanged(nameof(DashboardSecuritySummaryText));
    }

    private string GetDashboardSecuritySummary()
    {
        if (ScanResult is null)
        {
            return T("Dashboard_SecuritySummaryNotScanned");
        }

        var actionableFindings = ScanResult.Findings.Count(finding => finding.Level is RiskLevel.Critical or RiskLevel.High or RiskLevel.Medium or RiskLevel.Low);
        var defenderText = DefenderStatus.RealTimeProtectionEnabled == true
            ? T("Security_RealtimeProtectionActive")
            : T("Security_RealtimeProtectionNeedsAttention");

        return localizationService.GetString("Dashboard_SecuritySummary", defenderText, actionableFindings);
    }

    private string GetDashboardPerformanceSummary()
    {
        if (ScanResult is null)
        {
            return T("Dashboard_PerformanceSummaryNotScanned");
        }

        var optimizable = StartupItems.Count(item => item.StartupClassification is StartupClassification.Optional or StartupClassification.Unnecessary);
        return localizationService.GetString("Dashboard_PerformanceSummary", StartupItems.Count, optimizable);
    }

    private string GetSecurityHealthText()
    {
        if (ScanResult is null)
        {
            return T("Security_HealthUnknown");
        }

        return ScanResult.OverallRiskLevel switch
        {
            RiskLevel.Critical or RiskLevel.High => T("Security_HealthUnsafe"),
            RiskLevel.Medium => T("Security_HealthAttention"),
            RiskLevel.Low => T("Security_HealthMostlySafe"),
            _ => T("Security_HealthSafe")
        };
    }

    private string GetSecurityHealthSummaryText()
    {
        if (ScanResult is null)
        {
            return T("Security_StartScanHint");
        }

        var actionableFindings = CountActionableFindings();
        var highFindings = CountHighFindings();
        var defenderOk = DefenderStatus.RealTimeProtectionEnabled == true && DefenderStatus.AntivirusEnabled != false;

        if (actionableFindings == 0 && defenderOk)
        {
            return T("Security_SummarySafe");
        }

        if (highFindings > 0)
        {
            return localizationService.GetString("Security_SummaryHighRisk", highFindings, actionableFindings);
        }

        return localizationService.GetString("Security_SummaryAttention", actionableFindings);
    }

    private IBrush GetSecurityHealthBrush()
    {
        return ScanResult?.OverallRiskLevel switch
        {
            RiskLevel.Critical or RiskLevel.High => new SolidColorBrush(Color.Parse("#B42318")),
            RiskLevel.Medium => new SolidColorBrush(Color.Parse("#B54708")),
            RiskLevel.Low => new SolidColorBrush(Color.Parse("#2563EB")),
            RiskLevel.None => new SolidColorBrush(Color.Parse("#047857")),
            _ => new SolidColorBrush(Color.Parse("#64748B"))
        };
    }

    private string GetSecurityDefenderSummaryText()
    {
        if (!DefenderStatus.IsAvailable)
        {
            return T("Security_DefenderUnavailable");
        }

        if (DefenderStatus.RealTimeProtectionEnabled == true && DefenderStatus.AntivirusEnabled != false)
        {
            return T("Security_DefenderHealthy");
        }

        return T("Security_DefenderAttention");
    }

    private IReadOnlyList<RiskFinding> GetSecurityTopFindings()
    {
        return (ScanResult?.Findings ?? [])
            .Where(finding => finding.Level is RiskLevel.Critical or RiskLevel.High or RiskLevel.Medium or RiskLevel.Low)
            .ToList();
    }

    private int CountActionableFindings()
    {
        return ScanResult?.Findings.Count(finding => finding.Level is RiskLevel.Critical or RiskLevel.High or RiskLevel.Medium or RiskLevel.Low) ?? 0;
    }

    private int CountHighFindings()
    {
        return ScanResult?.Findings.Count(finding => finding.Level is RiskLevel.Critical or RiskLevel.High) ?? 0;
    }

    private string GetPerformanceHealthText()
    {
        var highestUsage = Math.Max(Overview.CpuUsagePercent, Math.Max(Overview.MemoryUsagePercent, Overview.DiskUsagePercent));
        if (highestUsage >= 90)
        {
            return T("Performance_HealthSlow");
        }

        if (highestUsage >= 75 || GetOptimizableStartupCount() >= 8)
        {
            return T("Performance_HealthAttention");
        }

        return T("Performance_HealthGood");
    }

    private string GetPerformanceHealthSummaryText()
    {
        var highestUsage = Math.Max(Overview.CpuUsagePercent, Math.Max(Overview.MemoryUsagePercent, Overview.DiskUsagePercent));
        if (highestUsage >= 90)
        {
            return T("Performance_SummaryHighUsage");
        }

        if (ScanResult is null)
        {
            return T("Performance_SummaryLiveOnly");
        }

        return localizationService.GetString("Performance_SummaryWithStartup", GetOptimizableStartupCount(), StartupItems.Count);
    }

    private IBrush GetPerformanceHealthBrush()
    {
        var highestUsage = Math.Max(Overview.CpuUsagePercent, Math.Max(Overview.MemoryUsagePercent, Overview.DiskUsagePercent));
        if (highestUsage >= 90)
        {
            return new SolidColorBrush(Color.Parse("#B42318"));
        }

        if (highestUsage >= 75 || GetOptimizableStartupCount() >= 8)
        {
            return new SolidColorBrush(Color.Parse("#B54708"));
        }

        return new SolidColorBrush(Color.Parse("#047857"));
    }

    private string GetUsageStatusText(double usage)
    {
        if (usage >= 90)
        {
            return T("Performance_UsageHigh");
        }

        if (usage >= 75)
        {
            return T("Performance_UsageElevated");
        }

        return T("Performance_UsageGood");
    }

    private string GetPerformanceStartupImpactText()
    {
        if (ScanResult is null)
        {
            return T("Performance_StartupNotScanned");
        }

        return localizationService.GetString("Performance_StartupImpact", GetOptimizableStartupCount(), StartupItems.Count);
    }

    private int GetOptimizableStartupCount()
    {
        return StartupItems.Count(item => item.StartupClassification is StartupClassification.Optional or StartupClassification.Unnecessary);
    }

    private bool CanStartScan()
    {
        return !IsScanning;
    }

    [RelayCommand]
    private async Task ClearAgentMemoryAsync()
    {
        try
        {
            await agentMemoryService.ClearAsync(CancellationToken.None);
            AgentResults = [];
            AgentStatusMessage = T("Agent_MemoryCleared");
        }
        catch (Exception ex)
        {
            AgentStatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void CreateAgentActionPlan(AgentResult agentResult)
    {
        if (agentResult is null)
        {
            return;
        }

        SelectedAgentResult = agentResult;
        SelectedActionPlan = agentResult.SuggestedActionPlans.FirstOrDefault() ?? BuildReadOnlyActionPlan(agentResult);
        AgentStatusMessage = localizationService.GetString("ActionPlan_Created", agentResult.AgentName);
    }

    [RelayCommand]
    private void CancelActionPlan()
    {
        SelectedActionPlan = null;
        SelectedAgentResult = null;
    }

    [RelayCommand]
    private async Task ExecuteActionPlanAsync()
    {
        if (SelectedActionPlan is null)
        {
            return;
        }

        var plan = SelectedActionPlan;
        var executed = 0;
        var skipped = 0;

        foreach (var step in plan.Steps.OrderBy(step => step.Order))
        {
            var category = SelectedAgentResult?.Findings.FirstOrDefault()?.Category ?? GuessCategory(step.ActionType);
            if (!agentPolicyService.IsActionAllowed(step.ActionType, category))
            {
                skipped++;
                await WriteAgentHistoryAsync(step, ActionResultStatus.NotAllowed, "Agent policy blocked this action.");
                continue;
            }

            var status = await ExecuteActionPlanStepAsync(step);
            if (status == ActionResultStatus.Success)
            {
                executed++;
            }
            else
            {
                skipped++;
            }
        }

        ActionHistoryItems = await actionHistoryService.GetHistoryAsync(CancellationToken.None);
        OnPropertyChanged(nameof(HistoryCountText));
        OnPropertyChanged(nameof(IsHistoryEmpty));
        AgentStatusMessage = localizationService.GetString("ActionPlan_Executed", executed, skipped);
    }

    [RelayCommand]
    private void IgnoreAgentRecommendation(AgentResult agentResult)
    {
        AgentStatusMessage = agentResult is null
            ? string.Empty
            : localizationService.GetString("Agent_Ignored", agentResult.AgentName);
    }

    [RelayCommand]
    private void RemindAgentRecommendationLater(AgentResult agentResult)
    {
        AgentStatusMessage = agentResult is null
            ? string.Empty
            : localizationService.GetString("Agent_RemindLaterSet", agentResult.AgentName);
    }

    private ActionPlan BuildReadOnlyActionPlan(AgentResult agentResult)
    {
        var steps = agentResult.Recommendations
            .Select((recommendation, index) => new ActionPlanStep
            {
                Order = index + 1,
                Description = $"{recommendation.Title}: {recommendation.Description}",
                ActionType = recommendation.RelatedActionType,
                Target = recommendation.ExpectedBenefit,
                IsReversible = recommendation.SafetyLevel is AgentSafetyLevel.Safe or AgentSafetyLevel.RequiresConfirmation
            })
            .ToList();

        if (steps.Count == 0)
        {
            steps.Add(new ActionPlanStep
            {
                Order = 1,
                Description = agentResult.Summary,
                ActionType = AgentActionType.None,
                IsReversible = true
            });
        }

        return new ActionPlan
        {
            Title = agentResult.AgentName,
            Description = agentResult.Summary,
            Steps = steps,
            SafetyLevel = steps.All(step => agentPolicyService.IsActionAllowed(step.ActionType, agentResult.Findings.FirstOrDefault()?.Category ?? GuessCategory(step.ActionType)))
                ? AgentSafetyLevel.RequiresConfirmation
                : AgentSafetyLevel.NotAllowed,
            RequiresAdmin = steps.Any(step => step.RequiresAdmin),
            IsReversible = steps.All(step => step.IsReversible),
            BackupRequired = steps.Any(step => !string.IsNullOrWhiteSpace(step.BackupPath)),
            EstimatedImpact = string.Join(Environment.NewLine, agentResult.Recommendations.Select(recommendation => recommendation.ExpectedBenefit).Where(value => !string.IsNullOrWhiteSpace(value))),
            UserConfirmationText = T("Agent_RequiresApproval"),
            CanExecuteAutomaticallyAfterApproval = false
        };
    }

    private async Task<ActionResultStatus> ExecuteActionPlanStepAsync(ActionPlanStep step)
    {
        try
        {
            var status = step.ActionType switch
            {
                AgentActionType.OpenWindowsUpdate => OpenExternal("ms-settings:windowsupdate"),
                AgentActionType.OpenDeviceManager => OpenExternal("devmgmt.msc"),
                AgentActionType.OpenWindowsSecurity => (await defenderActionService.OpenWindowsSecurityAsync(CancellationToken.None)).Status,
                AgentActionType.StartDefenderQuickScan => (await defenderActionService.StartQuickScanAsync(CancellationToken.None)).Status,
                AgentActionType.ExportDriverReport => await ExportAgentDriverReportAsync(),
                AgentActionType.ShowOfficialManufacturerPage => ActionResultStatus.NotAllowed,
                AgentActionType.OpenFileLocation => ActionResultStatus.NotAllowed,
                AgentActionType.DisableStartupEntry => ActionResultStatus.NotAllowed,
                AgentActionType.EnableStartupEntry => ActionResultStatus.NotAllowed,
                AgentActionType.StopProcess => ActionResultStatus.NotAllowed,
                AgentActionType.RunNormalUninstaller => ActionResultStatus.NotAllowed,
                AgentActionType.ScanLeftovers => ActionResultStatus.NotAllowed,
                AgentActionType.MoveLeftoversToBackup => ActionResultStatus.NotAllowed,
                _ => ActionResultStatus.NotAllowed
            };

            await WriteAgentHistoryAsync(step, status, status == ActionResultStatus.Success ? "Action completed from agent plan." : "Action requires a more specific item or manual handling.");
            return status;
        }
        catch (Exception ex)
        {
            await WriteAgentHistoryAsync(step, ActionResultStatus.Failed, ex.Message);
            return ActionResultStatus.Failed;
        }
    }

    private static ActionResultStatus OpenExternal(string target)
    {
        Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        return ActionResultStatus.Success;
    }

    private async Task<ActionResultStatus> ExportAgentDriverReportAsync()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyPcGuard", "Reports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"driver-agent-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        var lines = new List<string>
        {
            "MyPcGuard Driver Agent Report",
            "Driver download policy: MyPcGuard never downloads or installs drivers automatically.",
            $"Created: {DateTimeOffset.Now:g}",
            string.Empty
        };

        if (SelectedAgentResult is not null)
        {
            lines.Add(SelectedAgentResult.Summary);
            lines.AddRange(SelectedAgentResult.Findings.Select(finding => $"- {finding.Title}: {finding.Description} {finding.Evidence}".Trim()));
        }

        await File.WriteAllLinesAsync(path, lines, CancellationToken.None);
        AgentStatusMessage = localizationService.GetString("Reports_ReportCreated", path);
        return ActionResultStatus.Success;
    }

    private async Task WriteAgentHistoryAsync(ActionPlanStep step, ActionResultStatus status, string message)
    {
        await actionHistoryService.AddAsync(new ActionHistoryItem
        {
            ActionType = MapActionType(step.ActionType),
            Status = status,
            Target = string.IsNullOrWhiteSpace(step.Target) ? step.Description : step.Target,
            Message = message
        }, CancellationToken.None);
    }

    private static ActionType MapActionType(AgentActionType actionType)
    {
        return actionType switch
        {
            AgentActionType.OpenFileLocation => ActionType.OpenFileLocation,
            AgentActionType.StopProcess => ActionType.StopProcess,
            AgentActionType.DisableStartupEntry => ActionType.DisableStartupEntry,
            AgentActionType.EnableStartupEntry => ActionType.EnableStartupEntry,
            AgentActionType.StartDefenderQuickScan => ActionType.StartDefenderQuickScan,
            AgentActionType.OpenWindowsSecurity => ActionType.OpenWindowsSecurity,
            AgentActionType.RunNormalUninstaller => ActionType.UninstallProgram,
            _ => ActionType.OpenWindowsSecurity
        };
    }

    private static AgentCategory GuessCategory(AgentActionType actionType)
    {
        return actionType switch
        {
            AgentActionType.OpenWindowsUpdate or AgentActionType.OpenDeviceManager or AgentActionType.ShowOfficialManufacturerPage or AgentActionType.ExportDriverReport => AgentCategory.DriverCheck,
            AgentActionType.DisableStartupEntry or AgentActionType.EnableStartupEntry or AgentActionType.StopProcess => AgentCategory.StartupOptimization,
            AgentActionType.OpenWindowsSecurity or AgentActionType.StartDefenderQuickScan => AgentCategory.Security,
            AgentActionType.RunNormalUninstaller or AgentActionType.ScanLeftovers or AgentActionType.MoveLeftoversToBackup => AgentCategory.ProgramUninstall,
            _ => AgentCategory.SystemHealth
        };
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

    [RelayCommand]
    private async Task QuarantineStartupAsync(StartupItem item)
    {
        if (item is null || !item.CanMoveToQuarantine)
        {
            StatusMessage = T("Action_NotAllowed");
            return;
        }

        if (!await confirmationDialogService.ConfirmAsync(
            T("Action_Quarantine_Title"),
            localizationService.GetString("Action_Quarantine_Confirm", item.Name),
            CancellationToken.None))
        {
            return;
        }

        try
        {
            var disableResult = item.IsEnabled && item.CanDisable
                ? await autostartActionService.DisableAsync(item, CancellationToken.None)
                : null;

            if (disableResult is not null && !disableResult.Success)
            {
                StatusMessage = LocalizeResultMessage(disableResult.Message);
                return;
            }

            var result = await quarantineService.MoveToQuarantineAsync(item.ExecutablePath, item.Recommendation, CancellationToken.None);
            StatusMessage = LocalizeResultMessage(result.Message);
            QuarantineItems = await quarantineService.GetItemsAsync(CancellationToken.None);
            OnPropertyChanged(nameof(QuarantineCountText));
            OnPropertyChanged(nameof(IsQuarantineEmpty));

            ScanResult = await scanOrchestrator.RunScanAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
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

    [RelayCommand]
    private async Task UninstallProgramAsync(InstalledProgramItem item)
    {
        if (item is null || !item.CanUninstall)
        {
            StatusMessage = T("Action_Uninstall_NotAvailable");
            return;
        }

        if (!await confirmationDialogService.ConfirmAsync(
            T("Programs_Uninstall"),
            localizationService.GetString("Programs_UninstallConfirm", item.Name),
            CancellationToken.None))
        {
            return;
        }

        try
        {
            var result = await installedProgramActionService.StartUninstallAsync(item, CancellationToken.None);
            StatusMessage = LocalizeResultMessage(result.Message);
            ActionHistoryItems = await actionHistoryService.GetHistoryAsync(CancellationToken.None);
            InstalledPrograms = await installedProgramScanner.ScanAsync(CancellationToken.None);
            OnPropertyChanged(nameof(HistoryCountText));
            OnPropertyChanged(nameof(IsHistoryEmpty));
            OnPropertyChanged(nameof(ProgramsCountText));
            OnPropertyChanged(nameof(DashboardProgramsSummaryText));
            _ = RefreshInstalledProgramsAfterDelayAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
    }

    private async Task RefreshInstalledProgramsAfterDelayAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            InstalledPrograms = await installedProgramScanner.ScanAsync(CancellationToken.None);
            OnPropertyChanged(nameof(ProgramsCountText));
            OnPropertyChanged(nameof(DashboardProgramsSummaryText));
            await RunAgentAnalysisAsync();
        }
        catch
        {
        }
    }

    [RelayCommand]
    private async Task AnalyzeProgramLeftoversAsync(InstalledProgramItem item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            UninstallCleanupPlan = await uninstallCleanupPlanner.AnalyzeLeftoversAsync(item, CancellationToken.None);
            StatusMessage = UninstallCleanupPlan.Summary;
        }
        catch (Exception ex)
        {
            StatusMessage = localizationService.GetString("Action_Failed", ex.Message);
        }
    }
}
