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
            var mvc = new Option<bool>("--mvc");
            var verbose = new Option<bool>("--verbose");

            var gen = new Command(name: "gen", description: "ソースコードの自動生成を実行します。") { xmlFilename, verbose };
            var debug = new Command(name: "debug", description: "プロジェクトのデバッグを開始します。") { xmlFilename, verbose };
            var template = new Command(name: "template", description: "アプリケーション定義ファイルのテンプレートを表示します。");

            gen.SetHandler((xmlFilename, mvc, verbose) => Gen(xmlFilename, mvc, verbose, cancellationTokenSource.Token), xmlFilename, mvc, verbose);
            debug.SetHandler((xmlFilename, verbose) => Debug(xmlFilename, verbose, cancellationTokenSource.Token), xmlFilename, verbose);
            template.SetHandler(() => Template(cancellationTokenSource.Token));

            var rootCommand = new RootCommand("HalApplicationBuilder");
            rootCommand.AddCommand(gen);
            rootCommand.AddCommand(debug);
            rootCommand.AddCommand(template);

            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler((ex, _) => {
                    cancellationTokenSource.Cancel();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(ex.ToString());
                    Console.ResetColor();
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
            using (var stream = DotnetEx.IO.OpenFileWithRetry(xmlFullPath))
            using (var reader = new StreamReader(stream)) {
                xmlContent = reader.ReadToEnd();
            }
            var config = Core.Config.FromXml(xmlContent);

            xmlDir = Path.GetDirectoryName(xmlFullPath) ?? throw new DirectoryNotFoundException();
            projectRoot = Path.Combine(xmlDir, config.OutProjectDir);

            return config;
        }

        private static void Gen(string? xmlFilename, bool mvc, bool verbose, CancellationToken cancellationToken) {
            var config = ReadConfig(xmlFilename, out var xmlContent, out var _, out var projectRoot);
            var generator = CodeGenerator.FromXml(xmlContent);
            if (mvc) {
                generator.GenerateAspNetCoreMvc(projectRoot, config, verbose, Console.Out, cancellationToken);
            } else {
                generator.GenerateReactAndWebApi(projectRoot, config, verbose, Console.Out, cancellationToken);
            }
        }

        private static void Debug(string? xmlFilename, bool verbose, CancellationToken cancellationToken) {
            if (xmlFilename == null) throw new InvalidOperationException($"対象XMLを指定してください。");

            var config = ReadConfig(xmlFilename, out var _, out var xmlDir, out var projectRoot);

            // migration用設定
            var migrationList = new DotnetEx.Cmd {
                WorkingDirectory = projectRoot!,
                CancellationToken = cancellationToken,
                Verbose = verbose,
            };
            var previousMigrationId = migrationList
                .ReadOutputs("dotnet", "ef", "migrations", "list")
                .LastOrDefault();
            var nextMigrationId = Guid
                .NewGuid()
                .ToString()
                .Replace("-", "");
            var migratedInThisProcess = false;

            // 以下の2種類のキャンセルがあるので統合する
            // - ユーザーの操作による halapp debug 全体のキャンセル
            // - 集約定義ファイル更新によるビルドのキャンセル
            CancellationTokenSource? rebuildCancellation = null;
            CancellationTokenSource? linkedTokenSource = null;

            // バックグラウンド処理の宣言
            DotnetEx.Cmd.Background? dotnetRun = null;
            DotnetEx.Cmd.Background? npmStart = null;

            // ファイル変更監視用オブジェクト
            FileSystemWatcher? watcher = null;

            try {
                var changed = false;

                // halapp debug 中ずっと同じインスタンスが使われるものを初期化する
                watcher = new FileSystemWatcher(xmlDir);
                watcher.Filter = xmlFilename;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += (_, _) => {
                    changed = true;
                    rebuildCancellation?.Cancel();
                };

                npmStart = new DotnetEx.Cmd.Background {
                    WorkingDirectory = Path.Combine(projectRoot, CodeGenerator.ReactAndWebApiGenerator.REACT_DIR),
                    Filename = "npm",
                    Args = new[] { "start" },
                    CancellationToken = cancellationToken,
                    Verbose = verbose,
                };

                // 監視開始
                watcher.EnableRaisingEvents = true;
                npmStart.Restart();

                // リビルドの度に実行される処理
                while (true) {
                    dotnetRun?.Dispose();
                    rebuildCancellation?.Dispose();
                    linkedTokenSource?.Dispose();

                    rebuildCancellation = new CancellationTokenSource();
                    linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        rebuildCancellation.Token);

                    try {
                        // ソースファイル再生成 & npm watch による自動更新
                        Gen(xmlFilename, false, verbose, linkedTokenSource.Token);

                        linkedTokenSource.Token.ThrowIfCancellationRequested();

                        // DB定義の更新
                        var migration = new DotnetEx.Cmd {
                            WorkingDirectory = projectRoot!,
                            CancellationToken = linkedTokenSource.Token,
                            Verbose = verbose,
                        };
                        migration.Exec("dotnet", "build");

                        // 集約定義を書き換えるたびにマイグレーションが積み重なっていってしまうため、
                        // 1回のhalapp debugで作成されるマイグレーションは1つまでとする
                        if (migratedInThisProcess && !string.IsNullOrWhiteSpace(previousMigrationId)) {
                            Console.WriteLine($"DB定義を右記地点に巻き戻します: {previousMigrationId}");
                            migration.Exec("dotnet", "ef", "database", "update", previousMigrationId, "--no-build");
                            migration.Exec("dotnet", "ef", "migrations", "remove", "--no-build");
                            migration.Exec("dotnet", "build");

                            linkedTokenSource.Token.ThrowIfCancellationRequested();
                        }

                        migration.Exec("dotnet", "ef", "migrations", "add", nextMigrationId, "--no-build");
                        migration.Exec("dotnet", "build");
                        migration.Exec("dotnet", "ef", "database", "update", nextMigrationId, "--no-build");

                        linkedTokenSource.Token.ThrowIfCancellationRequested();

                    } catch (OperationCanceledException ex) when (ex.CancellationToken == rebuildCancellation?.Token) {
                        // 実行中のビルドを中断してもう一度最初から
                        break;
                    } catch (Exception ex) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(ex.ToString());
                        Console.ResetColor();
                    }

                    changed = false;
                    migratedInThisProcess = true;

                    // ビルドが完了したので dotnet run を再開
                    dotnetRun = new DotnetEx.Cmd.Background {
                        WorkingDirectory = projectRoot,
                        Filename = "dotnet",
                        Args = new[] { "run", "--no-build" },
                        CancellationToken = linkedTokenSource.Token,
                        Verbose = verbose,
                    };
                    dotnetRun.Restart();

                    // 次の更新まで待機
                    while (changed == false) {
                        Thread.Sleep(100);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

            } catch (OperationCanceledException) {
                // 何もしない

            } finally {
                rebuildCancellation?.Dispose();
                linkedTokenSource?.Dispose();
                dotnetRun?.Dispose();
                npmStart?.Dispose();
                watcher?.Dispose();
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
