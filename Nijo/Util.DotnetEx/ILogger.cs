using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx {
    internal static class ILoggerExtension {
        // 保持しておくLoggerFactoryのインスタンス
        private static ILoggerFactory? _loggerFactory;

        internal static ILogger CreateConsoleLogger(bool verbose = true) {
            // 前回のLoggerFactoryがあれば破棄
            _loggerFactory?.Dispose();

            // 新しいLoggerFactoryを作成
            _loggerFactory = LoggerFactory.Create(builder => {
                builder.AddSimpleConsole(opt => {
                    opt.SingleLine = false;
                    opt.IncludeScopes = true;
                    opt.TimestampFormat = "yyyy/MM/dd HH:mm:ss.fff\t";
                    opt.UseUtcTimestamp = false;
                });

                if (verbose) {
                    builder.SetMinimumLevel(LogLevel.Trace);
                }

                // 標準出力が自動フラッシュされるように設定
                Console.SetOut(new AutoFlushTextWriter(Console.Out));
            });

            // プロセス終了時にLoggerFactoryを確実に破棄するためのハンドラーを追加
            AppDomain.CurrentDomain.ProcessExit += (s, e) => {
                _loggerFactory?.Dispose();
            };

            return _loggerFactory.CreateLogger(string.Empty);
        }

        // 自動フラッシュを行うTextWriterラッパー
        private class AutoFlushTextWriter : System.IO.TextWriter {
            private readonly System.IO.TextWriter _writer;

            public AutoFlushTextWriter(System.IO.TextWriter writer) {
                _writer = writer;
            }

            public override void Write(char value) {
                _writer.Write(value);
                _writer.Flush();
            }

            public override void Write(string? value) {
                _writer.Write(value);
                _writer.Flush();
            }

            public override void WriteLine(string? value) {
                _writer.WriteLine(value);
                _writer.Flush();
            }

            public override System.Text.Encoding Encoding => _writer.Encoding;
        }
    }
}
