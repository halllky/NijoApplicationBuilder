using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx {
    internal static class ProcessExtension {

        /// <summary>
        /// プロセスツリーを確実に終了させます。
        /// </summary>
        /// <returns>処理結果</returns>
        internal static string EnsureKill(this Process process) {
            int? pid = null;
            try {
                if (process.HasExited) return "Process is already exited. taskkill is skipped.";

                pid = process.Id;

                var kill = new Process();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    kill.StartInfo.FileName = "taskkill";
                    kill.StartInfo.ArgumentList.Add("/PID");
                    kill.StartInfo.ArgumentList.Add(pid.ToString()!);
                    kill.StartInfo.ArgumentList.Add("/T");
                    kill.StartInfo.ArgumentList.Add("/F");
                } else {
                    kill.StartInfo.FileName = "kill";
                    kill.StartInfo.ArgumentList.Add(pid.ToString()!);
                }
                kill.StartInfo.RedirectStandardOutput = true;
                kill.StartInfo.RedirectStandardError = true;

                kill.Start();
                kill.WaitForExit(TimeSpan.FromSeconds(5));

                if (kill.ExitCode == 0) {
                    return $"Success to task kill (PID = {pid})";
                } else {
                    return $"Exit code of TASKKILL is '{kill.ExitCode}' (PID = {pid})";
                }

            } catch (Exception ex) {
                return $"Failed to task kill (PID = {pid}): {ex.Message}";
            }
        }

    }
}
