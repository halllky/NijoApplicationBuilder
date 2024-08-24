using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.Logging {
    internal class DefaultLogger {
        internal const string CLASSNAME = "DefaultLogger";

        internal static SourceFile Render(CodeRenderingContext ctx) => new SourceFile {
            FileName = "DefaultLogger.cs",
            RenderContent = context => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using Microsoft.Extensions.Logging;
                    using System;
                    using System.IO;
                    using System.Text;

                    public class {{CLASSNAME}} : ILogger {
                        public {{CLASSNAME}}(string? logDirectory) {
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

                        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
                            if (!_directoryCraeted) {
                                if (!Directory.Exists(_logDirectory)) {
                                    Directory.CreateDirectory(_logDirectory);
                                }
                                _directoryCraeted = true;
                            }

                            var now = DateTime.Now;
                            var file = Path.Combine(_logDirectory, $"{now:yyyyMMdd}.log");
                            using var fileStream = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                            using var streamWriter = new StreamWriter(fileStream, encoding: Encoding.UTF8) { AutoFlush = true };
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
                """,
        };
    }
}
