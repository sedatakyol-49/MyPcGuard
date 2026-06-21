namespace MyPcGuard.Agents.Models;

public enum AgentActionType
{
    None,
    DisableStartupEntry,
    EnableStartupEntry,
    StopProcess,
    StartDefenderQuickScan,
    OpenWindowsSecurity,
    OpenFileLocation,
    RunNormalUninstaller,
    ScanLeftovers,
    MoveLeftoversToBackup,
    OpenWindowsUpdate,
    OpenDeviceManager,
    ShowOfficialManufacturerPage,
    ExportDriverReport,
    ExportReport,
    OpenBackupRecommendationGuide,
    ScheduleRecheck
}
