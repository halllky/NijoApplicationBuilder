using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    /// <summary>
    /// <see cref="Models.ReadModel2"/> においてサーバーからクライアント側に返すエラーメッセージ等のコンテナ。
    /// ここで設定されたエラー等は React hook form のエラーメッセージのAPIを通して表示されるため、当該APIの仕様の影響を強く受ける。
    /// 
    /// <code>インスタンス.プロパティ名.AddError(メッセージ)</code> のように直感的に書ける、
    /// 無駄なペイロードを避けるためにメッセージが無いときはJSON化されない、といった性質を持つ。
    /// </summary>
    internal class MessageReceiver {
        internal const string RECEIVER = "MessageReceiver";
        internal const string FORWARD_TO = "ForwardTo";
        internal const string EXEC_TRANSFER_MESSAGE = "ExecuteTransferMessages";

        internal const string RECEIVER_LIST = "MessageReceiverList";

        internal const string MESSAGE_OBJECT_MAPPER = "MessageObjectMapper";

        /// <summary>
        /// React hook form のsetErrorsの引数の形に準じている
        /// </summary>
        internal const string CLIENT_TYPE_TS = "[string, { types: { [key: `ERROR-${number}` | `WARN-${number}` | `INFO-${number}`]: string } }]";
        /// <summary>
        /// React hook form ではルート要素自体へのエラーはこの名前で設定される
        /// </summary>
        internal const string ROOT = "root";

        internal static SourceFile RenderCSharp() => new SourceFile {
            FileName = "MessageReceiver.cs",
            RenderContent = context => {
                return $$"""
                    using System.Collections;
                    using System.Text.Json;
                    using System.Text.Json.Nodes;

                    namespace {{context.Config.RootNamespace}};

                    /// <summary>
                    /// エラーメッセージの入れ物
                    /// </summary>
                    public partial class {{RECEIVER}} {
                        private readonly List<string> _errors = new();
                        private readonly List<string> _warnings = new();
                        private readonly List<string> _informations = new();
                    
                        /// <summary>
                        /// エラーメッセージを追加します。
                        /// </summary>
                        public void AddError(string message) {
                            _errors.Add(message);
                        }
                        /// <summary>
                        /// ワーニングメッセージを追加します。
                        /// </summary>
                        public void AddWarn(string message) {
                            _warnings.Add(message);
                        }
                        /// <summary>
                        /// インフォメーションメッセージを追加します。
                        /// </summary>
                        public void AddInfo(string message) {
                            _informations.Add(message);
                        }
                        /// <summary>
                        /// このオブジェクト内または子孫にエラーが1件以上あるかどうかを返します。
                        /// </summary>
                        public bool HasError() {
                            return EnumerateThisDescendants()
                                .Any(r => r._errors.Count > 0);
                        }

                        /// <summary>
                        /// このオブジェクトをJSON要素に変換します。
                        /// クライアント側へ返されるHTTPレスポンスではこのメソッドが使用されます。
                        /// JSON要素はクライアント側の画面でハンドリングされるエラーデータと同じ構造を持つ必要があります。
                        /// </summary>
                        /// <param name="path">React hook form のフィールドパスの記法に従った祖先要素のパス（末尾ピリオドなし）。nullの場合はルート要素であることを示す</param>
                        public virtual IEnumerable<JsonNode> ToJsonNodes(string? path) {
                            if (_errors.Count == 0 && _warnings.Count == 0 && _informations.Count == 0) {
                                yield break;
                            }
                            var types = new JsonObject();
                            for (var i = 0; i < _errors.Count; i++) {
                                types[$"ERROR-{i}"] = _errors[i]; // キーを "ERROR-" で始めるというルールはTypeScript側と合わせる必要がある
                            }
                            for (var i = 0; i < _warnings.Count; i++) {
                                types[$"WARN-{i}"] = _warnings[i]; // キーを "WARN-" で始めるというルールはTypeScript側と合わせる必要がある
                            }
                            for (var i = 0; i < _informations.Count; i++) {
                                types[$"INFO-{i}"] = _informations[i]; // キーを "INFO-" で始めるというルールはTypeScript側と合わせる必要がある
                            }
                            yield return new JsonArray {
                                path ?? "{{ROOT}}",
                                new JsonObject { ["types"] = types }, // "types" という名前は React hook form のエラーデータのルール
                            };
                        }

                        /// <summary>直近の子要素を列挙する。</summary>
                        protected virtual IEnumerable<{{RECEIVER}}> EnumerateChildren() {
                            yield break;
                        }
                        /// <summary>子孫を再帰的に列挙する。</summary>
                        private IEnumerable<{{RECEIVER}}> EnumerateThisDescendants() {
                            yield return this;
                            foreach (var descendant in EnumerateChildren().SelectMany(d => d.EnumerateThisDescendants())) {
                                yield return descendant;
                            }
                        }

                        #region WriteModelのオブジェクトに発生したエラーをReadModelのオブジェクトに転送するための仕組み（ReadModel一括更新処理用）
                        /// <summary>転送先</summary>
                        private {{RECEIVER}}? _forwardTo;

                        /// <summary>メッセージ転送紐づけ（このオブジェクトが転送する方、引数のオブジェクトが転送される方）</summary>
                        public void {{FORWARD_TO}}({{RECEIVER}} receiver) {
                            _forwardTo = receiver;
                        }
                        /// <summary>このオブジェクトと子孫のメッセージを、予め登録された転送先に転送します。</summary>
                        public void {{EXEC_TRANSFER_MESSAGE}}() {
                            ExecuteTransferMessages(_forwardTo);
                        }
                        private void ExecuteTransferMessages({{RECEIVER}}? forwardOfAncestor) {
                            var f = _forwardTo ?? forwardOfAncestor;
                            if (f != null) {
                                f._errors.AddRange(_errors);
                                f._warnings.AddRange(_warnings);
                                f._informations.AddRange(_informations);
                            }
                            foreach (var child in EnumerateChildren()) {
                                child.ExecuteTransferMessages(f);
                            }
                        }
                        #endregion WriteModelのオブジェクトに発生したエラーをReadModelのオブジェクトに転送するための仕組み（ReadModel一括更新処理用）
                    }
                    
                    /// <summary>
                    /// エラーメッセージの入れ物の配列。
                    /// 対応するデータの実際の件数が分からないので、インデクサでアクセスされた瞬間にそのインデックス位置の要素が存在するかのうように振る舞う。
                    /// </summary>
                    /// <typeparam name="T">要素の型</typeparam>
                    public partial class {{RECEIVER_LIST}}<T> : {{RECEIVER}}, IReadOnlyList<T?> where T : {{RECEIVER}}, new() {
                        private readonly Dictionary<int, T> _items = new();

                        public T this[int index] {
                            get {
                                ArgumentOutOfRangeException.ThrowIfNegative(index);

                                if (_items.TryGetValue(index, out var item)) {
                                    return item;
                                } else {
                                    var newItem = new T();
                                    _items[index] = newItem;
                                    return newItem;
                                }
                            }
                        }

                        public int Count => _items.Keys.Count == 0
                            ? 0
                            : (_items.Keys.Max() + 1);

                        public IEnumerator<T?> GetEnumerator() {
                            if (_items.Count == 0) yield break;

                            var max = _items.Keys.Max() + 1;
                            for (int i = 0; i < max; i++) {
                                if (_items.TryGetValue(i, out var item)) {
                                    yield return item;
                                } else {
                                    yield return default; // 存在しない位置の要素はnullを返す
                                }
                            }
                        }
                        IEnumerator IEnumerable.GetEnumerator() {
                            return GetEnumerator();
                        }

                        protected override IEnumerable<{{RECEIVER}}> EnumerateChildren() {
                            foreach (var item in this) {
                                if (item != null) yield return item;
                            }
                        }

                        public override IEnumerable<JsonNode> ToJsonNodes(string? path) {
                            // この配列自身に対するエラー
                            foreach (var node in base.ToJsonNodes(path)) {
                                yield return node;
                            }

                            // 配列の各要素に対するエラー
                            var items = this.ToArray();
                            for (var i = 0; i < items.Length; i++) {
                                var item = items[i];
                                if (item == null) continue;

                                foreach (var node in item.ToJsonNodes($"{path ?? "{{ROOT}}"}.{i}")) {
                                    yield return node;
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// WriteModelで発生したエラーを画面のどこに表示するか（ReadModelのどの項目にマッピングするか）を指定する処理を
                    /// 直感的に書けるようにするためのクラス
                    /// </summary>
                    public sealed class {{MESSAGE_OBJECT_MAPPER}} {
                        public {{MESSAGE_OBJECT_MAPPER}}({{RECEIVER}}[] containers) {
                            _containers = containers;
                        }
                        private readonly {{RECEIVER}}[] _containers;

                        /// <summary>
                        /// WriteModelで発生したエラーを画面のどこに表示するか（ReadModelのどの項目にマッピングするか）を指定する処理を記述してください。
                        /// </summary>
                        public void Map<TWriteModelMessage>(Action<TWriteModelMessage> mapping) where TWriteModelMessage : {{RECEIVER}} {
                            foreach (var item in _containers) {
                                if (item is TWriteModelMessage casted) {
                                    mapping(casted);
                                }
                            }
                        }
                    }
                    """;
            },
        };
    }
}
