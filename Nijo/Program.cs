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
using Nijo.CodeGenerating;
using Nijo.Models;

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

            // デバッグ実行時、ブラウザを立ち上げない
            var noBrowser = new Option<bool>(
                ["-b", "--no-browser"],
                description: "デバッグ開始時にブラウザを立ち上げません。");

            // デバッグ実行キャンセルファイル
            var cancelFile = new Option<string?>(
                ["-c", "--cancel-file"],
                description: "デバッグ実行の終了のトリガーは、通常はユーザーからのキー入力ですが、これを指定したときはこのファイルが存在したら終了と判定します。");

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
                { path, noBuild, noBrowser, cancelFile };
            run.SetHandler(Run, path, noBuild, noBrowser, cancelFile);
            rootCommand.AddCommand(run);

            // スキーマダンプ
            var dump = new Command(
                name: "dump",
                description: "スキーマ定義とプロパティパスの情報をMarkdown形式で出力します。")
                { path };
            dump.SetHandler(Dump, path);
            rootCommand.AddCommand(dump);

            // スキーマ定義オプション
            var generateInternal = new Command(
                name: "generate-internal",
                description: "スキーマ定義で使用できるオプションを説明するドキュメントをMarkdown形式で出力します。");
            generateInternal.SetHandler(GenerateInternal);
            rootCommand.AddCommand(generateInternal);

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
        /// <param name="noBrowser">デバッグ開始時にブラウザを立ち上げない</param>
        /// <param name="cancelFile">デバッグ実行を終了するトリガー。このファイルが存在したら終了する。</param>
        private static async Task Run(string? path, bool noBuild, bool noBrowser, string? cancelFile) {
            var projectRoot = path == null
                ? Directory.GetCurrentDirectory()
                : Path.Combine(Directory.GetCurrentDirectory(), path);
            var cancelFileFullPath = cancelFile == null
                ? null
                : Path.GetFullPath(cancelFile);
            var logger = ILoggerExtension.CreateConsoleLogger();

            if (!GeneratedProject.TryOpen(projectRoot, out var project, out var error)) {
                logger.LogError(error);
                return;
            }

            var firstLaunch = true;
            while (true) {
                logger.LogInformation("-----------------------------------------------");
                if (cancelFileFullPath == null) {
                    logger.LogInformation("デバッグを開始します。キーボードのQで終了します。それ以外のキーでリビルドします。");
                } else {
                    logger.LogInformation("デバッグを開始します。右記パスにファイルが存在したら終了します: {cancelFile}", cancelFileFullPath);
                }

                var config = project.GetConfig();
                using var launcher = new Runtime.GeneratedProjectLauncher(
                    project.WebapiProjectRoot,
                    project.ReactProjectRoot,
                    new Uri(config.DotnetDebuggingUrl),
                    new Uri(config.ReactDebuggingUrl),
                    logger);
                try {
                    if (!noBuild) {
                        var rule = SchemaParseRule.Default();
                        var parseContext = new SchemaParseContext(XDocument.Load(project.SchemaXmlPath), rule);
                        project.GenerateCode(parseContext, logger);
                    }

                    launcher.Launch();
                    launcher.WaitForReady();

                    // 初回ビルド時はブラウザ立ち上げ
                    if (firstLaunch && !noBrowser) {
                        try {
                            var launchBrowser = new Process();
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                                launchBrowser.StartInfo.FileName = "cmd";
                                launchBrowser.StartInfo.Arguments = $"/c \"start {config.ReactDebuggingUrl}\"";
                            } else {
                                launchBrowser.StartInfo.FileName = "open";
                                launchBrowser.StartInfo.Arguments = config.ReactDebuggingUrl;
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

                // 待機。breakで終了。continueでリビルド
                if (cancelFileFullPath == null) {
                    // キー入力待機
                    var input = Console.ReadKey(true);
                    if (input.Key == ConsoleKey.Q) break;

                } else {
                    // キャンセルファイル監視
                    while (!File.Exists(cancelFileFullPath)) {
                        await Task.Delay(500);
                    }
                    break;
                }
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
        /// Nijoプロジェクト内部にファイルを生成する
        /// </summary>
        private static void GenerateInternal() {
            var logger = ILoggerExtension.CreateConsoleLogger();
            var rule = SchemaParseRule.Default();

            // 各モデルでどういったオプションを使用できるかを記載
            var modelsDir = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, // net9.0
                "..", // Debug
                "..", // bin
                "..", // Nijo
                "Models"));
            void RenderOptionsMd(string filename, IModel model) {
                var fullpath = Path.Combine(modelsDir, filename);
                var availableOptions = rule.GetAvailableOptionsFor(model);

                File.WriteAllText(fullpath, $$"""
                    # {{model.GetType().Name}}に指定することができるオプション
                    {{availableOptions.SelectTextTemplate(opt => $$"""

                    ## `{{opt.AttributeName}}` （{{opt.DisplayName}}）
                    {{opt.HelpText}}
                    """)}}
                    """.Replace(SKIP_MARKER, string.Empty), new UTF8Encoding(false, false));

                logger.LogInformation("オプション属性ドキュメントを生成しました: {0}", fullpath);
            }

            RenderOptionsMd("DataModel.Options.md", new DataModel());
            RenderOptionsMd("QueryModel.Options.md", new QueryModel());
            RenderOptionsMd("CommandModel.Options.md", new CommandModel());
        }
    }
}
