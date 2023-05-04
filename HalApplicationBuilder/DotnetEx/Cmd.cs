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

        internal required string WorkingDirectory { get; init; }
        internal CancellationToken? CancellationToken { get; init; }
        internal bool Verbose { get; init; }

        internal void Exec(string filename, params string[] args) {
            Console.WriteLine($"{filename} {string.Join(" ", args)}");
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

            DataReceivedEventHandler? stdout;
            if (output != null)
                stdout = (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) output.Enqueue(e.Data); };
            else if (Verbose)
                stdout = OutToConsole;
            else
                stdout = null;

            using var process = CreateProcess(WorkingDirectory, filename, args, stdout);

            try {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.HasExited) {
                    Thread.Sleep(100);
                    CancellationToken?.ThrowIfCancellationRequested();
                }
                if (process.ExitCode != 0) {
                    throw new InvalidOperationException($"外部コマンド( {filename} {string.Join(" ", args)})実行時にエラーが発生しました。");
                }

            } catch (OperationCanceledException) {
                process.Kill(entireProcessTree: true);

                // Killの完了前に新規プロジェクト作成時の一時ディレクトリの削除処理が走ってしまい
                // 削除できなくて失敗することがあるので少し待つ
                Thread.Sleep(1000);

            } catch (Exception ex) {
                throw new Exception($"EXCEPTION: '{filename} {string.Join(" ", args)}'", ex);
            }
        }

        internal class Background : IDisposable {

            internal required string WorkingDirectory { get; init; }
            internal required string Filename { get; init; }
            internal required string[] Args { get; init; }
            internal required CancellationToken CancellationToken { get; init; }
            internal bool Verbose { get; init; }

            private Process? _process = null;

            /// <summary>複数回Restartしたときに古いTaskが残り続けてしまうのを避けるためのもの</summary>
            private CancellationTokenSource? _taskCanceller = null;

            internal void Restart() {
                Console.WriteLine($"{Filename} {string.Join(" ", Args)}");

                Stop();

                CancellationToken ct;
                lock (_lock) {
                    DataReceivedEventHandler? stdout = Verbose ? OutToConsole : null;
                    _process = CreateProcess(WorkingDirectory, Filename, Args, stdout);

                    _process.Start();
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();

                    _taskCanceller = new CancellationTokenSource();
                    ct = _taskCanceller.Token;
                }

                var _ = Task.Run(() => {
                    try {
                        while (!_process.HasExited) {
                            Thread.Sleep(100);
                            ct.ThrowIfCancellationRequested();
                            CancellationToken.ThrowIfCancellationRequested();
                        }
                    } catch (OperationCanceledException) {
                        Stop();
                    }
                }, ct);
            }
            private readonly object _lock = new object();
            internal void Stop() {
                lock (_lock) {
                    if (_taskCanceller != null) {
                        _taskCanceller.Cancel();
                        _taskCanceller.Dispose();
                        _taskCanceller = null;
                    }
                    if (_process != null) {
                        try {
                            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
                        } catch (InvalidOperationException ex) when (ex.Message == "No process is associated with this object.") {
                            // Processインスタンスが作成されてからStartする前にDisposeされると
                            // HasExitedを参照することはできずにこの例外が発生する。
                            // 開始されていないのでKillできなくとも問題ないと判断し、無視して先に進む
                        }
                        _process.Dispose();
                        _process = null;
                    }
                }
            }
            public void Dispose() {
                Stop();
            }
        }

        private static Process CreateProcess(string workingDirectory, string filename, string[] args, DataReceivedEventHandler? stdout) {
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
            if (stdout != null) process.OutputDataReceived += stdout;
            process.ErrorDataReceived += OutToStdError;

            return process;
        }

        private static void OutToConsole(object sender, DataReceivedEventArgs e) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(e.Data);
            Console.ResetColor();
        }
        private static void OutToStdError(object sender, DataReceivedEventArgs e) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(e.Data);
            Console.ResetColor();
        }
    }
}