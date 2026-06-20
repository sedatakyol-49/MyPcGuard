namespace MyPcGuard.Models;

public enum ActionType
{
    OpenFileLocation,
    StopProcess,
    DisableStartupEntry,
    EnableStartupEntry,
    MoveToQuarantine,
    RestoreFromQuarantine,
    DeleteQuarantinedFile,
    StartDefenderQuickScan,
    StartDefenderFullScan,
    OpenWindowsSecurity
}
