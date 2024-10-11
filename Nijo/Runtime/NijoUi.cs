using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Nijo.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Runtime {
    /// <summary>
    /// nijo.xml を生XMLではなくブラウザ上のGUIで編集する機能
    /// </summary>
    internal class NijoUi {
        internal NijoUi(AppSchemaXml schema) {
            _appSchemaXml = schema;
        }

        private readonly AppSchemaXml _appSchemaXml;

        /// <summary>
        /// Webアプリケーションを定義して返します。
        /// </summary>
        internal WebApplication CreateApp() {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            app.UseRouting();

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
    }
}
