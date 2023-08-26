using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {

    internal class BackgroundProcess : IDisposable {

        public required string WorkingDirectory { get; init; }
        public required string Filename { get; init; }
        public required string[] Args { get; init; }
        public required CancellationToken CancellationToken { get; init; }

        public event EventHandler<ProcessEx.LogEventArgs>? Log;

        private ProcessEx? _processEx = null;
        private Task? _task = null;

        /// <summary>
        /// 準備完了か否かを判定する関数。
        /// 例えばWebサーバーが立ち上がる前にHTTPリクエストが投げられてテストが落ちるといったことを防ぐためのもの。
        /// </summary>
        public required Func<ProcessEx.LogEventArgs, bool> IsReady { get; init; }
        private bool _ready = false;

        private bool _stopped = false;

        public async Task Launch() {
            if (_processEx != null || _stopped) throw new InvalidOperationException();

            Console.WriteLine($"{Filename} {string.Join(" ", Args)}");

            _processEx = new ProcessEx(WorkingDirectory, Filename, Args);
            _processEx.Log += OnStdOut;

            _task = _processEx.StartAsync();

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
                    if (_task.IsCompleted) {
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

                if (_processEx != null) {
                    _processEx.Dispose();
                    _processEx = null;
                }
            }
        }
        public void Dispose() {
            Stop();
        }

        private void OnStdOut(object? sender, ProcessEx.LogEventArgs e) {
            if (!_ready && IsReady(e)) _ready = true;
            Log?.Invoke(sender, e);
        }
    }
}

