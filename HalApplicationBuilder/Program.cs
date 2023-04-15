using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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

            var gen = new Command(name: "gen", description: "ソースコードの自動生成を実行します。") { xmlFilename };
            var debug = new Command(name: "debug", description: "プロジェクトのデバッグを開始します。") { xmlFilename };
            var template = new Command(name: "template", description: "アプリケーション定義ファイルのテンプレートを表示します。");

            gen.SetHandler(xmlFilename => Gen(xmlFilename, cancellationTokenSource.Token), xmlFilename);
            debug.SetHandler(xmlFilename => Debug(xmlFilename, cancellationTokenSource.Token), xmlFilename);
            template.SetHandler(() => Template(cancellationTokenSource.Token));

            var rootCommand = new RootCommand("HalApplicationBuilder");
            rootCommand.AddCommand(gen);
            rootCommand.AddCommand(debug);
            rootCommand.AddCommand(template);
            return await rootCommand.InvokeAsync(args);
        }


        private static void Gen(string? xmlFilename, CancellationToken cancellationToken) {
            if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");
            var xmlFullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), xmlFilename));
            var xmlContent = File.ReadAllText(xmlFullpath);
            var config = Core.Config.FromXml(xmlContent);
            CodeGenerator
                .FromXml(xmlContent)
                .GenerateCode(config, Console.Out, cancellationToken);
        }

        private static void Debug(string? xmlFilename, CancellationToken cancellationToken) {
            if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");
            var xmlFullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), xmlFilename));
            var xmlContent = File.ReadAllText(xmlFullpath);
            var config = Core.Config.FromXml(xmlContent);
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), config.OutProjectDir);

            var process = new DotnetEx.ExternalProcess(workingDirectory, cancellationToken);
            var task = process.StartAsync("dotnet", "watch", "run");

            using var watcher = new FileSystemWatcher(Path.GetDirectoryName(xmlFullpath)!, xmlFilename);
            watcher.Changed += (sender, e) => {
                Gen(xmlFilename, cancellationToken);
            };

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
