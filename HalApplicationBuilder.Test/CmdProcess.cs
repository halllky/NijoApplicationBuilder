using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Test {
    public class CmdProcess {
        public CmdProcess(string workingDirectory = null) {
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; }

        public StreamReader Start(string filename, params string[] args) {
            var startInfo = new ProcessStartInfo {
                WorkingDirectory = WorkingDirectory,
                FileName = filename,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
            };

            foreach (var arg in args) {
                startInfo.ArgumentList.Add(arg);
            }

            var process = Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0) {
                var standardError = process.StandardError.ReadToEnd();
                throw new InvalidOperationException(
                  $"プロセス\"{filename}\"の実行時にエラーが発生しました。" + Environment.NewLine +
                  $"実行時引数: " + string.Join(" ", args) + Environment.NewLine +
                  $"終了コード: " + process.ExitCode.ToString() + Environment.NewLine +
                  standardError);
            }

            return process.StandardOutput;
        }
    }
}