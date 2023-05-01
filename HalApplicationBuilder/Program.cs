using System;
using System.Collections.Generic;
using System.CommandLine;
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
            return await rootCommand.InvokeAsync(args);
        }


        private static Config ReadConfig(string? xmlFilename) {
            if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");
            var xmlFullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), xmlFilename));
            var xmlContent = File.ReadAllText(xmlFullpath);
            return Core.Config.FromXml(xmlContent);
        }

        private static void Gen(string? xmlFilename, bool mvc, CancellationToken cancellationToken) {
            if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");
            var xmlFullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), xmlFilename));
            var xmlContent = File.ReadAllText(xmlFullpath);
            var config = Core.Config.FromXml(xmlContent);
            var generator = CodeGenerator.FromXml(xmlContent);
            if (mvc) {
                generator.GenerateAspNetCoreMvc(config, Console.Out, cancellationToken);
            } else {
                generator.GenerateReactAndWebApi(config, Console.Out, cancellationToken);
            }
        }

        private static void Build(string? xmlFilename, CancellationToken cancellationToken) {
            var config = ReadConfig(xmlFilename);
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), config.OutProjectDir);

            var process = new DotnetEx.ExternalProcess(workingDirectory, cancellationToken);
            process.Start("dotnet", "build");
        }

        private static void Debug(string? xmlFilename, CancellationToken cancellationToken) {
            var config = ReadConfig(xmlFilename);
            var projectDir = Path.Combine(Directory.GetCurrentDirectory(), config.OutProjectDir);
            var npmRoot = Path.Combine(projectDir, CodeGenerator.ReactAndWebApiGenerator.REACT_DIR);

            // dotnet run & npm start
            CancellationTokenSource? runTokenSource = null;
            void StartRunning() {
                if (runTokenSource != null) {
                    runTokenSource.Cancel(true);
                }
                runTokenSource = new CancellationTokenSource();
                var dotnetWatch = new DotnetEx.ExternalProcess(projectDir, runTokenSource.Token);
                var npmStart = new DotnetEx.ExternalProcess(npmRoot, runTokenSource.Token);
                var task1 = dotnetWatch.StartAsync("dotnet", "watch", "run");
                var task2 = npmStart.StartAsync("npm", "start");
            }
            void StopRunning() {
                if (runTokenSource != null) {
                    runTokenSource.Cancel(true);
                }
            }

            // build task
            void Update() {
                Gen(xmlFilename, false, cancellationToken);
                Build(xmlFilename, cancellationToken);
                AddMigration(xmlFilename, true, cancellationToken);
            }

            // watching xml
            var xmlFullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), xmlFilename!));
            using var watcher = new FileSystemWatcher(Path.GetDirectoryName(xmlFullpath)!, xmlFilename!);
            watcher.Changed += (sender, e) => {
                StopRunning();
                Update();
                StartRunning();
            };

            // start
            Update();
            StartRunning();
            watcher.EnableRaisingEvents = true;

            while (!cancellationToken.IsCancellationRequested) {
                Thread.Sleep(100);
            }
        }

        private static void AddMigration(string? xmlFilename, bool noBuild, CancellationToken cancellationToken) {
            var config = ReadConfig(xmlFilename);
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), config.OutProjectDir);

            var process = new DotnetEx.ExternalProcess(workingDirectory, cancellationToken);
            var migrationId = Guid.NewGuid().ToString();
            process.Start("dotnet", "ef", "migrations", "add", migrationId, noBuild ? "--no-build" : "");
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
