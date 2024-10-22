using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Features.Logging;

namespace Nijo.Parts {
    internal class Configure {
        internal const string CLASSNAME_CORE = "DefaultConfiguration";
        internal const string CLASSNAME_WEBAPI = "DefaultConfigurationInWebApi";
        internal const string CLASSNAME_CLI = "DefaultConfigurationInCli";

        internal const string INIT_WEB_HOST_BUILDER = "InitWebHostBuilder";
        internal const string INIT_BATCH_PROCESS = "InitAsBatchProcess";
        internal const string CONFIGURE_SERVICES = "ConfigureServices";
        internal const string INIT_WEBAPPLICATION = "InitWebApplication";

        internal static string GetClassFullname(Config config) => $"{config.RootNamespace}.{CLASSNAME_CORE}";

        internal static SourceFile RenderConfigureServices() {
            return new SourceFile {
                FileName = "DefaultConfigurer.cs",
                RenderContent = _ctx => {
                    var appSrv = new WebServer.ApplicationService();
                    var runtimeServerSettings = RuntimeSettings.ServerSetiingTypeFullName;

                    return $$"""
                        namespace {{_ctx.Config.RootNamespace}} {
                            using Microsoft.Extensions.DependencyInjection;
                            using Microsoft.Extensions.Logging;

                            public static class {{CLASSNAME_CORE}} {

                                /// <summary>
                                /// DI設定
                                /// </summary>
                                public static void {{CONFIGURE_SERVICES}}(IServiceCollection services) {

                                    // アプリケーションサービス
                                    services.AddScoped<{{appSrv.ConcreteClassName}}>();
                                    services.AddScoped<{{appSrv.AbstractClassName}}, {{appSrv.ConcreteClassName}}>();

                                    // DB接続
                                    services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(provider => {
                                        return provider.GetRequiredService<{{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}}>();
                                    });
                                    services.AddDbContext<{{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}}>((provider, option) => {
                                        var setting = provider.GetRequiredService<{{runtimeServerSettings}}>();
                                        var connStr = setting.{{RuntimeSettings.GET_ACTIVE_CONNSTR}}();
                                        Microsoft.EntityFrameworkCore.ProxiesExtensions.UseLazyLoadingProxies(option);
                                        Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions.UseSqlite(option, connStr);
                                    });

                                    // 実行時設定ファイル
                                    services.AddScoped(_ => {
                                        var filename = "{{RuntimeSettings.JSON_FILE_NAME}}";
                                        if (System.IO.File.Exists(filename)) {
                                            using var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                            var parsed = System.Text.Json.JsonSerializer.Deserialize<{{runtimeServerSettings}}>(stream);
                                            return parsed ?? {{runtimeServerSettings}}.{{RuntimeSettings.GET_DEFAULT}}();
                                        } else {
                                            var setting = {{runtimeServerSettings}}.{{RuntimeSettings.GET_DEFAULT}}();
                                            File.WriteAllText(filename, System.Text.Json.JsonSerializer.Serialize(setting, new System.Text.Json.JsonSerializerOptions {
                                                WriteIndented = true,
                                            }));
                                            return setting;
                                        }
                                    });

                                    // ログ
                                    services.AddScoped<ILogger>(provider => {
                                        var setting = provider.GetRequiredService<{{runtimeServerSettings}}>();
                                        return new {{DefaultLogger.CLASSNAME}}(setting.LogDirectory);
                                    });

                                    {{WithIndent(_ctx.CoreLibrary.ConfigureServices.SelectTextTemplate(fn => fn.Invoke("services")), "           ")}}
                                }

                            }
                        }
                        """;
                },
            };
        }

        internal static SourceFile RenderWebapiConfigure() {
            return new SourceFile {
                FileName = "DefaultConfigurer.cs",
                RenderContent = _ctx => {
                    return $$"""
                        namespace {{_ctx.Config.RootNamespace}} {
                            using Microsoft.Extensions.DependencyInjection;
                            using Microsoft.Extensions.Logging;

                            internal static class {{CLASSNAME_WEBAPI}} {

                                /// <summary>
                                /// Webサーバー起動時初期設定
                                /// </summary>
                                internal static void {{INIT_WEB_HOST_BUILDER}}(this WebApplicationBuilder builder) {
                                    {{CLASSNAME_CORE}}.{{CONFIGURE_SERVICES}}(builder.Services);

                                    // HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)
                                    builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options => {
                                        options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All);
                                    });

                                    // npm start で実行されるポートがASP.NETのそれと別なので
                                    builder.Services.AddCors(options => {
                                        options.AddDefaultPolicy(builder => {
                                            builder.WithOrigins("{{_ctx.GeneratedProject.ReactProject.GetDebuggingClientUrl().ToString().TrimEnd('/')}}")
                                                .AllowAnyMethod()
                                                .AllowAnyHeader()
                                                .AllowCredentials();
                                        });
                                    });

                                    builder.Services.AddControllers(option => {
                                        // エラーハンドリング
                                        option.Filters.Add<{{_ctx.Config.RootNamespace}}.HttpResponseExceptionFilter>();

                                        // クライアント側からアップロードされたファイルをサーバー側ストレージに保存する処理がHTTPリクエスト処理の最初で実行されるようにする
                                        option.Filters.Add<SavingUploadedFilesFilter>();

                                    }).AddJsonOptions(option => {
                                        // JSON日本語設定
                                        {{Utility.UtilityClass.CLASSNAME}}.{{Utility.UtilityClass.MODIFY_JSONOPTION}}(option.JsonSerializerOptions);
                                    });

                                    // クライアント側からアップロードされたファイルをサーバー側ストレージに保存する処理
                                    builder.Services.AddScoped<IFileAttachmentRepository, AttachmentFileRepositoryWeb>();

                                    {{WithIndent(_ctx.WebApiProject.ConfigureServices.SelectTextTemplate(fn => fn.Invoke("builder.Services")), "           ")}}
                                }

                                /// <summary>
                                /// Webサーバー起動時初期設定
                                /// </summary>
                                internal static void {{INIT_WEBAPPLICATION}}(this WebApplication app) {
                                    // 前述AddCorsの設定をするならこちらも必要
                                    app.UseCors();

                                    {{WithIndent(_ctx.WebApiProject.ConfigureWebApp.SelectTextTemplate(fn => fn.Invoke("app")), "           ")}}
                                }
                            }

                        }
                        """;
                },
            };
        }

        internal static SourceFile RenderCliConfigure() {
            return new SourceFile {
                FileName = "DefaultConfigurer.cs",
                RenderContent = _ctx => {
                    return $$"""
                        namespace {{_ctx.Config.RootNamespace}} {
                            using Microsoft.Extensions.DependencyInjection;
                            using Microsoft.Extensions.Logging;

                            internal static class {{CLASSNAME_CLI}} {
                                /// <summary>
                                /// バッチプロセス起動時初期設定
                                /// </summary>
                                internal static void {{INIT_BATCH_PROCESS}}(this IServiceCollection services) {
                                    {{CLASSNAME_CORE}}.{{CONFIGURE_SERVICES}}(services);

                                    {{WithIndent(_ctx.CliProject.ConfigureServices.SelectTextTemplate(fn => fn.Invoke("services")), "           ")}}
                                }
                            }

                        }
                        """;

                },
            };

        }
    }
}
