using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.Runtime {
    public class GeneratedProjectLauncher : IDisposable {
        internal GeneratedProjectLauncher(string webapiProjectRoot, string reactProjectRoot, Uri webapiServerUrl, Uri npmServerUrl, ILogger logger) {
            _webapiProjectRoot = webapiProjectRoot;
            _reactProjectRoot = reactProjectRoot;
            _webapiServerUrl = webapiServerUrl;
            _npmServerUrl = npmServerUrl;
            _logger = logger;
        }

        private readonly string _webapiProjectRoot;
        private readonly string _reactProjectRoot;
        private readonly Uri _webapiServerUrl;
        private readonly Uri _npmServerUrl;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private readonly HttpClient _httpClient = new HttpClient() {
            // タイムアウトを短めに設定 (待機ループでリトライするため)
            Timeout = TimeSpan.FromSeconds(5)
        };

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
                // エンコーディングはConsole.OutputEncodingに合わせる
                _npmRun.StartInfo.StandardOutputEncoding = Console.OutputEncoding;
                _npmRun.StartInfo.StandardErrorEncoding = Console.OutputEncoding;
                _npmRun.OutputDataReceived += OnNpmStdOut; // 標準出力のハンドラはログ用に残す
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
                if (_state == E_State.Launched) _state = E_State.Ready; // エラー発生時もReadyにしてループを抜けるようにする（暫定）
            }
            _logger.LogError("dotnet run: {Data}", e.Data);
            OnError?.Invoke(this, e.Data);
        }

        private void OnNpmStdOut(object sender, DataReceivedEventArgs e) {
            // 標準出力の準備完了判定ロジックを削除
            if (e.Data != null) {
                _logger.LogTrace("npm run   : {Data}", e.Data);
            }
        }
        private void OnNpmStdErr(object sender, DataReceivedEventArgs e) {
            if (e.Data == null) return;
            lock (_lock) {
                if (_state == E_State.Launched) _state = E_State.Ready; // エラー発生時もReadyにしてループを抜けるようにする（暫定）
            }
            _logger.LogError("npm run   : {Data}", e.Data);
            OnError?.Invoke(this, e.Data);
        }

        private void OnReadyChanged() {
            lock (_lock) {
                if (!_dotnetReady) return;
                if (!_npmReady) return; // ここは _npmReady を見る
                if (_state >= E_State.Ready) return;

                _state = E_State.Ready;
            }
            OnReady?.Invoke(this, EventArgs.Empty);
        }

        // WaitForReadyメソッドを修正
        public void WaitForReady(TimeSpan? timeout = null) {
            var to = timeout ?? TimeSpan.FromSeconds(180);
            var sw = Stopwatch.StartNew(); // 時間計測用にStopwatchを使用
            var interval = TimeSpan.FromSeconds(1);

            while (_state < E_State.Ready) {
                // タイムアウトチェック
                if (sw.Elapsed > to) throw new TimeoutException($"サーバーの起動がタイムアウトしました ({to.TotalSeconds}秒). Dotnet Ready: {_dotnetReady}, Npm Ready: {_npmReady}");

                // npmサーバーの準備がまだならHTTPリクエストを試行
                if (!_npmReady) {
                    try {
                        // 非同期メソッドを同期的に呼び出す（待機ループのため許容）
                        var response = _httpClient.GetAsync(_npmServerUrl).GetAwaiter().GetResult();

                        if (response.IsSuccessStatusCode) {
                            _logger.LogInformation("npm run   : Ready. (HTTP check successful to {Url})", _npmServerUrl);
                            _npmReady = true;
                            OnReadyChanged(); // 状態が変わった可能性があるので呼び出す
                        } else {
                            // ステータスコードが200系でない場合（まだ起動中など）
                            _logger.LogTrace("npm run   : HTTP check to {Url} failed with status {StatusCode}.", _npmServerUrl, response.StatusCode);
                        }
                    } catch (HttpRequestException ex) {
                        // サーバーがまだリクエストを受け付けられない場合など
                        _logger.LogTrace("npm run   : HTTP check to {Url} failed: {Message}", _npmServerUrl, ex.Message);
                    } catch (Exception ex) {
                        // その他の予期せぬエラー
                        _logger.LogError(ex, "npm run   : Error during HTTP check to {Url}.", _npmServerUrl);
                        // エラー発生時はループを抜けるか、リトライするか検討が必要。ここではループ継続。
                    }
                }

                // 次のチェックまで待機
                Thread.Sleep(interval);
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

                _httpClient.Dispose();
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
