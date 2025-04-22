using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Nijo.Mcp;

public static class JobObjectHelper {
    // 定数
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
    private const int JOB_OBJECT_ALL_ACCESS = 0x1F001F;
    private const uint JOB_OBJECT_TERMINATE = 0x0008;

    // Win32 API 宣言
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob,
        JOBOBJECTINFOCLASS infoClass,
        IntPtr lpJobObjectInfo,
        uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenJobObject(uint dwDesiredAccess, bool bInheritHandles, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateJobObject(IntPtr hJob, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    // ジョブ情報設定用構造体
    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    private enum JOBOBJECTINFOCLASS {
        BasicLimitInformation = 2,
        ExtendedLimitInformation = 9,
    }

    /// <summary>
    /// 新規ジョブを作成し、プロセスをアサインします。
    /// </summary>
    /// <param name="jobName">再起動後にも同じ名前で開けるようユニークな名前を付ける</param>
    /// <param name="process">アサインしたい Process インスタンス</param>
    /// <returns>ジョブハンドル</returns>
    public static IntPtr CreateAndAssignJob(string jobName, Process process) {
        // ジョブ作成
        var hJob = CreateJobObject(IntPtr.Zero, jobName);
        if (hJob == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateJobObject failed");

        // プロセスをジョブにアサイン
        if (!AssignProcessToJobObject(hJob, process.Handle))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "AssignProcessToJobObject failed");

        return hJob;
    }

    /// <summary>
    /// 既存ジョブを開き、Terminate で一括終了します。
    /// </summary>
    /// <param name="jobName">Create 時と同じジョブ名</param>
    public static bool TryKillJobByName(string jobName) {
        // ジョブを開く（Terminate 権限だけあればOK）
        var hJob = OpenJobObject(JOB_OBJECT_TERMINATE, false, jobName);
        if (hJob == IntPtr.Zero) {
            return false; // OpenJobObject failed
        }

        try {
            if (!TerminateJobObject(hJob, 1))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "TerminateJobObject failed");

            return true;
        } finally {
            CloseHandle(hJob);
        }
    }
}
