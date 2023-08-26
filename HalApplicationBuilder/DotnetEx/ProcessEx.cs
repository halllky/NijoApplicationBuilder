using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {
    internal class ProcessEx : IDisposable {

        internal ProcessEx(string workingDirectory, string filename, params string[] args) {
            _process = new Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // powershell経由で実行する。
                // - windowsでnvmを使うとき直にnpmを実行できないため
                // - cmdだと、npm start を終了するときCtrl+Cを2回やらないといけない（1回だけだと "Terminate batch job? (Y/N)" が出るためWebサーバーが止まらない）
                _process.StartInfo.FileName = "powershell";
                _process.StartInfo.ArgumentList.Add("/c");
                _process.StartInfo.ArgumentList.Add($"{filename} {string.Join(" ", args)}");
            } else {
                _process.StartInfo.FileName = filename;
                foreach (var arg in args) _process.StartInfo.ArgumentList.Add(arg);
            }

            _process.StartInfo.WorkingDirectory = workingDirectory;

            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.StandardInputEncoding = Encoding.UTF8;
            _process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            _process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            _process.OutputDataReceived += OnStdOut;
            _process.ErrorDataReceived += OnStdErr;
        }

        private readonly Process _process;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly StringBuilder _error = new StringBuilder();

        internal event EventHandler<LogEventArgs>? Log;
        internal class LogEventArgs {
            internal required string Message { get; init; }
            internal required bool IsError { get; init; }
        }

        internal void Start(CancellationToken? cancellationToken = null) {
            StartAsync(cancellationToken).Wait();
        }
        internal async Task StartAsync(CancellationToken? cancellationToken = null) {
            if (disposedValue) throw new InvalidOperationException("This object is disposed.");
            await _semaphore.WaitAsync();

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            try {
                await Task.Run(() => {
                    while (true) {
                        if (_process.HasExited || cancellationToken?.IsCancellationRequested == true) break;
                        Thread.Sleep(100);
                    }
                    if (_process.ExitCode != 0) {
                        throw new InvalidOperationException($"Exit code of command '{_process.StartInfo.FileName} {_process.StartInfo.ArgumentList.Join(" ")}' is not Zero: {_process.ExitCode}{Environment.NewLine}{_error}");
                    }
                });
            } finally {
                _semaphore.Release();
                Dispose();
            }
        }

        private bool _returnsOutputImmediately = false;
        private readonly Queue<string> _stdOut = new Queue<string>();
        internal IEnumerable<string> Read(CancellationToken? cancellationToken = null) {
            if (disposedValue) throw new InvalidOperationException("This object is disposed.");
            _semaphore.Wait();

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _returnsOutputImmediately = true;

            try {
                while (true) {
                    while (_stdOut.Count > 0) yield return _stdOut.Dequeue();
                    if (_process.HasExited || cancellationToken?.IsCancellationRequested == true) break;
                    Thread.Sleep(100);
                }
                if (_process.ExitCode != 0) {
                    throw new InvalidOperationException($"Exit code of command '{_process.StartInfo.FileName} {_process.StartInfo.ArgumentList.Join(" ")}' is not Zero: {_process.ExitCode}{Environment.NewLine}{_error}");
                }
            } finally {
                _semaphore.Release();
                Dispose();
            }
        }

        private void OnStdOut(object sender, DataReceivedEventArgs e) {
            if (string.IsNullOrWhiteSpace(e.Data)) {
                return;
            }
            if (_returnsOutputImmediately) {
                _stdOut.Enqueue(e.Data);
            }
            Log?.Invoke(this, new LogEventArgs {
                IsError = false,
                Message = e.Data,
            });
        }
        private void OnStdErr(object sender, DataReceivedEventArgs e) {
            if (string.IsNullOrWhiteSpace(e.Data)) {
                return;
            }

            _error.AppendLine(e.Data);

            Log?.Invoke(this, new LogEventArgs {
                IsError = true,
                Message = e.Data,
            });
        }

        private bool disposedValue;
        public void Dispose() {
            if (!disposedValue) {
                Log?.Invoke(this, new LogEventArgs {
                    IsError = false,
                    Message = $"Kill process: '{_process.StartInfo.FileName} {_process.StartInfo.ArgumentList.Join(" ")}'",
                });

                _process.Kill(entireProcessTree: true);
                _process.Dispose();

                disposedValue = true;
                GC.SuppressFinalize(this);
            }
        }
        ~ProcessEx() {
            Dispose();
        }
    }
}
