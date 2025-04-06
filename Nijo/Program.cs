using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nijo.Util.DotnetEx;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Policy;

[assembly: InternalsVisibleTo("Nijo.IntegrationTest")]

namespace Nijo {
    public class Program {

        static async Task<int> Main(string[] args) {
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => {
                cancellationTokenSource.Cancel();

                // キャンセル時のリソース解放を適切に行うために既定の動作（アプリケーション終了）を殺す
                e.Cancel = true;
            };

            var rootCommand = DefineCommand(cancellationTokenSource);

            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()

                // 例外処理
                .UseExceptionHandler((ex, _) => {
                    if (ex is OperationCanceledException) {
                        Console.Error.WriteLine("キャンセルされました。");
                    } else {
                        cancellationTokenSource.Cancel();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(ex.ToString());
                        Console.ResetColor();
                    }
                })
                .Build();

            return await parser.InvokeAsync(args);
        }

        private static RootCommand DefineCommand(CancellationTokenSource cancellationTokenSource) {
            var rootCommand = new RootCommand("nijo");

            // 引数定義
            var path = new Argument<string?>(
                name: "project path",
                getDefaultValue: () => string.Empty,
                description: "カレントディレクトリから操作対象のnijoプロジェクトへの相対パス");

            var noBuild = new Option<bool>(
                ["-n", "--no-build"],
                description: "デバッグ開始時にコード自動生成をせず、アプリケーションの起動のみ行います。");

            // 検証
            var validate = new Command(
                name: "validate",
                description: "スキーマ定義の検証を行ないます。")
                { path };
            validate.SetHandler(Validate, path);
            rootCommand.AddCommand(validate);

            // コード自動生成
            var generate = new Command(
                name: "generate",
                description: "ソースコードの自動生成を実行します。")
                { path };
            generate.SetHandler(Generate, path);
            rootCommand.AddCommand(generate);

            // デバッグ実行開始
            var run = new Command(
                name: "run",
                description: "プロジェクトのデバッグを開始します。")
                { path, noBuild };
            run.SetHandler(Run, path, noBuild);
            rootCommand.AddCommand(run);

            return rootCommand;
        }


        /// <summary>
        /// スキーマ定義の検証を行ないます。
        /// </summary>
        /// <param name="path">対象フォルダまでの相対パス</param>
        private static void Validate(string? path) {
            var projectRoot = path == null
                ? Directory.GetCurrentDirectory()
                : Path.Combine(Directory.GetCurrentDirectory(), path);
            var logger = ILoggerExtension.CreateConsoleLogger();
            var project = new Ver1.GeneratedProject(projectRoot, logger);

            project.ValidateSchema();
        }


        /// <summary>
        /// ソースコードの自動生成を実行します。
        /// </summary>
        /// <param name="path">対象フォルダまでの相対パス</param>
        private static void Generate(string? path) {
            var projectRoot = path == null
                ? Directory.GetCurrentDirectory()
                : Path.Combine(Directory.GetCurrentDirectory(), path);
            var logger = ILoggerExtension.CreateConsoleLogger();
            var project = new Ver1.GeneratedProject(projectRoot, logger);

            project.GenerateCode();
        }


        /// <summary>
        /// 対象プロジェクトのデバッグ実行を開始します。
        /// </summary>
        /// <param name="path">対象フォルダまでの相対パス</param>
        /// <param name="noBuild">ソースコードの自動生成をスキップする場合はtrue</param>
        private static void Run(string? path, bool noBuild) {
            var projectRoot = path == null
                ? Directory.GetCurrentDirectory()
                : Path.Combine(Directory.GetCurrentDirectory(), path);
            var logger = ILoggerExtension.CreateConsoleLogger();
            var project = new Ver1.GeneratedProject(projectRoot, logger);

            var firstLaunch = true;
            while (true) {
                logger.LogInformation("-----------------------------------------------");
                logger.LogInformation("デバッグを開始します。キーボードのQで終了します。それ以外のキーでリビルドします。");

                var reactServerUrl = project.GetConfig().ReactDebuggingUrl;
                using var launcher = new Runtime.GeneratedProjectLauncher(project.WebapiProjectRoot, new Uri(reactServerUrl), project.ReactProjectRoot, logger);
                try {
                    if (!noBuild) {
                        project.GenerateCode();
                    }

                    launcher.Launch();
                    launcher.WaitForReady();

                    // 初回ビルド時はブラウザ立ち上げ
                    if (firstLaunch) {
                        try {
                            var launchBrowser = new Process();
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                                launchBrowser.StartInfo.FileName = "cmd";
                                launchBrowser.StartInfo.Arguments = $"/c \"start {reactServerUrl}\"";
                            } else {
                                launchBrowser.StartInfo.FileName = "open";
                                launchBrowser.StartInfo.Arguments = reactServerUrl;
                            }
                            launchBrowser.Start();
                            launchBrowser.WaitForExit();
                        } catch (Exception ex) {
                            logger.LogError("Fail to launch browser: {msg}", ex.Message);
                        }
                        firstLaunch = false;
                    }
                } catch (Exception ex) {
                    logger.LogError("{msg}", ex.ToString());
                }

                // キー入力待機
                var input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.Q) break;
            }
        }
    }
}
