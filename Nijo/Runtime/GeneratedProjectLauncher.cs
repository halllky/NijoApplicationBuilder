using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.Runtime {
    public class GeneratedProjectLauncher : IDisposable {
        internal GeneratedProjectLauncher(string webapiProjectRoot, Uri webapiServerUrl, string reactProjectRoot, ILogger logger) {
            _webapiProjectRoot = webapiProjectRoot;
            _webapiServerUrl = webapiServerUrl;
            _reactProjectRoot = reactProjectRoot;
            _logger = logger;
        }

        private readonly string _webapiProjectRoot;
        private readonly Uri _webapiServerUrl;
        private readonly string _reactProjectRoot;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private E_State _state = E_State.Initialized;

        private Process? _dotnetRun;
        private bool _dotnetReady;

        private Process? _npmRun;
        private bool _npmReady;

        public event EventHandler? OnReady;
        public event EventHandler<string>? OnError;

        public void Launch() {
            lock (_lock) {
                if (_state != E_State.Initialized)
                    throw new InvalidOperationException("プロセスは既に開始されています。");

                // dotnet run が実行されるポートを npm に連携する
                var envFile = new EnvFile(_webapiServerUrl, _reactProjectRoot, _logger);
                envFile.Overwrite();

                // npm run dev
                _npmRun = new Process();
                _npmRun.StartInfo.WorkingDirectory = _reactProjectRoot;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    _npmRun.StartInfo.FileName = "powershell";
                    _npmRun.StartInfo.Arguments = "/c \"npm run dev\"";
                } else {
                    _npmRun.StartInfo.FileName = "npm";
                    _npmRun.StartInfo.Arguments = "run dev";
                }
                _npmRun.StartInfo.RedirectStandardOutput = true;
                _npmRun.StartInfo.RedirectStandardError = true;
                _npmRun.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                _npmRun.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                _npmRun.OutputDataReceived += OnNpmStdOut;
                _npmRun.ErrorDataReceived += OnNpmStdErr;

                _npmRun.Start();
                _npmRun.BeginOutputReadLine();
                _npmRun.BeginErrorReadLine();
                _logger.LogInformation("npm run   : Started. PID {PID}", _npmRun.Id);

                // dotnet run
                _dotnetRun = new Process();
                _dotnetRun.StartInfo.WorkingDirectory = _webapiProjectRoot;
                _dotnetRun.StartInfo.FileName = "dotnet";
                _dotnetRun.StartInfo.Arguments = "run --launch-profile https";
                _dotnetRun.StartInfo.RedirectStandardOutput = true;
                _dotnetRun.StartInfo.RedirectStandardError = true;
                _dotnetRun.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
                _dotnetRun.StartInfo.StandardErrorEncoding = Console.OutputEncoding;
                _dotnetRun.OutputDataReceived += OnDotnetStdOut;
                _dotnetRun.ErrorDataReceived += OnDotnetStdErr;

                _dotnetRun.Start();
                _dotnetRun.BeginOutputReadLine();
                _dotnetRun.BeginErrorReadLine();
                _logger.LogInformation("dotnet run: Started. PID {PID}", _dotnetRun.Id);

                _state = E_State.Launched;
            }
        }

        private void OnDotnetStdOut(object sender, DataReceivedEventArgs e) {
            // dotnet run の準備完了時の文字列「Now listening on:」
            if (!_dotnetReady && e.Data?.Contains("Now listening on:") == true) {
                _logger.LogInformation("dotnet run: Ready. ({Data})", e.Data?.Trim());
                _dotnetReady = true;
                OnReadyChanged();

            } else if (e.Data != null) {
                _logger.LogTrace("dotnet run: {Data}", e.Data);
            }
        }
        private void OnDotnetStdErr(object sender, DataReceivedEventArgs e) {
            if (e.Data == null) return;
            lock (_lock) {
                if (_state == E_State.Launched) _state = E_State.Ready;
            }
            _logger.LogError("dotnet run: {Data}", e.Data);
            OnError?.Invoke(this, e.Data);
        }

        private void OnNpmStdOut(object sender, DataReceivedEventArgs e) {
            // viteの準備完了時のログ「➜  Local:」
            if (!_npmReady && e.Data?.Contains("➜") == true) {
                _logger.LogInformation("npm run   : Ready. ({Data})", e.Data?.Trim());
                _npmReady = true;
                OnReadyChanged();

            } else if (e.Data != null) {
                _logger.LogTrace("npm run   : {Data}", e.Data);
            }
        }
        private void OnNpmStdErr(object sender, DataReceivedEventArgs e) {
            if (e.Data == null) return;
            lock (_lock) {
                if (_state == E_State.Launched) _state = E_State.Ready;
            }
            _logger.LogError("npm run   : {Data}", e.Data);
            OnError?.Invoke(this, e.Data);
        }

        private void OnReadyChanged() {
            lock (_lock) {
                if (!_dotnetReady) return;
                if (!_npmReady) return;
                if (_state >= E_State.Ready) return;

                _state = E_State.Ready;
            }
            OnReady?.Invoke(this, EventArgs.Empty);
        }

        public void WaitForReady(TimeSpan? timeout = null) {
            var to = timeout ?? TimeSpan.FromSeconds(180);
            var current = TimeSpan.Zero;
            var interval = TimeSpan.FromSeconds(1);
            while (_state < E_State.Ready) {
                Thread.Sleep(interval);
                current += interval;
                if (current > to) throw new TimeoutException();
            }
        }
        public void WaitForTerminate() {
            if (_state == E_State.Initialized) throw new InvalidOperationException("プロセスが開始されていません。");
            if (_state == E_State.Stopped) return;

            var task1 = Task.Run(() => _dotnetRun?.WaitForExit());
            var task2 = Task.Run(() => _npmRun?.WaitForExit());
            Task.WaitAll(task1, task2);
        }
        public void Terminate() {
            lock (_lock) {
                if (_state == E_State.Stopped) return;

                _logger.LogInformation("Now terminating debug process ...");

                if (_npmRun != null) _logger.LogInformation("npm run   : {msg}", _npmRun.EnsureKill());
                if (_dotnetRun != null) _logger.LogInformation("dotnet run: {msg}", _dotnetRun.EnsureKill());
                _state = E_State.Stopped;
                _logger.LogInformation("Prosess is terminated.");
            }
        }

        public void Dispose() {
            Terminate();
        }

        private enum E_State {
            /// <summary>
            /// このクラスのインスタンスが作成されてから <see cref="Launch"/> でプロセスが開始されるまでの間
            /// </summary>
            Initialized = 0,
            /// <summary>
            /// <see cref="Launch"/> でプロセスが開始されてからnpmとdotnetの両プロセスが起動されるまでの間
            /// </summary>
            Launched = 1,
            /// <summary>
            /// npmとdotnetの両プロセスが起動されてからデバッグが停止されるまでの間
            /// </summary>
            Ready = 2,
            /// <summary>
            /// デバッグが停止されて以降
            /// </summary>
            Stopped = 3,
        }
    }
}
