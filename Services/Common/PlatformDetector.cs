using System.Runtime.InteropServices;
using MyPcGuard.Models;

namespace MyPcGuard.Services.Common;

public static class PlatformDetector
{
    public static OperatingSystemType Detect()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OperatingSystemType.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OperatingSystemType.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OperatingSystemType.MacOS;
        }

        return OperatingSystemType.Unknown;
    }
}
