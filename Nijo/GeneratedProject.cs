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
using Nijo.Util.CodeGenerating;

namespace Nijo {
    /// <summary>
    /// 自動生成されたプロジェクトに対する様々な操作を提供します。
    /// </summary>
    /// TODO: 最終的にはこの下に4つのプロジェクトがぶら下がる形になるので名前がややこしい。GeneratedApplicationとする。
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
                    log?.LogInformation("ProjectRoot: {0}", tempProject.SolutionRoot);

                    if (Directory.Exists(tempProject.SolutionRoot) || File.Exists(tempProject.SolutionRoot)) {
                        throw new InvalidOperationException($"Directory is already exists: {tempProject.SolutionRoot}");
                    }
                    Directory.CreateDirectory(tempProject.SolutionRoot);
                }

                var config = new Config {
                    RootNamespace = applicationName.ToCSharpSafe(),
                    GenerateUnusedRefToModules = false,
                    DbContextName = "MyDbContext",
                    CreateUserDbColumnName = null,
                    UpdateUserDbColumnName = null,
                    CreatedAtDbColumnName  = null,
                    UpdatedAtDbColumnName  = null,
                    VersionDbColumnName = null,
                    DisableLocalRepository = false,
                    ButtonColor = null,
                    MultiViewDetailLinkBehavior = Config.E_MultiViewDetailLinkBehavior.NavigateToEditMode,
                    VFormMaxColumnCount = null,
                    VFormMaxMemberCount = null,
                    VFormThreshold = null,
                };

                using (var _ = log?.BeginScope("nijo.xmlの作成")) {
                    var xmlPath = tempProject.SchemaXmlPath;
                    log?.LogInformation("XML Path: {0}", xmlPath);

                    // TODO: PJ間の依存関係の向きがぐちゃぐちゃなので直す
                    var defaultXml = Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream("Nijo.NewProjectDefaultXml.012_スカラメンバー網羅.xml");
                    using var sr = new StreamReader(defaultXml!, new UTF8Encoding(false));
                    var xmlContent = XDocument.Parse(sr.ReadToEnd());
                    xmlContent.Root!.Name = applicationName;

                    using var sw = SourceFile.GetStreamWriter(xmlPath);
                    sw.NewLine = "\n";
                    sw.WriteLine(xmlContent.ToString());
                }

                using (var _ = log?.BeginScope("アプリケーションテンプレートのコピー")) {
                    var resources = new Parts.EmbeddedResource.Collection(
                        Assembly.GetExecutingAssembly());

                    using (var reader = resources.FromResourceName("NIJO_APPLICATION_TEMPLATE.sln").GetStreamReader())
                    using (var writer = SourceFile.GetStreamWriter(Path.Combine(tempProject.SolutionRoot, "NIJO_APPLICATION_TEMPLATE.sln"))) {
                        while (!reader.EndOfStream) writer.WriteLine(reader.ReadLine());
                    }
                    using (var reader = resources.FromResourceName(".gitignore").GetStreamReader())
                    using (var writer = SourceFile.GetStreamWriter(Path.Combine(tempProject.SolutionRoot, ".gitignore"))) {
                        while (!reader.EndOfStream) writer.WriteLine(reader.ReadLine());
                    }
                    using (var reader = resources.FromResourceName(".editorconfig").GetStreamReader())
                    using (var writer = SourceFile.GetStreamWriter(Path.Combine(tempProject.SolutionRoot, ".editorconfig"))) {
                        while (!reader.EndOfStream) writer.WriteLine(reader.ReadLine());
                    }
                }

                using (var _ = log?.BeginScope("テンプレート中に登場するプロジェクト名を作成されるプロジェクト名に置換")) {
                    var beforeSln = Path.Combine(tempProject.SolutionRoot, "NIJO_APPLICATION_TEMPLATE.sln");
                    var afterSln = Path.Combine(tempProject.SolutionRoot, $"{config.RootNamespace}.sln");
                    File.Move(beforeSln, afterSln);

                    // テンプレート中に名前がハードコードされているファイル
                    var beforeReplace = File.ReadAllText(afterSln);
                    var afterReplace = beforeReplace.Replace("NIJO_APPLICATION_TEMPLATE", config.RootNamespace);
                    File.WriteAllText(afterSln, afterReplace, Encoding.UTF8);
                }

                using (var _ = log?.BeginScope("自動生成されるコードの初期化")) {
                    tempProject.CodeGenerator.GenerateCode();
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
                            git.StartInfo.WorkingDirectory = tempProject.SolutionRoot;
                            git.StartInfo.FileName = "git";
                            git.StartInfo.Arguments = "init";
                            git.Start();
                            git.WaitForExit();

                            git = new Process();
                            git.StartInfo.WorkingDirectory = tempProject.SolutionRoot;
                            git.StartInfo.FileName = "git";
                            git.StartInfo.Arguments = "add .";
                            git.Start();
                            git.WaitForExit();

                            git = new Process();
                            git.StartInfo.WorkingDirectory = tempProject.SolutionRoot;
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

        private GeneratedProject(string solutionRoot, IServiceProvider serviceProvider, ILogger? log) {
            if (string.IsNullOrWhiteSpace(solutionRoot))
                throw new ArgumentException($"'{nameof(solutionRoot)}' is required.");

            SolutionRoot = Path.GetFullPath(solutionRoot);
            _log = log ?? ILoggerExtension.CreateConsoleLogger();

            CodeGenerator = new NijoCodeGenerator(this, log);

            ServiceProvider = serviceProvider;

            CoreLibrary = new Parts.CoreLibrary(this);
            WebApiProject = new Parts.WebApiProject(this);
            ReactProject = new Parts.ReactProject(this);
            CliProject = new Parts.CliProject(this);
        }

        private readonly ILogger _log;

        /// <summary>自動生成されたプロジェクトのルートディレクトリ名</summary>
        public string SolutionRoot { get; }

        internal IServiceProvider ServiceProvider { get; }

        internal Parts.CoreLibrary CoreLibrary { get; }
        internal Parts.WebApiProject WebApiProject { get; }
        internal Parts.ReactProject ReactProject { get; }
        internal Parts.CliProject CliProject { get; }

        public NijoCodeGenerator CodeGenerator { get; }

        public string SchemaXmlPath => Path.Combine(SolutionRoot, "nijo.xml");

        public XDocument LoadSchemaXml() {
            using var stream = File.Open(SchemaXmlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var xmlContent = reader.ReadToEnd();
            return XDocument.Parse(xmlContent);
        }

        public Config ReadConfig() {
            return Config.FromXml(LoadSchemaXml());
        }
        /// <summary>
        /// アプリケーションスキーマを生成します。
        /// </summary>
        /// <exception cref="InvalidOperationException">アプリケーションスキーマが不正な場合</exception>
        internal AppSchema BuildSchema() {
            return Runtime.NijoUi.BuildAppSchemaFromXml(SchemaXmlPath);
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
    }
}
