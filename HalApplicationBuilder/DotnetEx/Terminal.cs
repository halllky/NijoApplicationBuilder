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

namespace HalApplicationBuilder.DotnetEx {
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
        internal async Task<Task> RunBackground(IEnumerable<string> command, Regex readyChecker, CancellationToken cancellationToken) {
            var logName = $"'{command.Join(" ")}'";

            // バックグラウンド処理の準備が整うまで
            var process = CreateProcess(command);
            try {
                bool ready = false;
                void CheckIfReady(object sender, DataReceivedEventArgs e) {
                    if (e.Data != null && readyChecker.IsMatch(e.Data)) ready = true;
                }
                process.OutputDataReceived += CheckIfReady;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _logger.LogInformation("{Command}: Start (PID = {PID})", logName, process.Id);

                await Task.Run(async () => {
                    while (!ready) {
                        await Task.Delay(100, cancellationToken);
                    }
                }, cancellationToken);

                process.OutputDataReceived -= CheckIfReady;

                _logger.LogInformation("{Command}: Ready", logName);

            } catch (TaskCanceledException) {
                _logger.LogInformation("{Command}: Cancelled", logName);
                await EnsureKill(process.Id, logName);
                process.Dispose();
                throw;

            } catch (Exception ex) {
                _logger.LogCritical(ex, "{Command}: Error", logName);
                await EnsureKill(process.Id, logName);
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
                        throw new InvalidOperationException($"{logName}: Exit code is '{process.ExitCode}'");
                    }
                } catch (OperationCanceledException) {
                    _logger.LogInformation("{Command}: Cancelled", logName);

                } finally {
                    await EnsureKill(process.Id, logName);
                    process.Dispose();
                }
            }, CancellationToken.None); // タスク開始前にキャンセルされてしまうとEnsureKillを通らなくなるのでCancellationToken.None
        }

        /// <summary>
        /// 新しいProcessオブジェクトを作成します。
        /// </summary>
        private Process CreateProcess(IEnumerable<string> command) {
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

            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardInputEncoding = Encoding.UTF8;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            process.OutputDataReceived += OnStdOut;
            process.ErrorDataReceived += OnStdOut;

            return process;
        }
        /// <summary>
        /// コマンドの単純実行
        /// </summary>
        private async Task ExecuteProcess(IEnumerable<string> command, Func<Process, Task> taskAwaiter) {
            var logName = $"'{command.Join(" ")}'";

            // プロセス開始
            using var process = CreateProcess(command);
            Exception? exception = null;
            int? pid = null;
            try {
                _logger.LogInformation("{Command}: Start", logName);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                pid = process.Id;

                await taskAwaiter(process);

                if (process.ExitCode != 0) {
                    exception = new InvalidOperationException($"Exit code is '{process.ExitCode}': {logName}");
                }

            } catch (TaskCanceledException) {
                _logger.LogInformation("{Command}: Cancelled", logName);
                throw;

            } catch (Exception ex) {
                _logger.LogCritical(ex, "{Command}: Error", logName);
                throw;

            } finally {
                await EnsureKill(process.Id, logName);
            }
        }
        /// <summary>
        /// プロセスツリーを確実に終了させます。
        /// </summary>
        /// <param name="pid">プロセスID</param>
        private async Task EnsureKill(int pid, string logName) {
            try {
                using var kill = CreateProcess(new[] { "taskkill", "/PID", pid.ToString(), "/T", "/F" });

                kill.Start();
                kill.BeginOutputReadLine();
                kill.BeginErrorReadLine();

                await kill.WaitForExitAsync(CancellationToken.None); // キャンセル不可

                if (kill.ExitCode == 0) {
                    _logger.LogInformation("{Command}: Success to task kill (PID = {PID})", logName, pid);
                } else {
                    _logger.LogInformation("{Command}: Exit code of TASKKILL is '{ExitCode}' (PID = {PID})", logName, kill.ExitCode, pid);
                }

            } catch (Exception ex) {
                _logger.LogError(ex, "{Command}: Failed to task kill (PID = {PID})", logName, pid);
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
