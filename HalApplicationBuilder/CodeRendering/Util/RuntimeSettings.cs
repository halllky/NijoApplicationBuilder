using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Util {
    partial class RuntimeSettings : TemplateBase {
        internal RuntimeSettings(CodeRenderingContext ctx) {
            _ctx = ctx;
        }

        private readonly CodeRenderingContext _ctx;

        internal string ServerSetiingTypeFullName => $"{_ctx.Config.RootNamespace}.{nameof(RuntimeSettings)}.{SERVER}";

        public override string FileName => $"RuntimeSettings.cs";

        private const string SERVER = "Server";

        internal const string JSON_FILE_NAME = "halapp-runtime-config.json";
        internal const string TO_JSON = "ToJson";
        internal const string GET_DEFAULT = "GetDefault";
        internal const string GET_ACTIVE_CONNSTR = "GetActiveConnectionString";

        protected override string Template() {
            return $$"""
                namespace {{_ctx.Config.RootNamespace}} {
                    using System.Text.Json;
                    using System.Text.Json.Serialization;

                    public static class RuntimeSettings {

                        /// <summary>
                        /// 実行時クライアント側設定
                        /// </summary>
                        public class Client {
                            [JsonPropertyName("server")]
                            public string? ApServerUri { get; set; }
                        }

                        /// <summary>
                        /// 実行時サーバー側設定。機密情報を含んでよい。
                        /// 本番環境ではサーバー管理者のみ閲覧編集可能、デバッグ環境では画面から閲覧編集可能。
                        /// </summary>
                        public class {{SERVER}} {
                            /// <summary>
                            /// ログ出力先ディレクトリ
                            /// </summary>
                            [JsonPropertyName("log")]
                            public string? LogDirectory { get; set; }
                            /// <summary>
                            /// バッチ実行結果出力先ディレクトリ
                            /// </summary>
                            [JsonPropertyName("job")]
                            public string? JobDirectory { get; set; }

                            /// <summary>
                            /// 現在接続中のDBの名前。 <see cref="DbProfiles"/> のいずれかのキーと一致
                            /// </summary>
                            [JsonPropertyName("currentDb")]
                            public string? CurrentDb { get; set; }

                            [JsonPropertyName("db")]
                            public List<DbProfile> DbProfiles { get; set; } = new();
                            public class DbProfile {
                                [JsonPropertyName("name")]
                                public string Name { get; set; } = string.Empty;
                                [JsonPropertyName("connStr")]
                                public string ConnStr { get; set; } = string.Empty;
                            }

                            /// <summary>
                            /// バックグラウンド処理に関する設定
                            /// </summary>
                            [JsonPropertyName("backgroundTask")]
                            public BackgroundTaskSetting BackgroundTask { get; set; } = new();
                            public class BackgroundTaskSetting {
                                /// <summary>
                                /// ポーリング間隔（ミリ秒）
                                /// </summary>
                                [JsonPropertyName("pollingSpanMilliSeconds")]
                                public int PollingSpanMilliSeconds { get; set; } = 5000;
                            }


                            public string {{GET_ACTIVE_CONNSTR}}() {
                                if (string.IsNullOrWhiteSpace(CurrentDb))
                                    throw new InvalidOperationException("接続文字列が未指定です。");

                                var db = DbProfiles.FirstOrDefault(db => db.Name == CurrentDb);
                                if (db == null) throw new InvalidOperationException($"接続文字列 '{CurrentDb}' は無効です。");

                                return db.ConnStr;
                            }
                            public string {{TO_JSON}}() {
                                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions {
                                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                                    WriteIndented = true,
                                });
                                json = json.Replace("\\u0022", "\\\""); // ダブルクォートを\u0022ではなく\"で出力したい

                                return json;
                            }

                            /// <summary>
                            /// 規定の実行時設定を返します。
                            /// </summary>
                            public static {{SERVER}} {{GET_DEFAULT}}() {
                                var connStr = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder();
                                connStr.DataSource = "bin/Debug/debug.sqlite3";
                                connStr.Pooling = false; // デバッグ終了時にshm, walファイルが残らないようにするため

                                return new {{SERVER}} {
                                    LogDirectory = "log",
                                    JobDirectory = "job",
                                    CurrentDb = "SQLITE",
                                    DbProfiles = new List<DbProfile> {
                                        new DbProfile { Name = "SQLITE", ConnStr = connStr.ToString() },
                                    },
                                };
                            }
                        }
                    }
                }
                """;
        }
    }
}
