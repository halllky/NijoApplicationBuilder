using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Test
{
    public class CmdProcess
    {
        public CmdProcess(string workingDirectory = null)
        {
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; }

        public string Start(string filename, params string[] args)
        {
            using var process = new Process();
            process.StartInfo.WorkingDirectory = WorkingDirectory;
            process.StartInfo.FileName = filename;
            foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);

            process.StartInfo.UseShellExecute = false;

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.OutputDataReceived += (sender, e) => standardOutput.AppendLine(e.Data);
            process.ErrorDataReceived += (sender, e) => standardError.AppendLine(e.Data);

            Console.WriteLine($"PROCESS START: {filename} {string.Join(" ", args)}");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            Console.WriteLine($"PROCESS END:   {filename} {string.Join(" ", args)}");

            if (process.ExitCode != 0 || standardError.Length > 0)
            {
                throw new InvalidOperationException(
                  $"プロセス\"{filename}\"の実行時にエラーが発生しました。" + Environment.NewLine +
                  $"実行時引数: " + string.Join(" ", args) + Environment.NewLine +
                  $"終了コード: " + process.ExitCode.ToString() + Environment.NewLine +
                  $"========== standard output =========" + Environment.NewLine +
                  standardOutput.ToString() + Environment.NewLine +
                  $"========== standard error ==========" + Environment.NewLine +
                  standardError.ToString() + Environment.NewLine +
                  $"====================================" + Environment.NewLine);
            }

            return standardOutput.ToString();
        }
    }
}