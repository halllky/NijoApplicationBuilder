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
using Nijo.Parts.Document;
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
        /// 新しいNijoApplicationBuilderプロジェクトを作成します。
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

            // Ver1フォルダをまるごとコピーする（暫定措置）
            const string VER1_ROOT = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.ApplicationTemplate.Ver1";
            if (!Directory.Exists(VER1_ROOT)) {
                project = null;
                error = $"テンプレートフォルダが存在しません: {VER1_ROOT}";
                return false;
            }
            DirectoryHelper.CopyDirectoryRecursively(VER1_ROOT, projectRoot);

            project = new GeneratedProject(Path.GetFullPath(projectRoot));
            error = null;
            return true;
        }

        /// <summary>
        /// 物理的なプロジェクトファイルを作成し、依存関係をインストールします。
        /// </summary>
        /// <param name="projectRoot">プロジェクトのルートディレクトリの絶対パス。</param>
        /// <param name="logger">ロガー。</param>
        /// <param name="skipNpmCi">trueの場合、npm ciコマンドをスキップします。</param>
        /// <returns>成功した場合は true、エラーメッセージ付きで失敗した場合は false。</returns>
        public static async Task<(bool Success, string? ErrorMessage)> CreatePhysicalProjectAndInstallDependenciesAsync(string projectRoot, ILogger logger, bool skipNpmCi = false) {
            try {
                Directory.CreateDirectory(projectRoot);

                // git archive したアプリケーションテンプレートを展開する。
                // アプリケーションテンプレートは埋め込みリソースになっている。
                // リポジトリのルートにある release.bat でビルドしたときのみ埋め込まれる。
                var assembly = Assembly.GetExecutingAssembly();
                const string RESOURCE_NAME = "Nijo.ApplicationTemplate.Ver1.zip";
                using (var stream = assembly.GetManifestResourceStream(RESOURCE_NAME)) {
                    if (stream == null) {
                        return (false,
                            "アプリケーションテンプレートのリソースが見つかりません。" +
                            "利用可能なリソースは以下です。\n" +
                            string.Join("\n", assembly.GetManifestResourceNames()));
                    }

                    using var archive = new ZipArchive(stream);
                    archive.ExtractToDirectory(projectRoot);
                }

                // npm ciをスキップするかどうかチェック
                if (!skipNpmCi) {
                    // npm ci
                    var npmCiResult = await ProcessExtension.ExecuteProcessAsync(startInfo => {
                        startInfo.FileName = "npm.cmd";
                        startInfo.Arguments = "ci";
                        startInfo.WorkingDirectory = Path.Combine(projectRoot, "react");
                    }, (std, line) => {
                        if (std == ProcessExtension.E_STD.StdOut) {
                            logger.LogInformation(line);
                        } else {
                            logger.LogError(line);
                        }
                    });

                    if (npmCiResult != 0) {
                        return (false, "npm ci に失敗しました。");
                    }
                } else {
                    logger.LogInformation("npm ciをスキップしました。");
                }

                return (true, null);

            } catch (Exception ex) {
                return (false, $"プロジェクト作成中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 既存のNijoApplicationBuilderプロジェクトを開きます。
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

        public string CoreLibraryRoot => Path.Combine(ProjectRoot, "core.AutoGenerated");
        public string CoreLibraryOverrideRoot => Path.Combine(ProjectRoot, "core");
        public string WebapiProjectRoot => Path.Combine(ProjectRoot, "webapi");
        public string ReactProjectRoot => Path.Combine(ProjectRoot, "react");
        public string UnitTestProjectRoot => Path.Combine(ProjectRoot, "Test");
        public string DocumentRoot => Path.Combine(ProjectRoot, "Document");

        /// <summary>
        /// このプロジェクトのソースコード自動生成設定を返します。
        /// </summary>
        public GeneratedProjectOptions GetConfig() {
            var xDocument = XDocument.Load(SchemaXmlPath);
            return new GeneratedProjectOptions(xDocument);
        }

        /// <summary>
        /// スキーマ定義の検証を行ないます。
        /// </summary>
        public bool ValidateSchema(SchemaParseContext parseContext, ILogger logger) {
            return parseContext.TryBuildSchema(parseContext.Document, out var _, logger);
        }

        /// <summary>
        /// コード自動生成を実行します。
        /// </summary>
        internal bool GenerateCode(SchemaParseContext parseContext, CodeRenderingOptions renderingOptions, ILogger logger) {
            // スキーマ定義のコレクションを作成
            if (!parseContext.TryBuildSchema(parseContext.Document, out var immutableSchema, logger)) {
                logger.LogError("エラーがある状態でソースコードの自動生成を行なうことはできません。");
                return false;
            }

            using var ctx = new CodeRenderingContext(this, GetConfig(), renderingOptions, parseContext, immutableSchema);

            logger.LogInformation("ソース自動生成開始");

            // ルート集約毎のコードを生成
            foreach (var rootAggregate in immutableSchema.GetRootAggregates()) {
                logger.LogInformation("レンダリング開始: {name}", rootAggregate.DisplayName);
                try {
                    rootAggregate.Model.GenerateCode(ctx, rootAggregate);
                } catch (Exception ex) {
                    throw new InvalidOperationException($"{rootAggregate}のレンダリングで例外が発生", ex);
                }

                // 自動生成ドキュメント
                ctx.Use<MarkdownDocument>().AddToIndexReadme(rootAggregate);
            }

            // ルート集約と対応しないコードを生成
            foreach (var model in parseContext.Models.Values) {
                logger.LogInformation("レンダリング開始: {name}", model.GetType().Name);
                try {
                    model.GenerateCode(ctx);
                } catch (Exception ex) {
                    throw new InvalidOperationException($"{model.GetType().Name}のレンダリングで例外が発生", ex);
                }
            }

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

            // スキーマ定義にかかわらず必ず生成されるモジュールの登録
            ctx.Use<ApplicationService>();
            ctx.Use<JsonUtil>();
            AspNetController.RegisterWebapiConfiguration(ctx);

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
                    dir.Generate(CharacterType.Render(ctx));
                    dir.Generate(FromTo.Render(ctx));
                });
            });
            ctx.WebapiProject(autoGenerated => {
                autoGenerated.Directory("Util", dir => {
                    dir.Generate(AspNetController.RenderAutoGeneratedEndpointAttribute(ctx));
                    dir.Generate(AspNetController.RenderAutoGeneratedEndpointMetadata(ctx));
                    dir.Generate(AspNetController.RenderPresentationContextModelBinder(ctx));
                    dir.Generate(AspNetController.RenderComplexPostRequestBodyModelBinder(ctx));
                    dir.Generate(E_AutoGeneratedActionType.RenderDeclaring(ctx));
                });
            });
            ctx.UnitTestProject(autoGenerated => {
                autoGenerated.Generate(Parts.UnitTest.AutoGeneratedTest.Render(ctx));
                autoGenerated.Directory("Util", dir => {
                    dir.Generate(Parts.UnitTest.TestUtil.Render(ctx));
                });
            });
            ctx.ReactProject(autoGenerated => {
                autoGenerated.Directory("util", dir => {
                    dir.Generate(Models.QueryModelModules.UiConstraint.RenderCommonConstraint(ctx));
                });
                autoGenerated.Generate(VForm2.RenderContainerQuery());
            });

            // index.tsの生成
            ctx.ReactProject(autoGenerated => {
                autoGenerated.Directory("util", dir => {
                    IndexTs.Render(dir, ctx);
                });
            });

            // 生成されていないファイルやディレクトリを削除
            ctx.CleanUnhandledFilesAndDirectories();

            logger.LogInformation("ソース自動生成終了");

            return true;
        }
    }
}
