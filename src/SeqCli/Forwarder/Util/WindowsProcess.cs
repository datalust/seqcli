#if WINDOWS

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;

// ReSharper disable once InconsistentNaming

namespace Seq.Forwarder.Util
{
    static class WindowsProcess
    {
        [StructLayout(LayoutKind.Sequential)]
        readonly struct PROCESS_BASIC_INFORMATION
        {
            readonly IntPtr _reserved1;
            readonly IntPtr _pebBaseAddress;
            readonly IntPtr _reserved2_0;
            readonly IntPtr _reserved2_1;
            readonly IntPtr _uniqueProcessId;
            public readonly IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("ntdll.dll")]
        static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        public static Process? GetParentProcess()
        {
            var currentProcess = Process.GetCurrentProcess();

            var pbi = new PROCESS_BASIC_INFORMATION();
            var status = NtQueryInformationProcess(currentProcess.Handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Could not query parent process information");
                return null;
            }
        }
    }
}

#endif
