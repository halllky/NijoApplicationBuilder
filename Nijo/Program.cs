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
using Nijo.Core;
using Nijo.Util.DotnetEx;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Build.Evaluation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nijo.Parts.WebServer;
using Nijo.Runtime;
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

            // DI
            var services = new ServiceCollection();
            GeneratedProject.ConfigureDefaultServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // 引数定義
            var verbose = new Option<bool>(
                name: "--verbose",
                description: "詳細なログを出力します。");

            var path = new Argument<string?>(
                name: "project path",
                getDefaultValue: () => string.Empty,
                description: "カレントディレクトリから操作対象のnijoプロジェクトへの相対パス");

            var applicationName = new Argument<string?>(
                name: "application name",
                description: "新規作成されるアプリケーションの名前");

            var keepTempIferror = new Option<bool>(
                name: "--keep-temp-if-error",
                description: "作成に失敗した場合、原因調査ができるようにするため一時フォルダを削除せず残します。");

            var mermaid = new Option<bool>(
                name: "--mermaid",
                description: "スキーマ定義をMermaid形式で表示します。");

            var noBuild = new Option<bool>(
                ["-n", "--no-build"],
                description: "デバッグ開始時にコード自動生成をせず、アプリケーションの起動のみ行います。");

            var port = new Option<int?>(
                ["-p", "--port"],
                description: "スキーマ定義編集アプリケーションが実行されるポートを明示的に指定します。");

            var noBrowser = new Option<bool>(
                ["-n", "--no-browser"],
                description: "スキーマ定義編集アプリケーションの開始時に自動的にブラウザを開くのを防ぎます。");

            var timeout = new Option<int?>(
                ["-t", "--timeout"],
                description: "TypeScriptやC#のビルドのタイムアウト時間。単位は秒。");

            // コマンド定義
            var create = new Command(name: "create", description: "新しいプロジェクトを作成します。") { verbose, applicationName, keepTempIferror };
            create.SetHandler((verbose, applicationName, keepTempIferror) => {
                if (string.IsNullOrEmpty(applicationName)) throw new ArgumentException($"Application name is required.");
                var logger = ILoggerExtension.CreateConsoleLogger(verbose);
                var projectRootDir = Path.Combine(Directory.GetCurrentDirectory(), applicationName);
                GeneratedProject.Create(
                    projectRootDir,
                    applicationName,
                    keepTempIferror,
                    serviceProvider,
                    cancellationTokenSource.Token,
                    logger);
            }, verbose, applicationName, keepTempIferror);
            rootCommand.AddCommand(create);

            var update = new Command(name: "update", description: "コード自動生成処理をかけなおします。") { verbose, path };
            update.SetHandler((verbose, path) => {
                var logger = ILoggerExtension.CreateConsoleLogger(verbose);
                GeneratedProject
                    .Open(path, serviceProvider, logger)
                    .CodeGenerator
                    .GenerateCode();
            }, verbose, path);
            rootCommand.AddCommand(update);

            var debug = new Command(name: "debug", description: "プロジェクトのデバッグを開始します。") { verbose, path, noBuild, timeout };
            debug.SetHandler((verbose, path, noBuild, timeout) => {
                var logger = ILoggerExtension.CreateConsoleLogger(verbose);
                var project = GeneratedProject.Open(path, serviceProvider, logger);
                var firstLaunch = true;
                while (true) {
                    logger.LogInformation("-----------------------------------------------");
                    logger.LogInformation("デバッグを開始します。キーボードのQで終了します。それ以外のキーでリビルドします。");

                    using var launcher = project.CreateLauncher();
                    try {
                        if (!noBuild) {
                            project.CodeGenerator.GenerateCode();
                        }

                        launcher.Launch();

                        var timeoutTimespan = timeout == null
                            ? (TimeSpan?)null
                            : TimeSpan.FromSeconds(timeout.Value);
                        launcher.WaitForReady(timeoutTimespan);

                        // 初回ビルド時はブラウザ立ち上げ
                        if (firstLaunch) {
                            try {
                                var npmUrl = project.ReactProject.GetDebuggingClientUrl();
                                var launchBrowser = new Process();
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                                    launchBrowser.StartInfo.FileName = "cmd";
                                    launchBrowser.StartInfo.Arguments = $"/c \"start {npmUrl}\"";
                                } else {
                                    launchBrowser.StartInfo.FileName = "open";
                                    launchBrowser.StartInfo.Arguments = npmUrl.ToString();
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
            }, verbose, path, noBuild, timeout);
            rootCommand.AddCommand(debug);

            var dump = new Command(
                name: "dump",
                description: "スキーマ定義から構築したスキーマ詳細を出力します。")
                { verbose, path, mermaid };
            dump.SetHandler((verbose, path, mermaid) => {
                var logger = ILoggerExtension.CreateConsoleLogger(verbose);
                var schema = GeneratedProject
                    .Open(path, serviceProvider, logger)
                    .BuildSchema();
                if (mermaid) {
                    Console.WriteLine(schema.ToMermaidText());
                } else {
                    Console.WriteLine(schema.DumpTsv());
                }
            }, verbose, path, mermaid);
            rootCommand.AddCommand(dump);

            var ui = new Command(
                name: "ui",
                description: $"スキーマ定義をGUIで編集します。")
                { path, port, noBrowser };
            ui.SetHandler(async (path, port, noBrowser) => {
                var project = GeneratedProject.Open(path, serviceProvider);
                var editor = new NijoUi(project);
                var app = editor.CreateApp();

                var url = $"https://localhost:{port ?? 5000}";

                // ブラウザを開く
                if (!noBrowser) {
                    Process.Start(new ProcessStartInfo {
                        FileName = url,
                        UseShellExecute = true,
                    });
                }

                // アプリケーション起動
                await app.RunAsync(url);

            }, path, port, noBrowser);
            rootCommand.AddCommand(ui);

            // *************************** ver.1.0.000 ***************************

            var generate = new Command(
                name: "generate-20250315",
                description: "ソースコードの自動生成を実行します。")
                { path };
            generate.SetHandler(path => {

                // *****************************
                // あらかたできるまではダミーXMLを使用
                if (!Directory.Exists("20250315")) Directory.CreateDirectory("20250315");
                Directory.SetCurrentDirectory("20250315");
                File.WriteAllText("nijo.xml", $$"""
                    <?xml version="1.0" encoding="utf-8" ?>
                    <NijoApplicationBuilder>
                      <参照先 is="data-model generate-default-query-model generate-batch-update-command:参照先一括更新">
                        <参照先集約ID is="word key" />
                        <参照先集約名 is="word name" />
                      </参照先>

                      <参照先一括更新 is="command-model">
                        <Parameter>
                          <Items is="children">
                            <参照先集約ID is="word key" />
                            <参照先集約名 is="word name" />
                          </Items>
                        </Parameter>
                        <ReturnType>
                        </ReturnType>
                      </参照先一括更新>

                      <参照元 is="data-model">
                        <参照元集約ID is="word key" />
                        <参照元集約名 is="word name" />
                        <参照 is="ref-to:参照先" />
                        <参照元集約名 is="My列挙体" />
                      </参照元>

                      <My列挙体 is="enum">
                        <値1 DisplayName="値（1）" key="1" />
                        <値2 key="2" />
                      </My列挙体>
                    </NijoApplicationBuilder>
                    """, new UTF8Encoding(false, false));
                // *****************************

                var projectRoot = path == null
                    ? Directory.GetCurrentDirectory()
                    : Path.Combine(Directory.GetCurrentDirectory(), path);
                var logger = ILoggerExtension.CreateConsoleLogger();

                var project = new Ver1.GeneratedProject(projectRoot, logger);
                project.GenerateCode();
            }, path);
            rootCommand.AddCommand(generate);

            return rootCommand;
        }
    }
}
