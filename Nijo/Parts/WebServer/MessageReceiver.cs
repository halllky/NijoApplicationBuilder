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
        internal const string RECEIVER_INTERFACE = "IDisplayMessageContainer";
        internal const string RECEIVER_ABSTRACT_CLASS = "DisplayMessageContainerBase";
        internal const string RECEIVER_CONCRETE_CLASS = "DisplayMessageContainer";
        internal const string RECEIVER_CONCRETE_CLASS_IN_GRID = "DisplayMessageContainerInGrid";
        internal const string RECEIVER_LIST = "DisplayMessageContainerList";

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
                    /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物のインターフェース
                    /// </summary>
                    public interface {{RECEIVER_INTERFACE}} {
                        void AddError(string message);
                        void AddInfo(string message);
                        void AddWarn(string message);
                        IEnumerable<{{RECEIVER_INTERFACE}}> EnumerateChildren();

                        bool HasError();
                        bool HasConfirm();
                    }

                    public static class DisplayMessageContainerExtensions {
                        /// <summary>
                        /// 子孫要素を再帰的に列挙します。
                        /// </summary>
                        public static IEnumerable<IDisplayMessageContainer> EnumerateDescendants(this IDisplayMessageContainer container) {
                            foreach (var child in container.EnumerateChildren()) {
                                yield return child;

                                foreach (var desc in child.EnumerateDescendants()) {
                                    yield return desc;
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物の抽象クラス
                    /// </summary>
                    public abstract class {{RECEIVER_ABSTRACT_CLASS}} : {{RECEIVER_INTERFACE}} {
                        public {{RECEIVER_ABSTRACT_CLASS}}(IEnumerable<string> path) {
                            _path = path;
                        }
                        private readonly IEnumerable<string> _path;

                        private readonly List<string> _errors = new();
                        private readonly List<string> _warnings = new();
                        private readonly List<string> _informations = new();

                        public virtual void AddError(string message) {
                            _errors.Add(message);
                        }
                        public virtual void AddWarn(string message) {
                            _warnings.Add(message);
                        }
                        public virtual void AddInfo(string message) {
                            _informations.Add(message);
                        }

                        public bool HasError() {
                            if (_errors.Count > 0) return true;
                            if (EnumerateDescendants().Any(container => container.HasError())) return true;
                            return false;
                        }
                        public bool HasConfirm() {
                            if (_warnings.Count > 0) return true;
                            if (EnumerateDescendants().Any(container => container.HasConfirm())) return true;
                            return false;
                        }

                        public virtual IEnumerable<JsonArray> ToReactHookFormErrors() {
                            if (_errors.Count > 0 || _warnings.Count > 0 || _informations.Count > 0) {
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
                                    _path.Any() ? string.Join(".", _path) : "root", // "root" という名前は React hook form のエラーデータのルール
                                    new JsonObject { ["types"] = types }, // "types" という名前は React hook form のエラーデータのルール
                                };
                            }
                            foreach (var child in EnumerateChildren().OfType<{{RECEIVER_ABSTRACT_CLASS}}>()) {
                                foreach (var msg in child.ToReactHookFormErrors()) {
                                    yield return msg;
                                }
                            }
                        }

                        public abstract IEnumerable<{{RECEIVER_INTERFACE}}> EnumerateChildren();

                        public IEnumerable<{{RECEIVER_ABSTRACT_CLASS}}> EnumerateDescendants() {
                            foreach (var child in EnumerateChildren().OfType<{{RECEIVER_ABSTRACT_CLASS}}>()) {
                                yield return child;

                                foreach (var desc in child.EnumerateDescendants()) {
                                    yield return desc;
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物
                    /// </summary>
                    public class {{RECEIVER_CONCRETE_CLASS}} : {{RECEIVER_ABSTRACT_CLASS}} {
                        public {{RECEIVER_CONCRETE_CLASS}}(IEnumerable<string> path) : base(path) { }

                        public override IEnumerable<{{RECEIVER_INTERFACE}}> EnumerateChildren() {
                            yield break;
                        }
                    }

                    /// <summary>
                    /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物のうち、グリッドの内部の項目。
                    /// グリッドのヘッダと自身のセルの部分の2か所にエラー等のメッセージを表示する必要があるため、
                    /// エラーメッセージが1個追加されるごとにHTTPレスポンスのエラーメッセージのオブジェクトが2個ずつ増えていく。
                    /// </summary>
                    public class {{RECEIVER_CONCRETE_CLASS_IN_GRID}} : {{RECEIVER_ABSTRACT_CLASS}} {
                        /// <param name="path">このメンバー自身のパス</param>
                        /// <param name="gridRoot">グリッドに表示されるメッセージの入れ物</param>
                        /// <param name="rowIndex">このオブジェクトがグリッドの何行目か</param>
                        public {{RECEIVER_CONCRETE_CLASS_IN_GRID}}(IEnumerable<string> path, {{RECEIVER_ABSTRACT_CLASS}} gridRoot, int rowIndex) : base(path) {
                            _gridRoot = gridRoot;
                            _rowIndex = rowIndex;
                        }
                        private readonly {{RECEIVER_ABSTRACT_CLASS}} _gridRoot;
                        private readonly int _rowIndex;

                        public override void AddError(string message) {
                            _gridRoot.AddError($"{_rowIndex + 1}行目: {message}");
                            base.AddError(message);
                        }
                        public override void AddWarn(string message) {
                            _gridRoot.AddWarn($"{_rowIndex + 1}行目: {message}");
                            base.AddWarn(message);
                        }
                        public override void AddInfo(string message) {
                            _gridRoot.AddInfo($"{_rowIndex + 1}行目: {message}");
                            base.AddInfo(message);
                        }

                        public override IEnumerable<IDisplayMessageContainer> EnumerateChildren() {
                            yield break;
                        }
                    }

                    /// <summary>
                    /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物の配列
                    /// </summary>
                    public class {{RECEIVER_LIST}}<T> : {{RECEIVER_ABSTRACT_CLASS}}, IReadOnlyList<T> where T : {{RECEIVER_INTERFACE}} {
                        public {{RECEIVER_LIST}}(IEnumerable<string> path, Func<int, T> createItem) : base(path) {
                            _createItem = createItem;
                        }

                        private readonly Func<int, T> _createItem;
                        private readonly Dictionary<int, T> _items = new();

                        public T this[int index] {
                            get {
                                ArgumentOutOfRangeException.ThrowIfNegative(index);

                                if (_items.TryGetValue(index, out var item)) {
                                    return item;
                                } else {
                                    var newItem = _createItem(index);
                                    _items[index] = newItem;
                                    return newItem;
                                }
                            }
                        }

                        public int Count => _items.Keys.Count == 0
                            ? 0
                            : (_items.Keys.Max() + 1);

                        public IEnumerator<T> GetEnumerator() {
                            if (_items.Count == 0) yield break;

                            var max = _items.Keys.Max() + 1;
                            for (int i = 0; i < max; i++) {
                                yield return this[i];
                            }
                        }
                        IEnumerator IEnumerable.GetEnumerator() {
                            return GetEnumerator();
                        }

                        public override IEnumerable<{{RECEIVER_INTERFACE}}> EnumerateChildren() {
                            return this.Cast<{{RECEIVER_INTERFACE}}>();
                        }
                    }
                    """;
            },
        };
    }
}
