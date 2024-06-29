using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BackgroundService {
    partial class BgTaskFeature {
        private static SourceFile RenderbgTaskContext() => new() {
            FileName = "BackgroundTaskContext.cs",
            RenderContent = ctx => {
                var appSrv = new ApplicationService();
                var dbContextFullName = $"{ctx.Config.DbContextNamespace}.{ctx.Config.DbContextName}";

                return $$"""
                    namespace {{ctx.Config.RootNamespace}} {
                        using Microsoft.Extensions.DependencyInjection;
                        using Microsoft.Extensions.Logging;

                        public sealed class BackgroundTaskContextFactory {
                            public BackgroundTaskContextFactory(DateTime startTime, IServiceProvider serviceProvider, string directory) {
                                _startTime = startTime;
                                _serviceProvider = serviceProvider;
                                _directory = directory;
                            }
                            private readonly DateTime _startTime;
                            private readonly IServiceProvider _serviceProvider;
                            private readonly string _directory;

                            public BackgroundTaskContext CraeteScopedContext(string jobId) {
                                var scope = _serviceProvider.CreateScope();
                                var dirName = $"{_startTime:yyyyMMddHHmmss}_{jobId}";
                                var workingDirectory = Path.Combine(_directory, dirName);
                                return new BackgroundTaskContext(scope, _startTime, workingDirectory);
                            }
                            public BackgroundTaskContext<TParameter> CraeteScopedContext<TParameter>(string jobId, TParameter parameter) {
                                var scope = _serviceProvider.CreateScope();
                                var dirName = $"{_startTime:yyyyMMddHHmmss}_{jobId}";
                                var workingDirectory = Path.Combine(_directory, dirName);
                                return new BackgroundTaskContext<TParameter>(parameter, scope, _startTime, workingDirectory);
                            }
                        }

                        public class BackgroundTaskContext : IDisposable {
                            public BackgroundTaskContext(IServiceScope serviceScope, DateTime startTime, string workingDirectory) {
                                StartTime = startTime;
                                WorkingDirectory = workingDirectory;
                                _serviceScope = serviceScope;
                            }

                            private readonly IServiceScope _serviceScope;

                            public DateTime StartTime { get; }
                            public string WorkingDirectory { get; }

                            public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;
                            public ILogger Logger => ServiceProvider.GetRequiredService<ILogger>();
                            public {{appSrv.ClassName}} AppSrv => ServiceProvider.GetRequiredService<{{appSrv.ClassName}}>();
                            public {{dbContextFullName}} DbContext => ServiceProvider.GetRequiredService<{{dbContextFullName}}>();

                            void IDisposable.Dispose() {
                                _serviceScope.Dispose();
                            }
                        }
                        public class BackgroundTaskContext<TParameter> : BackgroundTaskContext {
                            public BackgroundTaskContext(TParameter parameter, IServiceScope serviceScope, DateTime startTime, string workingDirectory)
                                : base(serviceScope, startTime, workingDirectory) {
                                Parameter = parameter;
                            }
                            public TParameter Parameter { get; }
                        }
                    }
                    """;
            },
        };
    }
}
