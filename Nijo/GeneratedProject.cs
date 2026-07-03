using Microsoft.Extensions.Logging;
using Nijo.CodeGenerating;
using Nijo.Parts.CSharp;
using Nijo.Parts.JavaScript;
using Nijo.SchemaParsing;
using Nijo.ImmutableSchema;
using Nijo.Parts.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.Util.DotnetEx;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;

namespace Nijo {
    /// <summary>
    /// 自動生成されるプロジェクトに対する操作を提供します。
    /// </summary>
    public class GeneratedProject {

        private const string NIJO_XML = "nijo.xml";

        /// <summary>
        /// 新規アプリのテンプレート。リリースビルド時にzip化されてこのexeのリソースとして埋め込まれる。
        /// </summary>
        private const string NEW_PROJECT_TEMPLATE_ZIP_NAME = "Nijo.NewProjectTemplate.zip";

        /// <summary>
        /// 新しいNijoAppScaffoldプロジェクトを作成します。
        /// </summary>
        /// <param name="projectRoot">プロジェクトのルートディレクトリの絶対パス。</param>
        /// <param name="project">作成されたプロジェクト。</param>
        /// <param name="error">エラー情報。</param>
        /// <returns>プロジェクトが作成された場合は true、作成できなかった場合は false。</returns>
        public static bool TryCreateNewProject(string projectRoot, [NotNullWhen(true)] out GeneratedProject? project, [NotNullWhen(false)] out string? error) {
            if (Directory.Exists(projectRoot)) {
                project = null;
                error = $"フォルダが存在します: {projectRoot}";
                return false;
            }

            try {
                Directory.CreateDirectory(projectRoot);

                // git archive したアプリケーションテンプレートを展開する。
                // アプリケーションテンプレートは埋め込みリソースになっている。
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(NEW_PROJECT_TEMPLATE_ZIP_NAME)) {
                    if (stream == null) {
                        project = null;
                        error = "アプリケーションテンプレートのリソースが見つかりません。" +
                               "利用可能なリソースは以下です。\n" +
                               string.Join("\n", assembly.GetManifestResourceNames());
                        return false;
                    }

                    using var archive = new ZipArchive(stream);
                    archive.ExtractToDirectory(projectRoot);
                }

                project = new GeneratedProject(Path.GetFullPath(projectRoot));
                error = null;
                return true;
            } catch (Exception ex) {
                project = null;
                error = $"プロジェクト作成中にエラーが発生しました: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 物理的なプロジェクトファイルを作成し、依存関係をインストールします。
        /// </summary>
        /// <param name="projectRoot">プロジェクトのルートディレクトリの絶対パス。</param>
        /// <param name="logger">ロガー。</param>
        /// <returns>成功した場合は true、エラーメッセージ付きで失敗した場合は false。</returns>
        public static (bool Success, string? ErrorMessage) CreatePhysicalProjectAndInstallDependenciesAsync(string projectRoot, ILogger logger) {
            try {
                Directory.CreateDirectory(projectRoot);

                // git archive したアプリケーションテンプレートを展開する。
                // アプリケーションテンプレートは埋め込みリソースになっている。
                // Task/Nijoリリース.bat でビルドしたときのみ埋め込まれる。
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(NEW_PROJECT_TEMPLATE_ZIP_NAME)) {
                    if (stream == null) {
                        return (false,
                            "アプリケーションテンプレートのリソースが見つかりません。" +
                            "利用可能なリソースは以下です。\n" +
                            string.Join("\n", assembly.GetManifestResourceNames()));
                    }

                    using var archive = new ZipArchive(stream);
                    archive.ExtractToDirectory(projectRoot);
                }

                return (true, null);

            } catch (Exception ex) {
                return (false, $"プロジェクト作成中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 既存のNijoAppScaffoldプロジェクトを開きます。
        /// </summary>
        /// <param name="projectRoot">プロジェクトのルートディレクトリの絶対パス。</param>
        /// <param name="project">開いたプロジェクト。</param>
        /// <param name="error">エラー情報。</param>
        /// <returns>プロジェクトが開けた場合は true、開けなかった場合は false。</returns>
        public static bool TryOpen(string projectRoot, [NotNullWhen(true)] out GeneratedProject? project, [NotNullWhen(false)] out string? error) {
            if (!Directory.Exists(projectRoot)) {
                project = null;
                error = "フォルダが存在しません。";
                return false;
            }

            var nijoXmlPath = Path.Combine(projectRoot, NIJO_XML);
            if (!File.Exists(nijoXmlPath)) {
                project = null;
                error = $"スキーマ定義ファイルが存在しません。右記パスにスキーマ定義パスを配置してください: {nijoXmlPath}";
                return false;
            }

            project = new GeneratedProject(Path.GetFullPath(projectRoot));
            error = null;
            return true;
        }

        private GeneratedProject(string projectRoot) {
            ProjectRoot = projectRoot;
        }

        /// <summary>プロジェクトのルートディレクトリの絶対パス</summary>
        public string ProjectRoot { get; }
        /// <summary>プロジェクトのスキーマ定義XMLの絶対パス</summary>
        public string SchemaXmlPath => Path.Combine(ProjectRoot, NIJO_XML);
        /// <summary>プロジェクトのビュー状態JSONの絶対パス</summary>
        public string ViewStateJsonPath => Path.Combine(ProjectRoot, "nijo.viewState.json");

        public string CoreLibraryRoot => Path.Combine(ProjectRoot, GetConfig().CoreLibraryFolderName);
        public string WebapiProjectRoot => Path.Combine(ProjectRoot, GetConfig().WebapiProjectFolderName);
        public string ReactProjectRoot => Path.Combine(ProjectRoot, GetConfig().ReactProjectFolderName);
        public string UnitTestProjectRoot => Path.Combine(ProjectRoot, GetConfig().UnitTestProjectFolderName);

        /// <summary>
        /// このプロジェクトのソースコード自動生成設定を返します。
        /// </summary>
        public GeneratedProjectOptions GetConfig() {
            if (_configCache == null) {
                var xDocument = XDocument.Load(SchemaXmlPath);
                _configCache = GeneratedProjectOptions.Parse(xDocument, true);
            }
            return _configCache;
        }
        private GeneratedProjectOptions? _configCache;

        /// <summary>
        /// スキーマ定義の検証を行ないます。
        /// </summary>
        public bool ValidateSchema(SchemaParseContext parseContext, ILogger logger) {
            var success = parseContext.TryBuildSchema(parseContext.Document, out var _, out var errors);

            // エラー内容表示
            if (!success) {
                logger.LogError("スキーマ定義にエラーがあります。");
            }
            foreach (var err in errors) {
                var path = err.XElement
                    .AncestorsAndSelf()
                    .Reverse()
                    .Skip(1)
                    .Select(el => el.Name.LocalName)
                    .Join("/");

                var errorMessages = err.OwnErrors
                    .Concat(err.AttributeErrors.SelectMany(x => x.Value, (p, v) => $"[{p.Key}] {v}"))
                    .ToArray();
                var summary = errorMessages.Length >= 2
                    ? $"{errorMessages.Length}件のエラー（{errorMessages.Join(", ")}）"
                    : errorMessages.Single();
                logger.LogError("  * {path}: {summary}", path, summary);
            }

            return success;
        }

        /// <summary>
        /// コード自動生成を実行します。
        /// </summary>
        internal bool GenerateCode(SchemaParseContext parseContext, CodeRenderingOptions renderingOptions, ILogger logger) {
            // スキーマ定義のコレクションを作成
            if (!parseContext.TryBuildSchema(parseContext.Document, out var immutableSchema, out var errors)) {
                logger.LogError("スキーマ定義にエラーがあります。エラーがある状態でソースコードの自動生成を行なうことはできません。");
                foreach (var err in errors) {
                    var path = err.XElement
                        .AncestorsAndSelf()
                        .Reverse()
                        .Skip(1)
                        .Select(el => el.Name.LocalName)
                        .Join("/");

                    var errorMessages = err.OwnErrors
                        .Concat(err.AttributeErrors.SelectMany(x => x.Value, (p, v) => $"[{p.Key}] {v}"))
                        .ToArray();
                    var summary = errorMessages.Length >= 2
                        ? $"{errorMessages.Length}件のエラー（{errorMessages.Join(", ")}）"
                        : errorMessages.Single();
                    logger.LogError("  * {path}: {summary}", path, summary);
                }
                return false;
            }

            using var ctx = new CodeRenderingContext(this, parseContext.ProjectOptions, renderingOptions, parseContext, immutableSchema);

            logger.LogInformation("ソース自動生成開始");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // ルート集約毎のコードを生成
            var rootAggregates = immutableSchema.GetRootAggregates().ToArray();
            var rootAggregateParallelOptions = new ParallelOptions {
                MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4),
            };
            Parallel.ForEach(rootAggregates, rootAggregateParallelOptions, rootAggregate => {
                logger.LogInformation("レンダリング開始: {name}", rootAggregate.DisplayName);
                try {
                    rootAggregate.Model.GenerateCode(ctx, rootAggregate);
                } catch (Exception ex) {
                    throw new InvalidOperationException($"{rootAggregate}のレンダリングで例外が発生", ex);
                }
            });

            // ルート集約1個と対応しない、モデル固有のコードを生成
            foreach (var model in rootAggregates.Select(r => r.Model).Distinct()) {
                logger.LogInformation("レンダリング開始: {name}", model.GetType().Name);
                try {
                    model.GenerateCode(ctx);
                } catch (Exception ex) {
                    throw new InvalidOperationException($"{model.GetType().Name}のレンダリングで例外が発生", ex);
                }
            }

            // スキーマ定義にかかわらず必ず生成されるモジュールの登録
            ctx.Use<ApplicationService>();
            ctx.Use<EnumFile>();
            ctx.Use<MetadataForPage>();
            ctx.CoreLibrary(dir => {
                dir.Directory("Util", utilDir => {
                    utilDir.Generate(NijoAttr.RenderDeclaration(ctx));
                });
            });

            // スキーマ定義にかかわらず必ず生成されるモジュールの登録: Query, Command モデル由来のもの
            ctx.Use<Models.ValueObjectModel.ValueObjectJsonConverter>();
            ctx.Use<MessageContainer.BaseClass>();
            ctx.Use<CommandQueryMappings>();
            ctx.Use<AspNetController.HandlerInterfaceFile>();

            // スキーマ定義にかかわらず必ず生成されるモジュールの登録: DataModel 由来のもの
            ctx.Use<DbContextClass>();
            Models.DataModelModules.ValidateCustom.RegisterApplicationServiceHooks(ctx);

            // IMultiAggregateSourceFile が別の IMultiAggregateSourceFile に依存することがあるので、
            // すべて漏らさず確実に依存関係を登録させる。
            // ソース自動生成中で一度でも登場した IMultiAggregateSourceFile それぞれ必ず1回ずつ依存関係登録メソッドを呼ぶ
            var handled = new HashSet<IMultiAggregateSourceFile>();
            while (true) {
                var appeared = ctx.GetMultiAggregateSourceFiles();
                var unhandled = appeared.Where(src => !handled.Contains(src)).ToArray();

                if (unhandled.Length == 0) {
                    break; // 全ての IMultiAggregateSourceFile の依存関係登録メソッドが呼ばれたら終了
                }
                foreach (var src in unhandled) {
                    src.RegisterDependencies(ctx);
                    handled.Add(src);
                }
            }
            // ValueMemberTypeについても同様に依存関係の登録を行う
            foreach (var vmType in parseContext.GetValueMemberTypes()) {
                vmType.RegisterDependencies(ctx);
            }

            // 以降は IMultiAggregateSourceFile の新規登録不可
            ctx.StopUseMultiAggregateSourceFiles();

            // IMultiAggregateSourceFile のレンダリング実行
            foreach (var src in ctx.GetMultiAggregateSourceFiles()) {
                logger.LogInformation("レンダリング開始: {name}", src.GetType().Name);
                try {
                    src.Render(ctx);
                } catch (Exception ex) {
                    throw new InvalidOperationException($"{src.GetType().Name}のレンダリングで例外が発生", ex);
                }
            }

            // スキーマ定義にかかわらず必ず生成されるモジュールを生成する
            foreach (var vmType in parseContext.GetValueMemberTypes()) {
                logger.LogInformation("レンダリング開始: {name}", vmType.GetType().Name);
                vmType.RenderStaticSources(ctx);
            }

            ctx.CoreLibrary(autoGenerated => {
                autoGenerated.Directory("Util", dir => {
                    dir.Generate(PresentationContext.RenderStaticCore(ctx));
                    dir.Generate(FromTo.Render(ctx));
                });
            });
            ctx.ReactProject(autoGenerated => {
                autoGenerated.Directory("util", dir => {
                    dir.Generate(ViewStateTypes.RenderViewStateTypes(ctx));
                });
            });

            // index.tsの生成
            ctx.ReactProject(autoGenerated => {
                autoGenerated.Directory("util", dir => {
                    IndexTs.Render(dir, ctx);
                });
            });

            // 生成されていないファイルやディレクトリを削除
            logger.LogInformation("不要ファイル削除開始");
            ctx.CleanUnhandledFilesAndDirectories();

            stopwatch.Stop();
            logger.LogInformation("ソース自動生成終了 ({elapsed}ms)", stopwatch.ElapsedMilliseconds);

            return true;
        }
    }
}
