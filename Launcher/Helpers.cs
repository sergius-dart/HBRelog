using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;

namespace HighVoltz.Launcher
{
    public class Helpers
    {

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CreateProcess(string lpApplicationName,
           string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
           ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles,
           uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr LoadLibraryA(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [Flags()]
        public enum AllocationType : uint
        {
            COMMIT = 0x1000,
            RESERVE = 0x2000,
            RESET = 0x80000,
            LARGE_PAGES = 0x20000000,
            PHYSICAL = 0x400000,
            TOP_DOWN = 0x100000,
            WRITE_WATCH = 0x200000
        }

        [Flags()]
        public enum MemoryProtection : uint
        {
            EXECUTE = 0x10,
            EXECUTE_READ = 0x20,
            EXECUTE_READWRITE = 0x40,
            EXECUTE_WRITECOPY = 0x80,
            NOACCESS = 0x01,
            READONLY = 0x02,
            READWRITE = 0x04,
            WRITECOPY = 0x08,
            GUARD_Modifierflag = 0x100,
            NOCACHE_Modifierflag = 0x200,
            WRITECOMBINE_Modifierflag = 0x400
        }
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
         IntPtr hProcess,
         IntPtr lpThreadAttributes,
         uint dwStackSize,
         IntPtr lpStartAddress, // raw Pointer into remote process
         IntPtr lpParameter,
         uint dwCreationFlags,
         out uint lpThreadId
       );

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        static extern int WaitForSingleObject(IntPtr handle, int dwMilisec);
        const int WAIT_ABANDONED = 0x00000080;
        const int WAIT_OBJECT_0 = 0x00000000;
        const int WAIT_TIMEOUT = 0x00000102;
        const int WAIT_FAILED = -1;

        [DllImport("kernel32.dll")]
        static extern bool GetExitCodeThread(IntPtr hThread, out int lpExitCode);


        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static int? StartProcessSuspended(string wowPath, string wowArgs, string proxy)
        {
            string dllPath = Path.Combine( Directory.GetCurrentDirectory(), "FakeHWID.dll" );
            //inject

            string fakeDir = Path.GetDirectoryName(dllPath);

            if (!File.Exists(dllPath))
            {
                MessageBox.Show("Not found FakeHWID.dll. Pls reinstall FakeHWID.");
                return null;
            }

            string path = fakeDir + ";" + Environment.GetEnvironmentVariable("Path");//add to load needed dlls
            SetEnvironmentVariable("Path", path);
            if ( proxy.Length == 0 )
            {
                MessageBox.Show("Need change proxy!");
                return null;
            }
            SetEnvironmentVariable("FAKE_NULLPTR_socket", proxy);//set env proxy

            //create process
            var pInfo = new PROCESS_INFORMATION();
            var sInfo = new STARTUPINFO();
            var pSec = new SECURITY_ATTRIBUTES();
            var tSec = new SECURITY_ATTRIBUTES();
            pSec.nLength = Marshal.SizeOf(pSec);
            tSec.nLength = Marshal.SizeOf(tSec);
            const uint CREATE_SUSPENDED = 0x00000004;
            var result = CreateProcess(
                null,
                wowPath + " " +
                    (wowArgs.IndexOf("noautolaunch64bit") > -1 ? //auto add run ONLY 32 bit
                        wowArgs :
                        (wowArgs + " -noautolaunch64bit")
                    ),
                ref pSec,
                ref tSec,
                false,
                CREATE_SUSPENDED,//create suspended, need dll init
                IntPtr.Zero,
                null,
                ref sInfo,
                out pInfo);
            if (result == false)
            {
                MessageBox.Show(String.Format("Error on create process : {0}", GetLastError()));
                return null;
            }


            //get PTR to LoadLibraryA
            IntPtr inst = GetModuleHandle("kernel32.dll");
            IntPtr target = GetProcAddress(inst, "LoadLibraryW");
            //get memory to args
            IntPtr argsLoadLibraryW = VirtualAllocEx(pInfo.hProcess, IntPtr.Zero, (IntPtr)dllPath.Length + 1, AllocationType.RESERVE | AllocationType.COMMIT, MemoryProtection.READWRITE);
            //write args
            IntPtr bytesWriten = IntPtr.Zero;
            byte[] qwe = GetBytes(dllPath);
            WriteProcessMemory(pInfo.hProcess, argsLoadLibraryW, GetBytes(dllPath), dllPath.Length * sizeof(char) + 1, out bytesWriten);
            //start load
            uint thrId = 0;
            IntPtr hRemoteThread = CreateRemoteThread(pInfo.hProcess, IntPtr.Zero, 0, target, argsLoadLibraryW, 0, out thrId); // start LoadLibrary 
            int ThreadTeminationStatus = 0;
            if (hRemoteThread.ToInt64() != 0) //start good?
            {
                WaitForSingleObject(hRemoteThread, -1);//wait init fake library
                GetExitCodeThread(hRemoteThread, out ThreadTeminationStatus);
                if (ThreadTeminationStatus == 0)//if init NOT retirn TRUE
                {
                    MessageBox.Show("Error on load library : " + GetLastError());
                    Process.GetProcessById(pInfo.dwProcessId).Kill();
                    return null;
                }
            }
            else
            {
                MessageBox.Show(String.Format("Error on CreateRemoteThread. Check permissions. {0} ", GetLastError()));
                Process.GetProcessById(pInfo.dwProcessId).Kill();
                return null;
            }

            uint error = GetLastError();
            //start process
            ResumeThread(pInfo.hThread);

            return result ? (int?)pInfo.dwProcessId : null;
        }

        public static void SuspendProcess(int pid)
        {
            Process proc = Process.GetProcessById(pid);

            if (proc.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in proc.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }

                SuspendThread(pOpenThread);
            }
        }

        public static void ResumeProcess(int pid)
        {
            Process proc = Process.GetProcessById(pid);

            if (proc.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in proc.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }

                ResumeThread(pOpenThread);
            }
        }

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public unsafe byte* lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
    }
}
