using System.Runtime.InteropServices;

namespace SeqCli.Forwarder.Util;

static class ExecutionEnvironment
{
    public static bool SupportsStandardIO => !IsRunningAsWindowsService;

    static bool IsRunningAsWindowsService
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var parent = WindowsProcess.GetParentProcess();
                return parent?.ProcessName == "services";
            }

            return false;
        }
    }
}
