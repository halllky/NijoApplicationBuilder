using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {
    internal class ExternalProcess {
        internal ExternalProcess(string workingDirectory, CancellationToken? cancellationToken = null) {
            WorkingDirectory = workingDirectory;
            CancellationToken = cancellationToken;
        }

        internal string WorkingDirectory { get; }
        internal CancellationToken? CancellationToken { get; }

        internal void Start(string filename, params string[] args) {
            using var process = CreateProcess(filename, args);
            try {
                CancellationToken?.ThrowIfCancellationRequested();

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.HasExited) {
                    CancellationToken?.ThrowIfCancellationRequested();
                    Thread.Sleep(100);
                }
                if (process.ExitCode != 0) {
                    throw new InvalidOperationException($"外部コマンド( {filename} {string.Join(" ", args)})実行時にエラーが発生しました。");
                }

            } catch (OperationCanceledException) {
                process.Kill(entireProcessTree: true);

                // Killの完了前に新規プロジェクト作成時の一時ディレクトリの削除処理が走ってしまい
                // 削除できなくて失敗することがあるので少し待つ
                Thread.Sleep(1000);

                throw;

            } catch (Exception ex) {
                throw new Exception($"EXCEPTION: '{filename} {string.Join(" ", args)}'", ex);
            }
        }

        internal async Task StartAsync(string filename, params string[] args) {
            if (CancellationToken == null)
                throw new InvalidOperationException($"{nameof(StartAsync)}の場合は{nameof(CancellationToken)}必須");

            await Task.Run(() => {
                using var process = CreateProcess(filename, args);
                try {
                    CancellationToken?.ThrowIfCancellationRequested();

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    while (!process.HasExited) {
                        CancellationToken?.ThrowIfCancellationRequested();
                        Thread.Sleep(100);
                    }
                } catch (OperationCanceledException) {
                    process.Kill(entireProcessTree: true);
                    throw;
                }
            });
        }

        private Process CreateProcess(string filename, params string[] args) {
            var process = new System.Diagnostics.Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // windowsでnvmを使うとき直にnpmを実行できないためcmd経由で実行する
                process.StartInfo.FileName = "cmd";
                process.StartInfo.ArgumentList.Add("/c");
                process.StartInfo.ArgumentList.Add($"{filename} {string.Join(" ", args)}");
            } else {
                process.StartInfo.FileName = filename;
                foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);
            }
            process.StartInfo.WorkingDirectory = WorkingDirectory;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
            process.StartInfo.StandardErrorEncoding = Console.OutputEncoding;
            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

            return process;
        }
    }
}
