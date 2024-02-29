namespace Seq.Forwarder.Util
{
    static class ExecutionEnvironment
    {
        public static bool SupportsStandardIO => !IsRunningAsWindowsService;

        static bool IsRunningAsWindowsService
        {
            get
            {
#if WINDOWS
                var parent = WindowsProcess.GetParentProcess();
                return parent?.ProcessName == "services";
#else
                return false;
#endif
            }
        }
    }
}
