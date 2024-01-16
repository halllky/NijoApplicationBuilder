using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx {
    internal class Terminal {

        internal Terminal(string workingDirectory, ILogger logger) {
            _workingDirectory = workingDirectory;
            _logger = logger;
        }

        private readonly string _workingDirectory;
        private readonly ILogger _logger;

        /// <summary>
        /// コマンドの単純実行
        /// </summary>
        internal async Task Run(IEnumerable<string> command, CancellationToken cancellationToken) {
            await ExecuteProcess(command, async process => {
                await process.WaitForExitAsync(cancellationToken);
            });
        }
        /// <summary>
        /// コマンドを実行し標準出力を読み取り
        /// </summary>
        internal async Task<IEnumerable<string>> RunAndReadOutput(IEnumerable<string> command, CancellationToken cancellationToken) {
            var result = new List<string>();
            void OnDataReceived(object sender, DataReceivedEventArgs e) {
                if (!string.IsNullOrWhiteSpace(e.Data)) result.Add(e.Data);
            }

            await ExecuteProcess(command, async process => {
                process.OutputDataReceived += OnDataReceived;
                await process.WaitForExitAsync(cancellationToken);
            });

            return result;
        }
        /// <summary>
        /// バックグラウンドで実行される処理を開始します。
        /// </summary>
        /// <param name="readyChecker">準備が完了したかどうかを標準出力に照らし合わせて判定します。</param>
        /// <returns>1こめのTaskは準備完了までを表すタスク。2こめのTaskはキャンセルされるまで永続的に実行されるタスク。</returns>
        internal async Task<Task> RunBackground(
            IEnumerable<string> command,
            Regex readyChecker,
            Encoding outputEncoding,
            CancellationToken cancellationToken) {

            using var logScope = _logger.BeginScope($"'{command.Join(" ")}'");

            // バックグラウンド処理の準備が整うまで
            var process = CreateProcess(command, outputEncoding);
            try {
                bool ready = false;
                void CheckIfReady(object sender, DataReceivedEventArgs e) {
                    if (e.Data != null && readyChecker.IsMatch(e.Data)) ready = true;
                }
                process.OutputDataReceived += CheckIfReady;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _logger.LogInformation("Start (PID = {PID})", process.Id);

                await Task.Run(async () => {
                    while (!ready) {
                        await Task.Delay(100, cancellationToken);
                    }
                }, cancellationToken);

                process.OutputDataReceived -= CheckIfReady;

                _logger.LogInformation("Ready");

            } catch (TaskCanceledException) {
                _logger.LogInformation("Cancelled");
                await EnsureKill(process);
                process.Dispose();
                throw;

            } catch (Exception ex) {
                _logger.LogCritical(ex, "Error");
                await EnsureKill(process);
                process.Dispose();
                throw;
            }

            // バックグラウンド処理の準備が整ったあと
            return Task.Run(async () => {
                try {
                    while (!cancellationToken.IsCancellationRequested) {
                        await Task.Delay(100, cancellationToken);
                    }
                    if (process.ExitCode != 0) {
                        throw new InvalidOperationException($"Exit code is '{process.ExitCode}'");
                    }
                } catch (OperationCanceledException) {
                    _logger.LogInformation("Cancelled");

                } finally {
                    await EnsureKill(process);
                    process.Dispose();
                }
            }, CancellationToken.None); // タスク開始前にキャンセルされてしまうとEnsureKillを通らなくなるのでCancellationToken.None
        }

        /// <summary>
        /// 新しいProcessオブジェクトを作成します。
        /// </summary>
        private Process CreateProcess(IEnumerable<string> command, Encoding? outputEncoding = null) {
            // 引数解釈
            if (!command.Any()) throw new ArgumentException("Command must contain more than 1.", nameof(command));
            var filename = command.First();
            var args = command.Skip(1).ToArray();

            var process = new Process();

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
            process.StartInfo.WorkingDirectory = _workingDirectory;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = outputEncoding;
            process.StartInfo.StandardErrorEncoding = outputEncoding;

            process.OutputDataReceived += OnStdOut;
            process.ErrorDataReceived += OnStdErr;

            return process;
        }
        /// <summary>
        /// コマンドの単純実行
        /// </summary>
        private async Task ExecuteProcess(IEnumerable<string> command, Func<Process, Task> taskAwaiter) {
            using var logScope = _logger.BeginScope($"'{command.Join(" ")}'");

            // プロセス開始
            using var process = CreateProcess(command);
            int? pid = null;
            try {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _logger.LogInformation("Start (PID = {PID})", process.Id);

                pid = process.Id;

                await taskAwaiter(process);

                if (process.ExitCode != 0) {
                    throw new InvalidOperationException($"Exit code is '{process.ExitCode}'");
                }

            } catch (TaskCanceledException) {
                _logger.LogInformation("Cancelled");
                throw;

            } catch (Exception ex) {
                _logger.LogCritical(ex, "Error");
                throw;

            } finally {
                await EnsureKill(process);
            }
        }
        /// <summary>
        /// プロセスツリーを確実に終了させます。
        /// </summary>
        /// <param name="process">プロセスID</param>
        private async Task EnsureKill(Process process) {
            int? pid = null;
            try {
                if (process.HasExited) return;

                pid = process.Id;

                using var kill = CreateProcess(new[] { "taskkill", "/PID", pid.ToString()!, "/T", "/F" });

                kill.Start();
                kill.BeginOutputReadLine();
                kill.BeginErrorReadLine();

                await kill.WaitForExitAsync(CancellationToken.None); // キャンセル不可

                if (kill.ExitCode == 0) {
                    _logger.LogInformation("Success to task kill (PID = {PID})", pid);
                } else {
                    _logger.LogInformation("Exit code of TASKKILL is '{ExitCode}' (PID = {PID})", kill.ExitCode, pid);
                }

            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to task kill (PID = {PID})", pid);
            }
        }

        private void OnStdOut(object sender, DataReceivedEventArgs e) {
            if (!string.IsNullOrWhiteSpace(e.Data)) _logger.LogTrace("{Data}", e.Data);
        }
        private void OnStdErr(object sender, DataReceivedEventArgs e) {
            if (!string.IsNullOrWhiteSpace(e.Data)) _logger.LogError("{Data}", e.Data);
        }
    }
}