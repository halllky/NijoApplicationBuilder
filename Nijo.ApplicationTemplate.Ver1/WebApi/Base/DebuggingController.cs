#if DEBUG

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyApp.Core;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MyApp.WebApi.Base;

[ApiController]
[Route("api/debug-info")]
public class DebuggingController : ControllerBase {

    [HttpGet]
    public IActionResult Index([FromServices] OverridedApplicationService app) {
        return Ok($$"""
            接続先DB: {{app.Settings.CurrentDbProfileName}} (Provider: {{app.DbContext.Database.ProviderName}})
            ログディレクトリ: {{Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), app.Settings.LogDirectory))}}
            アプリケーションバージョン: {{Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "N/A"}}
            .NET ランタイムバージョン: {{Environment.Version.ToString()}}
            OS: {{RuntimeInformation.OSDescription}}
            """);
    }

    [HttpGet("er-diagram")]
    public IActionResult GetErDiagram([FromServices] OverridedApplicationService app) {
        // DbContextからテーブル定義情報を取得
        var dbContext = app.DbContext;
        var erDiagramMermaid = GenerateErDiagramMermaid(dbContext);

        return Ok($$"""
            接続先DB: {{app.Settings.CurrentDbProfileName}}

            ```mermaid
            {{erDiagramMermaid}}
            ```
            """);
    }

    /// <summary>
    /// DbContextからER図をmermaid.js形式で生成します
    /// </summary>
    private string GenerateErDiagramMermaid(MyDbContext dbContext) {
        var sb = new StringBuilder();
        sb.AppendLine("erDiagram");

        var entityTypes = dbContext.Model.GetEntityTypes().ToList();

        // エンティティ（テーブル）定義
        foreach (var entityType in entityTypes) {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableName)) continue;

            sb.Append($"    {tableName} {{");

            // プロパティ（カラム）定義
            foreach (var property in entityType.GetProperties()) {
                var columnName = property.GetColumnName();
                var typeName = property.ClrType.Name;
                var isPrimaryKey = property.IsPrimaryKey();
                var keyMarker = isPrimaryKey ? "PK " : "";

                sb.AppendLine();
                sb.Append($"        {typeName} {columnName} {keyMarker}");
            }

            sb.AppendLine();
            sb.AppendLine("    }");
        }

        // リレーションシップ定義
        foreach (var entityType in entityTypes) {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableName)) continue;

            foreach (var navigation in entityType.GetNavigations()) {
                var targetType = navigation.TargetEntityType;
                var targetTableName = targetType.GetTableName();
                if (string.IsNullOrEmpty(targetTableName)) continue;

                var foreignKey = navigation.ForeignKey;
                string relationshipType;

                if (foreignKey.IsUnique) {
                    relationshipType = "||--o{";  // 1対1 or 1対0..1
                } else {
                    relationshipType = "||--o{"; // 1対多
                }

                var relationshipName = navigation.Name;
                sb.AppendLine($"    {tableName} {relationshipType} {targetTableName} : \"{relationshipName}\"");
            }
        }

        return sb.ToString();
    }
}

#endif
