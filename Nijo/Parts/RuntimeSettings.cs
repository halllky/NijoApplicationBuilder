using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Parts {
    internal class RuntimeSettings {

        internal static string ServerSetiingTypeFullName => $"{nameof(RuntimeSettings)}.{SERVER}";

        private const string SERVER = "Server";

        internal const string APP_SETTINGS_SECTION_NAME = "Nijo";
        internal const string TO_JSON = "ToJson";
        internal const string GET_DEFAULT = "GetDefault";
        internal const string GET_ACTIVE_CONNSTR = "GetActiveConnectionString";

        internal static SourceFile Render(CodeRenderingContext ctx) => new SourceFile {
            FileName = $"RuntimeSettings.cs",
            RenderContent = context => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using System.Text.Json;
                    using System.Text.Json.Serialization;

                    public static partial class RuntimeSettings {

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
                        public partial class {{SERVER}} {
                            /// <summary>
                            /// ログ出力先ディレクトリ
                            /// </summary>
                            public string? LogDirectory { get; set; }
                            /// <summary>
                            /// バッチ実行結果出力先ディレクトリ
                            /// </summary>
                            public string? JobDirectory { get; set; }

                            /// <summary>
                            /// 現在接続中のDBの名前。 <see cref="DbProfiles"/> のいずれかのキーと一致
                            /// </summary>
                            public string? CurrentDb { get; set; }

                            public List<DbProfile> DbProfiles { get; set; } = new();
                            public class DbProfile {
                                public string Name { get; set; } = string.Empty;
                                public string ConnStr { get; set; } = string.Empty;
                            }

                            /// <summary>
                            /// バックグラウンド処理に関する設定
                            /// </summary>
                            public BackgroundTaskSetting BackgroundTask { get; set; } = new();
                            public class BackgroundTaskSetting {
                                /// <summary>
                                /// ポーリング間隔（ミリ秒）
                                /// </summary>
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
                            /// 既定の実行時設定を返します。
                            /// </summary>
                            public static {{SERVER}} {{GET_DEFAULT}}() {
                                var connStr = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder();
                                connStr.DataSource = "../DEBUG.sqlite3";
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
                """,
        };
    }
}
