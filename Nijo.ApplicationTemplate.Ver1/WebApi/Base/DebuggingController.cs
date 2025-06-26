#if DEBUG

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyApp.Core;
using MyApp.Core.Debugging;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLog;
using MyApp.Core.Util;

namespace MyApp.WebApi.Base;

[ApiController]
[Route("api/debug-info")]
public class DebuggingController : ControllerBase {

    /// <summary>
    /// デバッグ用情報を返す
    /// </summary>
    [HttpGet]
    public IActionResult Index([FromServices] OverridedApplicationService app) {
        var webApiUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host; // ベースURLを取得
        return Ok($$"""
            接続先DB: {{app.Settings.CurrentDbProfileName}} (Provider: {{app.DbContext.Database.ProviderName}})
            ログディレクトリ: {{Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), app.Settings.LogDirectory))}}
            アプリケーションバージョン: {{Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "N/A"}}
            .NET ランタイムバージョン: {{Environment.Version.ToString()}}
            OS: {{RuntimeInformation.OSDescription}}
            Swagger UI: {{webApiUrl}}/swagger
            """);
    }

    /// <summary>
    /// このアプリケーションのER図を mermaid.js 形式で返す
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 調査用のSQLをhttpから実行できるようにするエンドポイント。
    /// 参照系のクエリのみ実行可能
    /// </summary>
    [HttpPost("execute-sql")]
    public async Task<IActionResult> ExecuteSql([FromBody] ExecuteSqlRequest request) {
        var app = HttpContext.RequestServices.GetRequiredService<OverridedApplicationService>(); // HttpContextから取得

        // リクエストボディの内容をログに出力（デバッグ用）
        try {
            Request.EnableBuffering(); // リクエストボディを複数回読めるようにする
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true); // leaveOpen: true
            var rawRequestBody = await reader.ReadToEndAsync();
            Console.WriteLine($"---- Received ExecuteSql Request Body ----\n{rawRequestBody}\n----------------------------------------");
            Request.Body.Position = 0; // ストリームの位置を元に戻す
        } catch (Exception ex) {
            Console.WriteLine($"Error reading request body: {ex.Message}");
        }

        var sql = request.Sql?.Trim() ?? string.Empty;

        // SELECT文以外は受け付けない基本的なチェック (必須ではないが、意図しない操作の早期発見に役立つ)
        if (string.IsNullOrWhiteSpace(sql) || !sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) {
            return BadRequest("SELECT文のみを想定しています。"); // 必要であればより厳格なメッセージに
        }

        var connection = app.DbContext.Database.GetDbConnection();
        DbTransaction? transaction = null; // トランザクション変数を宣言

        try {
            await connection.OpenAsync();
            transaction = await connection.BeginTransactionAsync(); // トランザクション開始

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction; // コマンドにトランザクションを割り当て

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<Dictionary<string, object?>>();
            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

            while (await reader.ReadAsync()) {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++) {
                    row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }

            // 結果取得後、必ずロールバックして変更を破棄
            await transaction.RollbackAsync();

            // 結果をJSON文字列にシリアライズして返す
            // System.Text.Json は object? の辞書を適切に処理できる
            return Content(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }), "application/json");

        } catch (DbException ex) {
            if (transaction != null) await transaction.RollbackAsync(); // エラー時もロールバック試行
            return BadRequest($"SQLの実行中にエラーが発生しました: {ex.Message}");
        } catch (Exception ex) {
            if (transaction != null) await transaction.RollbackAsync(); // エラー時もロールバック試行
            return StatusCode(500, $"予期せぬエラーが発生しました: {ex.Message}");
        } finally {
            // transaction?.Dispose(); // BeginTransactionAsync で取得したものは using で管理されていないため、Disposeは不要
            if (connection.State == System.Data.ConnectionState.Open) {
                await connection.CloseAsync();
            }
        }
    }

    public class ExecuteSqlRequest {
        public string? Sql { get; set; }
    }

    /// <summary>
    /// DB再作成のためのダミーデータ生成オプションを返す
    /// </summary>
    [HttpGet("dummy-data-generate-options")]
    public IActionResult GetDummyDataGenerateOptions() {
        return Ok(new DummyDataGenerateOptions());
    }

    /// <summary>
    /// DB再作成
    /// </summary>
    [HttpPost("destroy-and-reset-database")]
    public async Task<IActionResult> DestroyAndResetDatabase([FromBody] DummyDataGenerateOptions options) {
        var serviceProvider = HttpContext.RequestServices;

        try {

            // データベース定義を削除
            using (var scope = serviceProvider.CreateScope()) {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                await dbContext.Database.EnsureDeletedAsync();

                // 念のためファイル削除の完了を待機
                await Task.Delay(100);
            }

            using (var scope = serviceProvider.CreateScope()) {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                var settings = scope.ServiceProvider.GetRequiredService<RuntimeSetting>();

                // データベース定義を再作成
                await dbContext.EnsureCreatedAsyncEx(settings);

                // ダミーデータを投入
                var generator = new OverridedDummyDataGenerator();
                var dbDescriptor = new DummyDataDbOutput(dbContext);
                await generator.GenerateAsync(dbDescriptor, options);
            }

            return Ok(new {
                success = true,
                message = "データベースのリセットとダミーデータの投入が正常に完了しました",
            });
        } catch (Exception ex) {
            return StatusCode(500, new {
                success = false,
                message = "データベースのリセット中にエラーが発生しました",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}

#endif
