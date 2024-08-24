using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    /// <summary>
    /// <see cref="Models.ReadModel2"/> においてサーバーからクライアント側に返すエラーメッセージ等のコンテナ。
    /// 
    /// <code>インスタンス.プロパティ名.AddError(メッセージ)</code> のように直感的に書ける、
    /// 無駄なペイロードを避けるためにメッセージが無いときはJSON化されない、といった性質を持つ。
    /// </summary>
    internal class ErrorReceiver {
        internal const string RECEIVER = "ErrorReceiver";
        internal const string FORWARD_TO = "ForwardTo";
        internal const string EXEC_TRANSFER_MESSAGE = "ExecuteTransferMessages";

        internal const string RECEIVER_LIST = "ErrorReceiverList";

        internal const string ERROR_MESSAGE_MAPPER = "ErrorMessageMapper";

        /// <summary>
        /// React hook form のsetErrorsの引数の形に準じている
        /// </summary>
        internal const string CLIENT_TYPE_TS = "[string, { types: { [key: string]: string } }]";
        /// <summary>
        /// React hook form ではルート要素自体へのエラーはこの名前で設定される
        /// </summary>
        internal const string ERROR_TO_ROOT = "root";

        internal static SourceFile RenderCSharp() => new SourceFile {
            FileName = "ErrorReceiver.cs",
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
                        private readonly List<string> _errorMessages = new();

                        /// <summary>
                        /// エラーメッセージを追加します。
                        /// </summary>
                        public void Add(string message) {
                            _errorMessages.Add(message);
                        }
                        /// <summary>
                        /// このオブジェクト内または子孫にエラーが1件以上あるかどうかを返します。
                        /// </summary>
                        public bool HasError() {
                            return EnumerateThisDescendants()
                                .Any(r => r._errorMessages.Count > 0);
                        }

                        /// <summary>
                        /// このオブジェクトをJSON要素に変換します。
                        /// クライアント側へ返されるHTTPレスポンスではこのメソッドが使用されます。
                        /// JSON要素はクライアント側の画面でハンドリングされるエラーデータと同じ構造を持つ必要があります。
                        /// </summary>
                        /// <param name="path">React hook form のフィールドパスの記法に従った祖先要素のパス（末尾ピリオドなし）。nullの場合はルート要素であることを示す</param>
                        public virtual IEnumerable<JsonNode> ToJsonNodes(string? path) {
                            if (_errorMessages.Count == 0) {
                                yield break;
                            }
                            var types = new JsonObject();
                            for (var i = 0; i < _errorMessages.Count; i++) {
                                types[i.ToString()] = _errorMessages[i];
                            }
                            yield return new JsonArray {
                                path ?? "{{ERROR_TO_ROOT}}",
                                new JsonObject { ["types"] = types },
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
                                f._errorMessages.AddRange(_errorMessages);
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

                                foreach (var node in item.ToJsonNodes($"{path ?? "{{ERROR_TO_ROOT}}"}.{i}")) {
                                    yield return node;
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// WriteModelで発生したエラーを画面のどこに表示するか（ReadModelのどの項目にマッピングするか）を指定する処理を
                    /// 直感的に書けるようにするためのクラス
                    /// </summary>
                    public sealed class {{ERROR_MESSAGE_MAPPER}} {
                        public {{ERROR_MESSAGE_MAPPER}}({{RECEIVER}}[] containers) {
                            _errorContainers = containers;
                        }
                        private readonly {{RECEIVER}}[] _errorContainers;

                        /// <summary>
                        /// WriteModelで発生したエラーを画面のどこに表示するか（ReadModelのどの項目にマッピングするか）を指定する処理を記述してください。
                        /// </summary>
                        public void Map<TWriteModelError>(Action<TWriteModelError> mapping) where TWriteModelError : {{RECEIVER}} {
                            foreach (var item in _errorContainers) {
                                if (item is TWriteModelError casted) {
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
