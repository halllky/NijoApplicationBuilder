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
using Nijo.SchemaParsing;
using System.Xml.Linq;
using Nijo.CodeGenerating.Helpers;
using Nijo.Models.DataModelModules;
using Nijo.Models.QueryModelModules;
using Nijo.ImmutableSchema;

[assembly: InternalsVisibleTo("Nijo.IntegrationTest")]
[assembly: InternalsVisibleTo("Nijo.Ui")]

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

            // ---------------------------------------------------
            // ** 引数定義 **

            // プロジェクト相対パス
            var path = new Argument<string?>(
                name: "project path",
                getDefaultValue: () => string.Empty,
                description: "カレントディレクトリから操作対象のnijoプロジェクトへの相対パス");

            // ビルドスキップ
            var noBuild = new Option<bool>(
                ["-n", "--no-build"],
                description: "デバッグ開始時にコード自動生成をせず、アプリケーションの起動のみ行います。");

            // ---------------------------------------------------
            // ** コマンド **

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

            // スキーマダンプ
            var dump = new Command(
                name: "dump",
                description: "スキーマ定義とプロパティパスの情報をMarkdown形式で出力します。")
                { path };
            dump.SetHandler(Dump, path);
            rootCommand.AddCommand(dump);

            // スキーマ定義ルール
            var rule = new Command(
                name: "rule",
                description: "スキーマ定義ルールを説明するドキュメントをMarkdown形式で出力します。");
            rule.SetHandler(Rule);
            rootCommand.AddCommand(rule);

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

            if (!GeneratedProject.TryOpen(projectRoot, out var project, out var error)) {
                logger.LogError(error);
                return;
            }
            var rule = SchemaParseRule.Default();
            var parseContext = new SchemaParseContext(XDocument.Load(project.SchemaXmlPath), rule);

            project.ValidateSchema(parseContext, logger);
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

            if (!GeneratedProject.TryOpen(projectRoot, out var project, out var error)) {
                logger.LogError(error);
                return;
            }
            var rule = SchemaParseRule.Default();
            var parseContext = new SchemaParseContext(XDocument.Load(project.SchemaXmlPath), rule);

            project.GenerateCode(parseContext, logger);
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

            if (!GeneratedProject.TryOpen(projectRoot, out var project, out var error)) {
                logger.LogError(error);
                return;
            }

            var firstLaunch = true;
            while (true) {
                logger.LogInformation("-----------------------------------------------");
                logger.LogInformation("デバッグを開始します。キーボードのQで終了します。それ以外のキーでリビルドします。");

                var reactServerUrl = project.GetConfig().ReactDebuggingUrl;
                using var launcher = new Runtime.GeneratedProjectLauncher(project.WebapiProjectRoot, new Uri(reactServerUrl), project.ReactProjectRoot, logger);
                try {
                    if (!noBuild) {
                        var rule = SchemaParseRule.Default();
                        var parseContext = new SchemaParseContext(XDocument.Load(project.SchemaXmlPath), rule);
                        project.GenerateCode(parseContext, logger);
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

        /// <summary>
        /// スキーマ定義とプロパティパスの情報をMarkdown形式で出力します。
        /// </summary>
        /// <param name="path">対象フォルダまでの相対パス</param>
        private static void Dump(string? path) {
            var projectRoot = path == null
                ? Directory.GetCurrentDirectory()
                : Path.Combine(Directory.GetCurrentDirectory(), path);
            var logger = ILoggerExtension.CreateConsoleLogger();

            if (!GeneratedProject.TryOpen(projectRoot, out var project, out var error)) {
                logger.LogError(error);
                return;
            }

            var rule = SchemaParseRule.Default();
            var xDocument = XDocument.Load(project.SchemaXmlPath);
            var parseContext = new SchemaParseContext(xDocument, rule);

            // TryBuildSchemaメソッドを使用してApplicationSchemaのインスタンスを生成
            if (parseContext.TryBuildSchema(xDocument, out var appSchema, logger)) {
                // ApplicationSchemaクラスのGenerateMarkdownDumpメソッドを使用
                var markdownContent = appSchema.GenerateMarkdownDump();

                // 標準出力に出力
                Console.WriteLine(markdownContent);
            } else {
                logger.LogError("スキーマのビルドに失敗したため、ダンプを生成できませんでした。");
            }
        }

        /// <summary>
        /// スキーマ定義ルールを説明するドキュメントをMarkdown形式で出力します。
        /// </summary>
        private static void Rule() {
            var rule = SchemaParseRule.Default();
            var markdownContent = rule.RenderMarkdownDocument();

            // 標準出力に出力
            Console.WriteLine(markdownContent);
        }
    }
}
