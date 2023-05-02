using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder {
    public class Program {

        static async Task<int> Main(string[] args) {

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => {

                cancellationTokenSource.Cancel();

                // キャンセル時のリソース解放を適切に行うために既定の動作（アプリケーション終了）を殺す
                e.Cancel = true;
            };

            var xmlFilename = new Argument<string?>();
            var mvc =new Option<bool>("mvc");

            var gen = new Command(name: "gen", description: "ソースコードの自動生成を実行します。") { xmlFilename };
            var debug = new Command(name: "debug", description: "プロジェクトのデバッグを開始します。") { xmlFilename };
            var template = new Command(name: "template", description: "アプリケーション定義ファイルのテンプレートを表示します。");

            gen.SetHandler((xmlFilename, mvc) => Gen(xmlFilename, mvc, cancellationTokenSource.Token), xmlFilename, mvc);
            debug.SetHandler(xmlFilename => Debug(xmlFilename, cancellationTokenSource.Token), xmlFilename);
            template.SetHandler(() => Template(cancellationTokenSource.Token));

            var rootCommand = new RootCommand("HalApplicationBuilder");
            rootCommand.AddCommand(gen);
            rootCommand.AddCommand(debug);
            rootCommand.AddCommand(template);

            var parser = new CommandLineBuilder(rootCommand)
                .UseExceptionHandler((ex, _) => {
                    cancellationTokenSource.Cancel();
                    Console.Error.WriteLine(ex.ToString());
                })
                .Build();
            return await parser.InvokeAsync(args);
        }


        private static Config ReadConfig(
            string? xmlFilename,
            out string xmlContent,
            out string xmlDir,
            out string projectRoot) {

            if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");
            var xmlFullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), xmlFilename));
            xmlContent = File.ReadAllText(xmlFullPath);
            var config = Core.Config.FromXml(xmlContent);

            xmlDir = Path.GetDirectoryName(xmlFullPath) ?? throw new DirectoryNotFoundException();
            projectRoot = Path.Combine(xmlDir, config.OutProjectDir);

            return config;
        }

        private static void Gen(string? xmlFilename, bool mvc, CancellationToken cancellationToken) {
            var config = ReadConfig(xmlFilename, out var xmlContent, out var _, out var projectRoot);
            var generator = CodeGenerator.FromXml(xmlContent);
            if (mvc) {
                generator.GenerateAspNetCoreMvc(projectRoot, config, Console.Out, cancellationToken);
            } else {
                generator.GenerateReactAndWebApi(projectRoot, config, Console.Out, cancellationToken);
            }
        }

        private static void Debug(string? xmlFilename, CancellationToken cancellationToken) {
            if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");

            var config = ReadConfig(xmlFilename, out var _, out var xmlDir, out var projectRoot);

            using var dotnetRun = new DotnetEx.ExternalProcess.BackgroundExternalProcess {
                CancellationToken = cancellationToken,
                WorkingDirectory = projectRoot,
                Filename = "dotnet",
                Args = new[] { "run" },
            };

            void RebuildDotnet() {
                dotnetRun.Stop();
                var migrationId = Guid.NewGuid().ToString().Replace("-", "");
                var migrationProcess = new DotnetEx.ExternalProcess(projectRoot!, cancellationToken);
                migrationProcess.Start("dotnet", "ef", "migrations", "add", migrationId);
                migrationProcess.Start("dotnet", "ef", "database", "update");
                dotnetRun.Restart();
            }

            var npmRoot = Path.Combine(projectRoot, CodeGenerator.ReactAndWebApiGenerator.REACT_DIR);
            using var npmStart = new DotnetEx.ExternalProcess.BackgroundExternalProcess {
                CancellationToken = cancellationToken,
                WorkingDirectory = npmRoot,
                Filename = "npm",
                Args = new[] { "start" },
            };

            // watching xml
            using var watcher = new FileSystemWatcher(xmlDir);

            // ソース自動生成処理が1秒間に何度も走らないようにするための仕組み
            var INTERVAL = TimeSpan.FromSeconds(1);
            watcher.Filter = xmlFilename;
            DateTime? lastExecutionTime = null;
            Timer? timer = null;

            void OnChangeXml() {
                // ソースファイル再生成 & npm watch による自動更新
                Gen(xmlFilename, false, cancellationToken);

                // dotnetの更新
                RebuildDotnet();

                lastExecutionTime = DateTime.Now;
                timer?.Dispose();
                timer = null;
            }
            watcher.Changed += (_, _) => {
                if (lastExecutionTime == null || ((DateTime.Now - lastExecutionTime) >= INTERVAL)) {
                    OnChangeXml();
                } else {
                    timer ??= new Timer(_ => OnChangeXml(), null, Timeout.Infinite, Timeout.Infinite);
                    timer.Change(INTERVAL, TimeSpan.Zero);
                }
            };

            // start
            npmStart.Restart();
            RebuildDotnet(); 
            watcher.EnableRaisingEvents = true;

            while (!cancellationToken.IsCancellationRequested) {
                Thread.Sleep(100);
            }
        }

        private static void Template(CancellationToken cancellationToken) {
            var thisAssembly = Assembly.GetExecutingAssembly();
            var source = thisAssembly.GetManifestResourceStream("HalApplicationBuilder.Template.xml")!;
            using var sourceReader = new StreamReader(source);

            while (!sourceReader.EndOfStream) {
                Console.WriteLine(sourceReader.ReadLine());
            }
        }
    }
}
