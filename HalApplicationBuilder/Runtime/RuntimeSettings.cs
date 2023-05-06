using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Runtime {
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
        public class Server {
            /// <summary>
            /// 現在接続中のDBの名前。 <see cref="ConnectionStrings"/> のいずれかのキーと一致
            /// </summary>
            [JsonPropertyName("currentDb")]
            public string? CurrentDB { get; set; }
            [JsonPropertyName("db")]
            public Dictionary<string, string> ConnectionStrings { get; set; } = new();

            public string GetActiveConnectionString() {
                if (string.IsNullOrWhiteSpace(CurrentDB))
                    throw new InvalidOperationException("接続文字列が未指定です。");
                if (!ConnectionStrings.TryGetValue(CurrentDB, out var connStr))
                    throw new InvalidOperationException($"接続文字列 '{CurrentDB}' は無効です。");
                return connStr;
            }
        }
    }
}
