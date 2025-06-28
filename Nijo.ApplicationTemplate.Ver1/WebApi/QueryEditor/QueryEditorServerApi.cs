using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MyApp.Core;

namespace Nijo.ApplicationTemplate.Ver1.WebApi.QueryEditor;

[ApiController]
[Route("api/query-editor")]
public class QueryEditorServerApi : ControllerBase {

    public QueryEditorServerApi(OverridedApplicationService app) {
        _app = app;
    }

    private readonly OverridedApplicationService _app;

    [HttpPost("execute-query")]
    public async Task<IActionResult> ExecuteQuery([FromBody] string sql) {
        try {
            using var conn = _app.DbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var command = conn.CreateCommand();
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync();

            // カラム名収集
            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++) {
                columns.Add(reader.GetName(i));
            }

            // 行データ収集
            var rows = new List<Dictionary<string, string?>>();
            while (await reader.ReadAsync()) {
                var row = new Dictionary<string, string?>();
                for (int i = 0; i < reader.FieldCount; i++) {
                    var value = reader.GetValue(i);
                    if (value == DBNull.Value) {
                        row[reader.GetName(i)] = null;
                    } else {
                        row[reader.GetName(i)] = value.ToString();
                    }
                }
                rows.Add(row);
            }

            return Ok(new ExecuteQueryReturn {
                Columns = columns,
                Rows = rows,
            });

        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("get-table-metadata")]
    public IActionResult GetTableMetadata() {
        var data = MetadataForDataPreview.EnumerateDataModelsOrderByDataFlow();

        // カスタムJsonConverterを使用してポリモーフィックシリアライゼーションを実現
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new AggregateMemberConverter() }
        };

        var json = JsonSerializer.Serialize(data, options);
        return Content(json, "application/json");
    }
    /// <summary>
    /// IAggregateMemberのポリモーフィックシリアライゼーション用のカスタムコンバーター
    /// </summary>
    private class AggregateMemberConverter : JsonConverter<MetadataForDataPreview.IAggregateMember> {
        public override MetadataForDataPreview.IAggregateMember Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException("デシリアライゼーションは実装していません");
        }

        public override void Write(Utf8JsonWriter writer, MetadataForDataPreview.IAggregateMember value, JsonSerializerOptions options) {
            // 実際の型でシリアライゼーションを行う
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }


    [HttpPost("get-db-records")]
    public async Task<IActionResult> GetDbRecords([FromBody] GetDbRecordsParameter query) {
        try {
            using var conn = _app.DbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var command = conn.CreateCommand();
            var whereClause = string.IsNullOrWhiteSpace(query.WhereClause)
                ? ""
                : $" WHERE {query.WhereClause}";
            command.CommandText = $$"""
                SELECT * FROM "{{query.TableName}}"{{whereClause}}
                """;

            using var reader = await command.ExecuteReaderAsync();

            // 行データ収集
            var records = new List<EditableDbRecord>();
            while (await reader.ReadAsync()) {
                var record = new EditableDbRecord();
                record.UniqueId = Guid.NewGuid().ToString();
                record.TableName = query.TableName;
                record.ExistsInDb = true;

                for (int i = 0; i < reader.FieldCount; i++) {
                    var value = reader.GetValue(i);
                    if (value == DBNull.Value) {
                        record.Values[reader.GetName(i)] = null;
                    } else {
                        record.Values[reader.GetName(i)] = value.ToString();
                    }
                }
                records.Add(record);
            }

            return Ok(new GetDbRecordsReturn {
                Records = records,
            });
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("batch-update")]
    public async Task<IActionResult> BatchUpdate([FromBody] List<EditableDbRecord> records) {
        try {
            using var conn = _app.DbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var tran = await conn.BeginTransactionAsync();

            foreach (var record in records) {
                await InsertOrUpdateOrDelete(conn, record);
            }

            await tran.CommitAsync();

            return Ok();
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }

        async Task InsertOrUpdateOrDelete(DbConnection conn, EditableDbRecord record) {

            // 更新対象のテーブルのキー情報を探す
            var entityType = _app.DbContext.Model
                .GetEntityTypes()
                .FirstOrDefault(x => x.GetTableName() == record.TableName);
            if (entityType == null) {
                throw new Exception($"テーブル {record.TableName} が見つかりません");
            }
            var keyProperties = entityType
                .GetProperties()
                .Where(x => x.IsPrimaryKey())
                .Select(x => x.GetColumnName()!)
                .ToList();
            if (keyProperties.Count == 0) {
                throw new Exception($"テーブル {record.TableName} に主キーがありません");
            }

            try {

                if (!record.ExistsInDb) {
                    // SQL文生成
                    using var insertCommand = conn.CreateCommand();

                    // 排他制御用のバージョンカラムはUIでは表示しないのでここで強制的に0を設定する
                    var hasVersion = entityType.GetProperties().Any(x => x.GetColumnName() == "Version");

                    insertCommand.CommandText = $$"""
                        INSERT INTO "{{record.TableName}}" (
                        {{string.Join(Environment.NewLine, record.Values.Keys.Select((key, ix) => $$"""
                            {{(ix == 0 ? "" : ",")}}"{{key}}"
                        """))}}
                        {{(hasVersion ? $$"""
                            ,"Version"
                        """ : string.Empty)}}
                        ) VALUES (
                        {{string.Join(Environment.NewLine, record.Values.Values.Select((x, ix) => $$"""
                            {{(ix == 0 ? "" : ",")}} @p{{ix}}
                        """))}}
                        {{(hasVersion ? $$"""
                            ,0
                        """ : string.Empty)}}
                        )
                        """;

                    // パラメータ設定
                    for (int ix = 0; ix < record.Values.Count; ix++) {
                        var parameter = ToDbParameter(insertCommand, $"p{ix}", record.Values.Values.ElementAt(ix));
                        insertCommand.Parameters.Add(parameter);
                    }

                    // 実行
                    var affectedRows = await insertCommand.ExecuteNonQueryAsync();
                    if (affectedRows == 0) {
                        throw new Exception($"INSERTにより影響を受けたレコードの数が0でした。");
                    }

                } else if (record.Deleted) {
                    // SQL文生成
                    using var deleteCommand = conn.CreateCommand();
                    deleteCommand.CommandText = $$"""
                        DELETE FROM "{{record.TableName}}"
                        WHERE {{string.Join(" AND ", keyProperties.Select((colName, i) => $"\"{colName}\" = @p{i}"))}}
                        """;

                    for (int i = 0; i < keyProperties.Count; i++) {
                        var parameter = ToDbParameter(deleteCommand, $"p{i}", record.Values[keyProperties[i]]);
                        deleteCommand.Parameters.Add(parameter);
                    }

                    var affectedRows = await deleteCommand.ExecuteNonQueryAsync();
                    if (affectedRows == 0) {
                        throw new Exception($"削除対象のレコードが見つかりませんでした。");
                    }

                } else if (record.Changed) {
                    // SQL文生成
                    using var updateCommand = conn.CreateCommand();
                    var values = record.Values.ToArray();
                    updateCommand.CommandText = $$"""
                        UPDATE "{{record.TableName}}"
                        SET {{string.Join(", ", values.Select((x, i) => $"\"{x.Key}\" = @pv{i}"))}}
                        WHERE {{string.Join(" AND ", keyProperties.Select((colName, i) => $"\"{colName}\" = @pk{i}"))}}
                        """;

                    // パラメータ（主キー）
                    for (int i = 0; i < keyProperties.Count; i++) {
                        var parameter = ToDbParameter(updateCommand, $"pk{i}", record.Values[keyProperties[i]]);
                        updateCommand.Parameters.Add(parameter);
                    }

                    // パラメータ（更新対象）
                    for (int i = 0; i < values.Length; i++) {
                        var parameter = ToDbParameter(updateCommand, $"pv{i}", values[i].Value);
                        updateCommand.Parameters.Add(parameter);
                    }

                    var affectedRows = await updateCommand.ExecuteNonQueryAsync();
                    if (affectedRows == 0) {
                        throw new Exception($"更新対象のレコードが見つかりませんでした。");
                    }
                }

            } catch (Exception ex) {
                throw new Exception($"【{record.TableName}（{string.Join(",", keyProperties.Select(x => $"{x}: {record.Values[x]}"))}）】: {ex.Message}", ex);
            }

            DbParameter ToDbParameter(DbCommand command, string name, string? value) {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                if (value == null) {
                    parameter.Value = DBNull.Value;
                } else {
                    parameter.Value = value;
                }
                return parameter;
            }
        }
    }
}

/// <summary>
/// クエリ実行結果
/// </summary>
public class ExecuteQueryReturn {
    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = [];
    [JsonPropertyName("rows")]
    public List<Dictionary<string, string?>> Rows { get; set; } = [];
}

/// <summary>
/// 更新対象レコード取得リクエスト
/// </summary>
public class GetDbRecordsParameter {
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = "";
    [JsonPropertyName("whereClause")]
    public string WhereClause { get; set; } = "";
}

/// <summary>
/// 更新対象レコード取得結果
/// </summary>
public class GetDbRecordsReturn {
    [JsonPropertyName("records")]
    public List<EditableDbRecord> Records { get; set; } = [];
}

/// <summary>
/// 更新対象レコード
/// </summary>
public class EditableDbRecord {
    /// <summary>
    /// UI上での一意性を担保するID
    /// </summary>
    [JsonPropertyName("uniqueId")]
    public string UniqueId { get; set; } = "";
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = "";
    [JsonPropertyName("values")]
    public Dictionary<string, string?> Values { get; set; } = [];
    [JsonPropertyName("existsInDb")]
    public bool ExistsInDb { get; set; }
    [JsonPropertyName("changed")]
    public bool Changed { get; set; }
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}
