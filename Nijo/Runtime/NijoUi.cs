using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nijo.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nijo.Runtime {
    /// <summary>
    /// nijo.xml を生XMLではなくブラウザ上のGUIで編集する機能
    /// </summary>
    internal class NijoUi {
        internal NijoUi(string projectRoot) {
            _projectRoot = projectRoot;
        }

        private readonly string _projectRoot;

        /// <summary>
        /// Webアプリケーションを定義して返します。
        /// </summary>
        internal WebApplication CreateApp() {
            var builder = WebApplication.CreateBuilder();

            // React側のデバッグのためにポートが異なっていてもアクセスできるようにする
            const string CORS_POLICY_NAME = "AllowAll";
            builder.Services.AddCors(options => {
                options.AddPolicy(CORS_POLICY_NAME, builder => {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            var app = builder.Build();
            app.UseRouting();
            app.UseCors(CORS_POLICY_NAME);

            // ルートにアクセスされた場合、GUI設定プロジェクトのビルド後html（js, css がすべて1つのhtmlファイル内にバンドルされているもの）を返す。
            app.MapGet("/", async context => {
                var assembly = Assembly.GetExecutingAssembly();

                // このプロジェクトのプロジェクト名 + csprojで指定したリソース名 + 実ファイル名(index.html)
                const string RESOURCE_NAME = "Nijo.GuiWebAppHtml.index.html";

                using var stream = assembly.GetManifestResourceStream(RESOURCE_NAME)
                    ?? throw new InvalidOperationException("GUI設定プロジェクトのビルド結果が見つかりません。");

                using var reader = new StreamReader(stream);
                var html = await reader.ReadToEndAsync();

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(html);
            });

            app.MapGet("/load", async context => {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(LoadInitialStateJson());
            });

            //app.MapGet("/{fileName}", async context => {
            //    var fileName = context.Request.RouteValues["fileName"]?.ToString();
            //    var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            //    if (File.Exists(filePath)) {
            //        context.Response.ContentType = "application/octet-stream";
            //        await context.Response.SendFileAsync(filePath);
            //    } else {
            //        context.Response.StatusCode = StatusCodes.Status404NotFound;
            //        await context.Response.WriteAsync("File not found.");
            //    }
            //});

            return app;
        }


        #region XML設定GUIアプリ側のための処理

        // ここのデータ型はGUIアプリ側の定義と合わせる必要がある
        internal class AggregateOrMember {
            [JsonPropertyName("displayName")]
            internal string? DisplayName { get; set; }
            [JsonPropertyName("type")]
            internal string? Type { get; set; }
            [JsonPropertyName("attrs")]
            internal List<OptionalAttributeDef>? Attrs { get; set; }
            [JsonPropertyName("children")]
            internal List<AggregateOrMember>? Children { get; set; }
        }
        internal class OptionalAttributeValue {
            [JsonPropertyName("id")]
            internal string? Id { get; set; }
            [JsonPropertyName("value")]
            internal string? Value { get; set; }
        }
        internal class AggregateOrMemberTypeDef {
            [JsonPropertyName("id")]
            internal string? Id { get; set; }
            [JsonPropertyName("displayName")]
            internal string? DisplayName { get; set; }
            [JsonPropertyName("isRefTo")]
            internal bool? IsRefTo { get; set; }
            [JsonPropertyName("isVariationItem")]
            internal bool? IsVariationItem { get; set; }
        }
        internal class OptionalAttributeDef {
            [JsonPropertyName("id")]
            internal string? Id { get; set; }
            [JsonPropertyName("displayName")]
            internal string? DisplayName { get; set; }
            [JsonPropertyName("helpText")]
            internal string? HelpText { get; set; }
            [JsonPropertyName("type")]
            internal E_OptionalAttributeType? Type { get; set; }
        }
        internal enum E_OptionalAttributeType {
            [JsonPropertyName("string")]
            String,
            [JsonPropertyName("number")]
            Number,
            [JsonPropertyName("boolean")]
            Boolean,
        }

        private string LoadInitialStateJson() {

            var schemaXml = new AppSchemaXml(_projectRoot);

            // この戻り値の型と名前はGUIアプリ側の画面全体の状態の型と合わせる必要がある
            var initialData = new {
                projectRoot = _projectRoot,
                editingXmlFilePath = schemaXml.GetPath(),
            };

            return ToJson(initialData);
        }


        private static string ToJson<T>(T obj) {
            var options = _cachedOptions ??= new JsonSerializerOptions {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
            };
            return JsonSerializer.Serialize(obj, options);
        }
        private static JsonSerializerOptions? _cachedOptions;
        #endregion XML設定GUIアプリ側のための処理
    }
}
