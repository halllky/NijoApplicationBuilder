using Nijo.Ver1.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.CSharp {
    /// <summary>
    /// アプリケーション起動時に実行される設定処理。
    /// </summary>
    public class ApplicationConfigure : IMultiAggregateSourceFile {

        public const string ABSTRACT_CLASS_CORE = "DefaultConfiguration";
        public const string ABSTRACT_CLASS_WEBAPI = "DefaultConfigurationInWebApi";


        #region Add
        private readonly List<Func<string, string>> _coreConfigureServices = [];
        private readonly List<string> _coreMethods = [];
        private readonly List<string> _webapi = [];
        private readonly List<Func<string, string>> _addControllers = [];

        /// <summary>
        /// ConfigureServicesに生成されるソースコード。
        /// 引数は <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/> のインスタンスの名前。
        /// </summary>
        public ApplicationConfigure AddCoreConfigureServices(Func<string, string> render) {
            _coreConfigureServices.Add(render);
            return this;
        }
        /// <summary>
        /// Coreプロジェクトのアプリケーション起動時に実行される設定処理にメソッド等を追加します。
        /// </summary>
        /// <param name="configureServices">
        /// </param>
        /// <param name="abstractMethodSource">クラス直下にレンダリングされるソースコード</param>
        public ApplicationConfigure AddCoreMethod(string sourceCode) {
            _coreMethods.Add(sourceCode);
            return this;
        }
        /// <summary>
        /// Webapiプロジェクトのアプリケーション起動時に実行される設定処理にメソッド等を追加します。
        /// </summary>
        /// <param name="abstractMethod">ソースコード</param>
        public ApplicationConfigure AddWebapiMethod(string abstractMethod) {
            _webapi.Add(abstractMethod);
            return this;
        }
        /// <summary>
        /// AddControllersの中にレンダリングされるソース。引数はオプション変数の名前
        /// </summary>
        public ApplicationConfigure AddControllers(Func<string, string> render) {
            _addControllers.Add(render);
            return this;
        }
        #endregion Add


        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // 特になし
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            ctx.CoreLibrary(dir => {
                dir.Generate(RenderCore(ctx));
            });

            ctx.WebapiProject(dir => {
                dir.Generate(RenderWebapiConfigure(ctx));
            });
        }

        private SourceFile RenderCore(CodeRenderingContext ctx) {
            var coreConfigureServices = new List<Func<string, string>>(_coreConfigureServices);
            var coreMethods = new List<string>(_coreMethods);

            // ApplicationServiceを登録
            coreConfigureServices.Add(services => $$"""
                // アプリケーションサービス
                {{services}}.AddScoped(ConfigureApplicationService);
                """);
            coreMethods.Add($$"""
                /// <summary>
                /// アプリケーションサービスのインスタンスを定義する
                /// </summary>
                protected abstract {{ApplicationService.ABSTRACT_CLASS}} ConfigureApplicationService(IServiceProvider services);
                """);

            // NLog.Logger を登録
            coreConfigureServices.Add(services => $$"""
                // ログ出力
                {{services}}.AddSingleton(ConfigureLogger);
                """);
            coreMethods.Add($$"""
                /// <summary>
                /// ログ出力設定
                /// </summary>
                protected abstract NLog.Logger ConfigureLogger(IServiceProvider services);
                """);

            return new SourceFile {
                FileName = "DefaultConfiguration.cs",
                Contents = $$"""
                    using Microsoft.Extensions.DependencyInjection;
                    using NLog;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// 自動生成されたアプリケーション実行時設定。
                    /// 設定の一部を変更したい場合はこのクラスをオーバーライドしたクラスを作る。
                    /// </summary>
                    public abstract partial class {{ABSTRACT_CLASS_CORE}} {

                        #region DI設定
                        /// <summary>
                        /// DI設定。
                        /// このメソッドをオーバーライドするときは必ずbaseを呼び出すこと。
                        /// </summary>
                        public virtual void ConfigureServices(IServiceCollection services) {
                    {{coreConfigureServices.SelectTextTemplate(render => $$"""

                            {{WithIndent(render("services"), "        ")}}
                    """)}}
                        }
                        #endregion DI設定

                    {{coreMethods.SelectTextTemplate(source => $$"""

                        {{WithIndent(source, "    ")}}
                    """)}}
                    }
                    """,
            };
        }

        private SourceFile RenderWebapiConfigure(CodeRenderingContext _ctx) {
            return new SourceFile {
                FileName = "DefaultConfigurer.cs",
                Contents = $$"""
                    using Microsoft.AspNetCore.Mvc;
                    using Microsoft.AspNetCore.Mvc.Filters;
                    using Microsoft.AspNetCore.Mvc.ModelBinding;
                    using Microsoft.Extensions.DependencyInjection;
                    using Microsoft.Extensions.Logging;

                    namespace {{_ctx.Config.RootNamespace}};
                    internal abstract class {{ABSTRACT_CLASS_WEBAPI}} {

                        /// <summary>
                        /// <see cref="WebApplicationBuilder"/> に対する初期設定を行ないます。
                        /// </summary>
                        internal virtual void InitWebHostBuilder(WebApplicationBuilder builder) {

                            // HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)
                            builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options => {
                                options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All);
                            });

                            // npm start で実行されるポートがASP.NETのそれと別なので
                            builder.Services.AddCors(options => {
                                options.AddDefaultPolicy(builder => {
                                    builder.WithOrigins("{{_ctx.Config.ReactDebuggingUrl.TrimEnd('/')}}")
                                        .AllowAnyMethod()
                                        .AllowAnyHeader()
                                        .AllowCredentials();
                                });
                            });

                            builder.Services.AddControllers(option => {
                                // エラーハンドリング
                                // TODO ver.1: option.Filters.Add<ここでExceptionFilterを登録>();
                    {{_addControllers.SelectTextTemplate(render => $$"""

                                {{WithIndent(render("option"), "            ")}}
                    """)}}

                            }).AddJsonOptions(option => {
                                // JSON日本語設定
                                // TODO ver.1: ここでJSON日本語設定
                            });
                        }

                        /// <summary>
                        /// <see cref="WebApplication"/> に対する初期設定を行ないます。
                        /// </summary>
                        internal virtual void InitWebApplication(WebApplication app) {
                            // AddCorsの設定をするならこちらも必要
                            app.UseCors();
                        }
                    {{_webapi.SelectTextTemplate(sourceCode => $$"""

                        {{WithIndent(sourceCode, "    ")}}
                    """)}}
                    }
                    """,
            };
        }
    }
}
