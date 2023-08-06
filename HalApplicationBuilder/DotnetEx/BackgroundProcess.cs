using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {

    public class BackgroundProcess : IDisposable {

        public required string WorkingDirectory { get; init; }
        public required string Filename { get; init; }
        public required string[] Args { get; init; }
        public required CancellationToken CancellationToken { get; init; }

        public event DataReceivedEventHandler? OnStandardOut;
        public event DataReceivedEventHandler? OnStandardError;

        private Process? _process = null;

        /// <summary>
        /// 準備完了か否かを判定する関数。
        /// 例えばWebサーバーが立ち上がる前にHTTPリクエストが投げられてテストが落ちるといったことを防ぐためのもの。
        /// </summary>
        public required Func<DataReceivedEventArgs, bool> IsReady { get; init; }
        private bool _ready = false;

        private bool _stopped = false;

        public async Task Launch() {
            if (_process != null || _stopped) throw new InvalidOperationException();

            Console.WriteLine($"{Filename} {string.Join(" ", Args)}");

            _process = Cmd.CreateProcess(WorkingDirectory, Filename, Args, OnStdOut, OnStandardError);

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            await Task.Run(() => {
                while (true) {
                    if (_ready) return;
                    if (_stopped) return;
                    if (CancellationToken.IsCancellationRequested) return;
                    Thread.Sleep(100);
                }
            });

            _ = Task.Run(() => {
                while (true) {
                    if (_process.HasExited) {
                        Stop();
                        return;
                    }
                    if (_stopped) return;
                    if (CancellationToken.IsCancellationRequested) return;
                    Thread.Sleep(100);
                }
            });
        }

        private readonly object _lock = new object();
        public void Stop() {
            lock (_lock) {
                if (_stopped) return;
                _stopped = true;

                if (_process != null) {
                    try {
                        Console.WriteLine($"KILLING PROCESS: {Filename} {Args.Join(" ")}");
                        _process.Kill(entireProcessTree: true);
                    } catch (InvalidOperationException ex) when (ex.Message == "No process is associated with this object.") {
                        // Processインスタンスが作成されてからStartする前にDisposeされるとこの例外が発生する。
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

        private void OnStdOut(object sender, DataReceivedEventArgs e) {
            if (!_ready && IsReady(e)) _ready = true;
            OnStandardOut?.Invoke(sender, e);
        }
    }
}

