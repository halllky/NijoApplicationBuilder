using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {
    internal class Cmd {
        internal Cmd(string workingDirectory, CancellationToken? cancellationToken = null) {
            WorkingDirectory = workingDirectory;
            CancellationToken = cancellationToken;
        }

        internal string WorkingDirectory { get; }
        internal CancellationToken? CancellationToken { get; }

        internal void Exec(string filename, params string[] args) {
            Execute(null, filename, args);
        }
        internal string ReadOutput(string filename, params string[] args) {
            var output = new Queue<string>();
            Execute(output, filename, args);
            return string.Join(Environment.NewLine, output);
        }
        internal IEnumerable<string> ReadOutputs(string filename, params string[] args) {
            var output = new Queue<string>();
            Execute(output, filename, args);
            while (output.Count > 0) {
                yield return output.Dequeue();
            }
        }
        private void Execute(Queue<string>? output, string filename, params string[] args) {
            DataReceivedEventHandler stdout = output == null
                ? (_, e) => Console.WriteLine(e.Data)
                : (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) output.Enqueue(e.Data); };
            using var process = CreateProcess(WorkingDirectory, filename, args, stdout);

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

        internal class Background : IDisposable {

            internal required CancellationToken CancellationToken { get; init; }
            internal required string WorkingDirectory { get; init; }
            internal required string Filename { get; init; }
            internal required string[] Args { get; init; }

            private Process? _process = null;

            internal void Restart() {
                var _ = Task.Run(() => {
                    try {
                        Stop();

                        _process = CreateProcess(WorkingDirectory, Filename, Args, (_, e) => Console.WriteLine(e.Data));
                        CancellationToken.ThrowIfCancellationRequested();

                        _process.Start();
                        _process.BeginOutputReadLine();
                        _process.BeginErrorReadLine();

                        while (!_process.HasExited) {
                            CancellationToken.ThrowIfCancellationRequested();
                            Thread.Sleep(100);
                        }
                    } catch (OperationCanceledException) {
                        Stop();
                        throw;
                    }
                });
            }
            private readonly object _lock = new object();
            internal void Stop() {
                lock (_lock) {
                    if (_process == null) return;
                    _process.Kill(entireProcessTree: true);
                    _process.Dispose();
                    _process = null;
                }
            }
            public void Dispose() {
                Stop();
            }
        }

        private static Process CreateProcess(string workingDirectory, string filename, string[] args, DataReceivedEventHandler stdout) {
            var process = new System.Diagnostics.Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // powershell経由で実行する。
                // - windowsでnvmを使うとき直にnpmを実行できないため
                // - cmdだと、npm start を終了するときCtrl+Cを2回やらないといけない（1回だけだと "Terminate batch job? (Y/N)" が出るためWebサーバーが止まらない）
                process.StartInfo.FileName = "powershell";
                process.StartInfo.ArgumentList.Add("/c");
                process.StartInfo.ArgumentList.Add($"{filename} {string.Join(" ", args)}");
            } else {
                process.StartInfo.FileName = filename;
                foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);
            }
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
            process.StartInfo.StandardErrorEncoding = Console.OutputEncoding;
            process.OutputDataReceived += stdout;
            process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

            return process;
        }
    }
}
