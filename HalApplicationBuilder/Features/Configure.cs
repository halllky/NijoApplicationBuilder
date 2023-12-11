using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.Features.Logging;

namespace HalApplicationBuilder.Features {
    internal class Configure {
        internal const string CLASSNAME = "HalappConfigurer";
        internal const string INIT_WEB_HOST_BUILDER = "InitWebHostBuilder";
        internal const string INIT_BATCH_PROCESS = "InitAsBatchProces";
        internal const string CONFIGURE_SERVICES = "ConfigureServices";
        internal const string INIT_WEBAPPLICATION = "InitWebApplication";

        internal static string GetClassFullname(Config config) => $"{config.RootNamespace}.{CLASSNAME}";

        internal static SourceFile Render() {
            return new SourceFile {
                FileName = "HalappConfigurer.cs",
                RenderContent = _ctx => {
                    var appSrv = new ApplicationService(_ctx.Config);
                    var runtimeServerSettings = Util.RuntimeSettings.ServerSetiingTypeFullName;

                    return $$"""
                        namespace {{_ctx.Config.RootNamespace}} {

                            internal static class {{CLASSNAME}} {

                                /// <summary>
                                /// Webサーバー起動時初期設定
                                /// </summary>
                                internal static void {{INIT_WEB_HOST_BUILDER}}(this WebApplicationBuilder builder) {
                                    {{CONFIGURE_SERVICES}}(builder.Services);

                                    // HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)
                                    builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options => {
                                        options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All);
                                    });

                                    // npm start で実行されるポートがASP.NETのそれと別なので
                                    builder.Services.AddCors(options => {
                                        options.AddDefaultPolicy(builder => {
                                            builder.AllowAnyOrigin()
                                                .AllowAnyMethod()
                                                .AllowAnyHeader();
                                        });
                                    });

                                    builder.Services.AddControllers(option => {
                                        // エラーハンドリング
                                        option.Filters.Add<{{_ctx.Config.RootNamespace}}.HttpResponseExceptionFilter>();

                                    }).AddJsonOptions(option => {
                                        // JSON日本語設定
                                        {{Util.Utility.CLASSNAME}}.{{Util.Utility.MODIFY_JSONOPTION}}(option.JsonSerializerOptions);
                                    });

                                    //// バッチ処理
                                    //builder.Services.AddHostedService<{{BackgroundService.BackgroundTaskLauncher.CLASSNAME}}>();
                                }

                                /// <summary>
                                /// Webサーバー起動時初期設定
                                /// </summary>
                                internal static void {{INIT_WEBAPPLICATION}}(this WebApplication app) {
                                    // 前述AddCorsの設定をするならこちらも必要
                                    app.UseCors();
                                }

                                /// <summary>
                                /// バッチプロセス起動時初期設定
                                /// </summary>
                                internal static void {{INIT_BATCH_PROCESS}}(this IServiceCollection services) {
                                    {{CONFIGURE_SERVICES}}(services);
                                }

                                internal static void {{CONFIGURE_SERVICES}}(IServiceCollection services) {

                                    // アプリケーションサービス
                                    services.AddScoped<{{appSrv.ClassName}}, {{appSrv.ConcreteClass}}>();

                                    // DB接続
                                    services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(provider => {
                                        return provider.GetRequiredService<{{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}}>();
                                    });
                                    services.AddDbContext<{{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}}>((provider, option) => {
                                        var setting = provider.GetRequiredService<{{runtimeServerSettings}}>();
                                        var connStr = setting.{{Util.RuntimeSettings.GET_ACTIVE_CONNSTR}}();
                                        Microsoft.EntityFrameworkCore.ProxiesExtensions.UseLazyLoadingProxies(option);
                                        Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions.UseSqlite(option, connStr);
                                    });

                                    // 実行時設定ファイル
                                    services.AddScoped(_ => {
                                        var filename = "{{Util.RuntimeSettings.JSON_FILE_NAME}}";
                                        if (System.IO.File.Exists(filename)) {
                                            using var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                            var parsed = System.Text.Json.JsonSerializer.Deserialize<{{runtimeServerSettings}}>(stream);
                                            return parsed ?? {{runtimeServerSettings}}.{{Util.RuntimeSettings.GET_DEFAULT}}();
                                        } else {
                                            var setting = {{runtimeServerSettings}}.{{Util.RuntimeSettings.GET_DEFAULT}}();
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
                                }
                            }

                        }
                        """;

                },
            };

        }
    }
}
