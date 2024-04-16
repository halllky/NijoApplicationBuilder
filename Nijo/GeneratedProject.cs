using Nijo.Core;
using Nijo.Util.DotnetEx;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Build.Evaluation;
using Nijo.Parts.WebServer;
using Microsoft.Extensions.DependencyInjection;

namespace Nijo {
    public sealed partial class GeneratedProject {

        public static void ConfigureDefaultServices(IServiceCollection serviceDescriptors) {
            serviceDescriptors.AddTransient<IPackageInstaller, PackageInstaller>();
        }

        /// <summary>
        /// 新しいプロジェクトを作成します。
        /// </summary>
        /// <param name="applicationName">アプリケーション名</param>
        /// <param name="verbose">ログの詳細出力を行うかどうか</param>
        /// <returns>作成されたプロジェクトを表すオブジェクト</returns>
        public static GeneratedProject Create(
            string projectRootDir,
            string? applicationName,
            bool keepTempIferror,
            IServiceProvider serviceProvider,
            CancellationToken? cancellationToken = null,
            ILogger? log = null,
            bool initGitRepository = true) {

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new InvalidOperationException($"Please specify name of new application.");

            if (Path.GetInvalidFileNameChars().Any(applicationName.Contains))
                throw new InvalidOperationException($"'{applicationName}' contains invalid characters for a file name.");

            if (Directory.Exists(projectRootDir))
                throw new InvalidOperationException($"'{projectRootDir}' is already exists.");

            var tempDir = keepTempIferror
                // 最終的な出力先ディレクトリに直に生成する場合はこちら
                ? projectRootDir
                // まず一時ディレクトリに生成して、最後まで正常終了した場合だけ最終的な出力先ディレクトリにコピーする場合はこちら
                : Path.Combine(Path.GetTempPath(), "nijo.temp." + Path.GetRandomFileName());

            var error = false;
            try {
                var tempProject = new GeneratedProject(tempDir, serviceProvider, log);

                using (var _ = log?.BeginScope("プロジェクトディレクトリの作成")) {
                    log?.LogInformation("ProjectRoot: {0}", tempProject.ProjectRoot);

                    if (Directory.Exists(tempProject.ProjectRoot) || File.Exists(tempProject.ProjectRoot)) {
                        throw new InvalidOperationException($"Directory is already exists: {tempProject.ProjectRoot}");
                    }
                    Directory.CreateDirectory(tempProject.ProjectRoot);
                }

                var config = new Config {
                    ApplicationName = applicationName,
                    DbContextName = "MyDbContext",
                };

                using (var _ = log?.BeginScope("nijo.xmlの作成")) {
                    var xmlPath = tempProject.SchemaXml.GetPath();
                    log?.LogInformation("XML Path: {0}", xmlPath);

                    // TODO: PJ間の依存関係の向きがぐちゃぐちゃなので直す
                    var defaultXml = Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream("Nijo.NewProjectDefaultXml.012_スカラメンバー網羅.xml");
                    using var sr = new StreamReader(defaultXml!, new UTF8Encoding(false));
                    var xmlContent = XDocument.Parse(sr.ReadToEnd());
                    xmlContent.Root!.Name = applicationName;

                    using var sw = new StreamWriter(xmlPath, append: false, encoding: new UTF8Encoding(false));
                    sw.NewLine = "\n";
                    sw.WriteLine(xmlContent.ToString());
                }

                using (var _ = log?.BeginScope(".gitignoreの作成")) {
                    var gitignore = Path.Combine(tempProject.ProjectRoot, ".gitignore");
                    log?.LogInformation(".gitignore path: {0}", gitignore);

                    File.WriteAllText(gitignore, $$"""
                        /*.sqlite3
                        """);
                }

                using (var _ = log?.BeginScope("React & ASP.NET テンプレートのコピー")) {
                    var resources = new Parts.EmbeddedResource.Collection(
                        Assembly.GetExecutingAssembly());

                    foreach (var resource in resources.Enumerate("REACT_AND_WEBAPI", "react")) {
                        var destination = Path.Combine(
                            tempProject.WebClientProjectRoot,
                            Path.GetRelativePath("REACT_AND_WEBAPI/react", resource.RelativePath));
                        log?.LogInformation("From template : {0}", resource.RelativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                        using var reader = resource.GetStreamReader();
                        using var writer = new StreamWriter(destination);
                        while (!reader.EndOfStream) {
                            writer.WriteLine(reader.ReadLine());
                        }
                    }
                    foreach (var resource in resources.Enumerate("REACT_AND_WEBAPI", "webapi")) {
                        var destination = Path.Combine(
                            tempProject.WebApiProjectRoot,
                            Path.GetRelativePath("REACT_AND_WEBAPI/webapi", resource.RelativePath));
                        log?.LogInformation("From template : {0}", resource.RelativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                        using var reader = resource.GetStreamReader();
                        using var writer = new StreamWriter(destination);
                        while (!reader.EndOfStream) {
                            writer.WriteLine(reader.ReadLine());
                        }
                    }
                }

                using (var _ = log?.BeginScope("reactのデバッグ用コードを除去")) {
                    var appTsx = Path.Combine(tempProject.WebClientProjectRoot, "src", "App.tsx");
                    File.WriteAllText(appTsx, $$"""
                        import { DefaultNijoApp } from './__autoGenerated'

                        function App() {

                          return (
                            <DefaultNijoApp />
                          )
                        }

                        export default App
                        """, new UTF8Encoding(false));
                }

                using (var _ = log?.BeginScope("テンプレート中に登場するプロジェクト名を作成されるプロジェクト名に置換")) {
                    var programCs = Path.Combine(tempProject.WebApiProjectRoot, "Program.cs");
                    var beforeReplace = File.ReadAllText(programCs);
                    var afterReplace = beforeReplace.Replace("REACT_AND_WEBAPI", config.RootNamespace);
                    File.WriteAllText(programCs, afterReplace);

                    var beforeCsproj = Path.Combine(tempProject.WebApiProjectRoot, "REACT_AND_WEBAPI.csproj");
                    var afterCsproj = Path.Combine(tempProject.WebApiProjectRoot, $"{config.ApplicationName}.csproj");
                    File.Move(beforeCsproj, afterCsproj);
                }

                using (var _ = log?.BeginScope("自動生成されないコードの初期化")) {
                    var overridedAppSrv = new ApplicationService();
                    var overrideAppSrv = Path.Combine(tempProject.WebApiProjectRoot, overridedAppSrv.ConcreteClassFileName);
                    File.WriteAllText(overrideAppSrv, overridedAppSrv.RenderConcreteClass(config));
                }

                using (var _ = log?.BeginScope("自動生成されるコードの初期化")) {
                    tempProject.CodeGenerator.UpdateAutoGeneratedCode();
                }

                using (var _ = log?.BeginScope("外部パッケージのインストール")) {
                    tempProject.ServiceProvider
                        .GetRequiredService<IPackageInstaller>()
                        .InstallDependencies(tempProject, CancellationToken.None)
                        .Wait();
                }

                if (initGitRepository) {
                    using (var _ = log?.BeginScope("git初期化")) {
                        Process? git = null;
                        try {
                            log?.LogWarning("gitリポジトリを作成します。");

                            git = new Process();
                            git.StartInfo.WorkingDirectory = tempProject.ProjectRoot;
                            git.StartInfo.FileName = "git";
                            git.StartInfo.Arguments = "init";
                            git.Start();
                            git.WaitForExit();

                            git = new Process();
                            git.StartInfo.WorkingDirectory = tempProject.ProjectRoot;
                            git.StartInfo.FileName = "git";
                            git.StartInfo.Arguments = "add .";
                            git.Start();
                            git.WaitForExit();

                            git = new Process();
                            git.StartInfo.WorkingDirectory = tempProject.ProjectRoot;
                            git.StartInfo.FileName = "git";
                            git.StartInfo.Arguments = "commit -m \"init\"";
                            git.Start();
                            git.WaitForExit();

                        } catch (Exception ex) {
                            log?.LogWarning("gitリポジトリの作成に失敗しました: {msg}", ex.Message);

                        } finally {
                            git?.EnsureKill();
                        }
                    }
                }

                // ここまでの処理がすべて成功したら一時ディレクトリを本来のディレクトリ名に変更
                if (tempDir != projectRootDir) {
                    if (Directory.Exists(projectRootDir)) throw new InvalidOperationException($"プロジェクトディレクトリを {projectRootDir} に移動できません。");
                    Directory.Move(tempDir, projectRootDir);
                }

                log?.LogInformation("プロジェクト作成完了");

                return new GeneratedProject(projectRootDir, serviceProvider, log);

            } catch {
                error = true;
                throw;

            } finally {
                if (tempDir != projectRootDir && Directory.Exists(tempDir) && (keepTempIferror == false || error == false)) {
                    try {
                        Directory.Delete(tempDir, true);
                    } catch (Exception ex) {
                        log?.LogError(ex, new Exception("Failure to delete temp directory.", ex).ToString());
                    }
                }
            }
        }
        /// <summary>
        /// 既存のプロジェクトを開きます。
        /// </summary>
        /// <param name="path">プロジェクトルートディレクトリの絶対パス</param>
        /// <returns>作成されたプロジェクトを表すオブジェクト</returns>
        public static GeneratedProject Open(
            string? path,
            IServiceProvider serviceProvider,
            ILogger? log = null) {

            string normalizedPath;

            if (string.IsNullOrWhiteSpace(path))
                normalizedPath = Directory.GetCurrentDirectory();
            else if (Path.IsPathRooted(path))
                normalizedPath = path;
            else
                normalizedPath = Path.Combine(Directory.GetCurrentDirectory(), path);

            if (!Directory.Exists(normalizedPath))
                throw new InvalidOperationException($"Directory is not exist: {normalizedPath}");
            return new GeneratedProject(normalizedPath, serviceProvider, log);
        }

        private GeneratedProject(string projetctRoot, IServiceProvider serviceProvider, ILogger? log) {
            if (string.IsNullOrWhiteSpace(projetctRoot))
                throw new ArgumentException($"'{nameof(projetctRoot)}' is required.");

            ProjectRoot = Path.GetFullPath(projetctRoot);
            _log = log ?? ILoggerExtension.CreateConsoleLogger();

            CodeGenerator = new NijoCodeGenerator(this, log);
            SchemaXml = new AppSchemaXml(ProjectRoot);

            ServiceProvider = serviceProvider;
        }

        private readonly ILogger _log;

        public string ProjectRoot { get; }
        public string WebClientProjectRoot => Path.Combine(ProjectRoot, "react");
        public string WebApiProjectRoot => Path.Combine(ProjectRoot, "webapi");

        internal IServiceProvider ServiceProvider { get; }

        public NijoCodeGenerator CodeGenerator { get; }
        public AppSchemaXml SchemaXml { get; }

        public Config ReadConfig() {
            var xDocument = SchemaXml.Load();
            return Config.FromXml(xDocument);
        }
        /// <summary>
        /// アプリケーションスキーマを生成します。
        /// </summary>
        /// <exception cref="InvalidOperationException">アプリケーションスキーマが不正な場合</exception>
        internal AppSchema BuildSchema() {
            var builder = new AppSchemaBuilder();
            if (!SchemaXml.ConfigureBuilder(builder, out var errors)) {
                throw new InvalidOperationException(errors.Join(Environment.NewLine));
            }

            // Nijo標準機能
            foreach (var feature in CodeGenerator.GetFeatures()) {
                feature.BuildSchema(builder);
            }

            if (!builder.TryBuild(out var appSchema, out var errors1)) {
                throw new InvalidOperationException(errors1.Join(Environment.NewLine));
            }

            return appSchema;
        }
        internal bool ValidateSchema(out IEnumerable<string> errors) {
            var errorList = new List<string>();
            errors = errorList;

            var builder = new AppSchemaBuilder();
            var builderOk = SchemaXml.ConfigureBuilder(builder, out var errors1);
            errorList.AddRange(errors1);

            if (builderOk) {
                var schemaOk = builder.TryBuild(out var _, out var errors2);
                errorList.AddRange(errors2);
                return schemaOk;
            } else {
                return false;
            }
        }

        /// <summary>
        /// プロジェクトの検査を行います。結果は戻り値ではなくログに出力されます。
        /// </summary>
        public async Task<bool> CompilerCheck(CancellationToken cancellationToken) {
            var builder = new Runtime.GeneratedProjectBuilder(this, _log);
            return await builder.StaticCheck(cancellationToken);
        }

        public Runtime.GeneratedProjectLauncher CreateLauncher() {
            return new Runtime.GeneratedProjectLauncher(this, _log);
        }

        /// <summary>
        /// デバッグ時に起動されるアプリケーションのURLを返します。
        /// </summary>
        public Uri GetDebugUrl() {
            return new Uri(GetDebuggingServerUrl().Split(';')[0]);
        }
        /// <summary>
        /// デバッグ時に起動されるSwagger UIのURLを返します。
        /// </summary>
        /// <returns></returns>
        public Uri GetSwaggerUrl() {
            return new Uri(new Uri(GetDebuggingServerUrl().Split(';')[0]), "swagger");
        }
        /// <summary>
        /// launchSettings.jsonのhttpsプロファイルのapplicationUrlセクションの値を読み取ります。
        /// </summary>
        private string GetDebuggingServerUrl() {
            var properties = Path.Combine(WebApiProjectRoot, "Properties");
            if (!Directory.Exists(properties)) throw new DirectoryNotFoundException(properties);
            var launchSettings = Path.Combine(properties, "launchSettings.json");
            if (!File.Exists(launchSettings)) throw new FileNotFoundException(launchSettings);

            var json = File.ReadAllText(launchSettings);
            var obj = JsonSerializer.Deserialize<JsonObject>(json);
            if (obj == null)
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (!obj.TryGetPropertyValue("profiles", out var profiles))
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (profiles == null)
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (!profiles.AsObject().TryGetPropertyValue("https", out var https))
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (https == null)
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (!https.AsObject().TryGetPropertyValue("applicationUrl", out var applicationUrl))
                throw new InvalidOperationException($"Invalid json: {launchSettings}");
            if (applicationUrl == null)
                throw new InvalidOperationException($"Invalid json: {launchSettings}");

            return applicationUrl.GetValue<string>();
        }
        /// <summary>
        /// vite.config.ts からポートを参照してURLを生成して返します。
        /// </summary>
        /// <returns></returns>
        public Uri GetDebuggingClientUrl() {
            var viteConfigTs = Path.Combine(WebClientProjectRoot, "vite.config.ts");
            if (!File.Exists(viteConfigTs))
                throw new FileNotFoundException(viteConfigTs);

            using var stream = new StreamReader(viteConfigTs, Encoding.UTF8);
            var regex = new Regex(@"port:\s*([^,]*)");
            while (!stream.EndOfStream) {
                var line = stream.ReadLine();
                if (line == null) continue;
                var match = regex.Match(line);
                if (!match.Success) continue;
                var port = match.Groups[1].Value;
                return new Uri($"http://localhost:{port}");
            }

            throw new InvalidOperationException("vite.config.ts からポート番号を読み取れません。'port: 9999'のようにポートを設定している行があるか確認してください。");
        }
    }
}
