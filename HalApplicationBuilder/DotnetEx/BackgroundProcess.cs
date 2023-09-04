using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Reflection;
using System.Linq;

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

        // -------------------------------------
        public class DefaultLogger : ILogger {
            public DefaultLogger(string? logDirectory) {
                _logDirectory = logDirectory ?? "job";
            }
            private readonly string _logDirectory;
            private bool _directoryCraeted = false;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
                return default;
            }

            public bool IsEnabled(LogLevel logLevel) {
                return true;
            }

            private static object _lock = new object();

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
                if (!_directoryCraeted) {
                    if (!Directory.Exists(_logDirectory)) {
                        Directory.CreateDirectory(_logDirectory);
                    }
                    _directoryCraeted = true;
                }

                var now = DateTime.Now;
                var file = Path.Combine(_logDirectory, $"{now:yyyyMMdd}.log");
                lock (_lock) {
                    using var streamWriter = new StreamWriter(file, append: true, encoding: Encoding.UTF8);
                    using var textWriter = TextWriter.Synchronized(streamWriter);

                    var header = $"{now:G}\t[{logLevel}]";
                    textWriter.WriteLine($"{header}\t{formatter(state, exception)}");

                    if (exception != null) {
                        textWriter.WriteLine($"");
                        textWriter.WriteLine($"{exception}");
                        textWriter.WriteLine($"");
                    }
                }
            }
        }
    }
}

