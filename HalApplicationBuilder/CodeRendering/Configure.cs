using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering {
    partial class Configure : TemplateBase {
        internal Configure(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public const string CLASSNAME = "HalappConfigurer";
        public const string INIT_WEB_HOST_BUILDER = "InitWebHostBuilder";
        public const string INIT_BATCH_PROCESS = "InitAsBatchProces";
        public const string CONFIGURE_SERVICES = "ConfigureServices";
        public const string INIT_WEBAPPLICATION= "InitWebApplication";

        public override string FileName => "HalappConfigurer.cs";
        public string Namespace => _ctx.Config.RootNamespace;
        public string ClassFullname => $"{_ctx.Config.RootNamespace}.{CLASSNAME}";

        private string RuntimeServerSettings => new Util.RuntimeSettings(_ctx).ServerSetiingTypeFullName;

        protected override string Template() {
            return $$"""
                namespace {{Namespace}} {

                    internal static class {{CLASSNAME}} {

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
                                option.Filters.Add<{{Namespace}}.HttpResponseExceptionFilter>();

                            }).AddJsonOptions(option => {
                                {{Util.Utility.CLASSNAME}}.{{Util.Utility.MODIFY_JSONOPTION}}(option.JsonSerializerOptions);
                            });

                            builder.Services.AddHostedService<{{new BackgroundService.BackgroundTaskLauncher(_ctx).ClassFullname}}>();
                        }

                        internal static void {{INIT_WEBAPPLICATION}}(this WebApplication app) {
                            // 前述AddCorsの設定をするならこちらも必要
                            app.UseCors();
                        }

                        internal static void {{INIT_BATCH_PROCESS}}(this IServiceCollection services) {
                            {{CONFIGURE_SERVICES}}(services);
                        }

                        internal static void {{CONFIGURE_SERVICES}}(IServiceCollection services) {
                            services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(provider => {
                                return provider.GetRequiredService<{{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}}>();
                            });

                            services.AddDbContext<{{_ctx.Config.DbContextNamespace}}.{{_ctx.Config.DbContextName}}>((provider, option) => {
                                var setting = provider.GetRequiredService<{{RuntimeServerSettings}}>();
                                var connStr = setting.{{Util.RuntimeSettings.GET_ACTIVE_CONNSTR}}();
                                Microsoft.EntityFrameworkCore.ProxiesExtensions.UseLazyLoadingProxies(option);
                                Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions.UseSqlite(option, connStr);
                            });

                            services.AddScoped(_ => {
                                var filename = "{{Util.RuntimeSettings.JSON_FILE_NAME}}";
                                if (System.IO.File.Exists(filename)) {
                                    using var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                    var parsed = System.Text.Json.JsonSerializer.Deserialize<{{RuntimeServerSettings}}>(stream);
                                    return parsed ?? {{RuntimeServerSettings}}.{{Util.RuntimeSettings.GET_DEFAULT}}();
                                } else {
                                    var setting = {{RuntimeServerSettings}}.{{Util.RuntimeSettings.GET_DEFAULT}}();
                                    File.WriteAllText(filename, System.Text.Json.JsonSerializer.Serialize(setting, new System.Text.Json.JsonSerializerOptions {
                                        WriteIndented = true,
                                    }));
                                    return setting;
                                }
                            });

                            services.AddScoped<ILogger>(provider => {
                                var setting = provider.GetRequiredService<{{RuntimeServerSettings}}>();
                                return new {{Util.DefaultLogger.CLASSNAME}}(setting.LogDirectory);
                            });
                        }
                    }

                }
                """;
        }
    }
}
