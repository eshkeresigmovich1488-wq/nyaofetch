using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Nyaofetch;

public sealed record SystemInfo(
    string Username,
    string Hostname,
    string OsName,
    string OsBuild,
    string CpuName,
    string GpuName,
    string TotalRamMb,
    string UsedRamMb,
    string DiskTotalGb,
    string DiskFreeGb,
    string Uptime,
    string Shell,
    string Resolution
);

public static class SysInfo
{
    // ---- P/Invoke declarations -------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)] public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        [MarshalAs(UnmanagedType.U4)] public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    // ---- individual field collectors -------------------------------------------

    private static (string name, string build) GetOsInfo()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var productName = key?.GetValue("ProductName") as string ?? "Windows";
            var buildStr = key?.GetValue("CurrentBuildNumber") as string ?? "0";
            var ubr = key?.GetValue("UBR"); // update build revision, e.g. the ".3880" part

            int.TryParse(buildStr, out int build);
            // Registry still says "Windows 10" even on Windows 11 - the build
            // number is the only reliable signal to tell them apart.
            string name = build >= 22000 ? productName.Replace("Windows 10", "Windows 11") : productName;

            string fullBuild = ubr != null ? $"{buildStr}.{ubr}" : buildStr;
            return (name, fullBuild);
        }
        catch
        {
            return ("Windows (unknown)", "unknown");
        }
    }

    private static string GetCpuName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return (key?.GetValue("ProcessorNameString") as string)?.Trim() ?? "Unknown CPU";
        }
        catch
        {
            return "Unknown CPU";
        }
    }

    private static string GetGpuName()
    {
        try
        {
            var dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(dd);
            if (EnumDisplayDevices(null, 0, ref dd, 0))
                return dd.DeviceString;
        }
        catch { /* fall through */ }
        return "Unknown GPU";
    }

    private static (string totalMb, string usedMb) GetRam()
    {
        try
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref status))
            {
                ulong totalMb = status.ullTotalPhys / (1024 * 1024);
                ulong usedMb = totalMb - (status.ullAvailPhys / (1024 * 1024));
                return (totalMb.ToString(), usedMb.ToString());
            }
        }
        catch { /* fall through */ }
        return ("Unknown", "Unknown");
    }

    private static (string totalGb, string freeGb) GetDisk()
    {
        try
        {
            var drive = new DriveInfo("C");
            long totalGb = drive.TotalSize / (1024 * 1024 * 1024);
            long freeGb = drive.TotalFreeSpace / (1024 * 1024 * 1024);
            return (totalGb.ToString(), freeGb.ToString());
        }
        catch
        {
            return ("Unknown", "Unknown");
        }
    }

    private static string GetUptime()
    {
        var ts = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return ts.Days > 0
            ? $"{ts.Days}d {ts.Hours}h {ts.Minutes}m"
            : $"{ts.Hours}h {ts.Minutes}m";
    }

    private static string GetShell()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WT_SESSION")))
            return "Windows Terminal";

        var comspec = Environment.GetEnvironmentVariable("ComSpec");
        return string.IsNullOrEmpty(comspec) ? "cmd.exe" : Path.GetFileName(comspec);
    }

    private static string GetResolution()
    {
        int w = GetSystemMetrics(SM_CXSCREEN);
        int h = GetSystemMetrics(SM_CYSCREEN);
        return $"{w}x{h}";
    }

    // ---- public entry point -----------------------------------------------------

    public static SystemInfo Collect()
    {
        var (osName, osBuild) = GetOsInfo();
        var (totalRam, usedRam) = GetRam();
        var (diskTotal, diskFree) = GetDisk();

        return new SystemInfo(
            Username: Environment.UserName,
            Hostname: Environment.MachineName,
            OsName: osName,
            OsBuild: osBuild,
            CpuName: GetCpuName(),
            GpuName: GetGpuName(),
            TotalRamMb: totalRam,
            UsedRamMb: usedRam,
            DiskTotalGb: diskTotal,
            DiskFreeGb: diskFree,
            Uptime: GetUptime(),
            Shell: GetShell(),
            Resolution: GetResolution()
        );
    }
}
