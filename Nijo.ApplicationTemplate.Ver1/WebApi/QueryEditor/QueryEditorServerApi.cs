﻿using System.Data;
using System.Data.Common;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var columns = new List<string>();
            var rows = new List<Dictionary<string, string?>>();

            while (await reader.ReadAsync()) {
                var row = new Dictionary<string, string?>();

                // カラム名収集
                if (columns.Count == 0) {
                    for (int i = 0; i < reader.FieldCount; i++) {
                        columns.Add(reader.GetName(i));
                    }
                }

                // 行データ収集
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

    [HttpGet("get-table-names")]
    public IActionResult GetTableNames() {
        try {
            var tableNames = _app.DbContext.Model
            .GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Where(tableName => tableName != null)
            .Distinct()
            .OrderBy(tableName => tableName)
            .ToList();
            return Ok(tableNames);

        } catch (Exception ex) {
            return Problem(ex.Message);
        }
    }

    [HttpPost("get-db-records")]
    public async Task<IActionResult> GetDbRecords([FromBody] DbTableEditor query) {
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
            var columns = new List<string>();
            var records = new List<EditableDbRecord>();
            while (await reader.ReadAsync()) {
                var record = new EditableDbRecord();
                record.TableName = query.TableName;
                record.ExistsInDb = true;

                if (columns.Count == 0) {
                    for (int i = 0; i < reader.FieldCount; i++) {
                        columns.Add(reader.GetName(i));
                    }
                }

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
                Columns = columns,
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
                    insertCommand.CommandText = $$"""
                        INSERT INTO "{{record.TableName}}" (
                        {{string.Join(Environment.NewLine, record.Values.Keys.Select((key, ix) => $$"""
                            {{(ix == 0 ? "" : ",")}}"{{key}}"
                        """))}}
                        ) VALUES (
                        {{string.Join(Environment.NewLine, record.Values.Values.Select((x, ix) => $$"""
                            {{(ix == 0 ? "" : ",")}} @p{{ix}}
                        """))}}
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
public class DbTableEditor {
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = "";
    [JsonPropertyName("whereClause")]
    public string WhereClause { get; set; } = "";
}

/// <summary>
/// 更新対象レコード取得結果
/// </summary>
public class GetDbRecordsReturn {
    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = [];
    [JsonPropertyName("records")]
    public List<EditableDbRecord> Records { get; set; } = [];
}

/// <summary>
/// 更新対象レコード
/// </summary>
public class EditableDbRecord {
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
