using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.Common {
    /// <summary>
    /// エラーメッセージなどのメッセージの入れ物。
    /// 対応するモデルと同じ形の構造を持つ。
    /// 
    /// <code>インスタンス.プロパティ名.AddError(メッセージ)</code> のように直感的に書ける、
    /// 無駄なペイロードを避けるためにメッセージが無いときはJSON化されない、といった性質を持つ。
    /// </summary>
    internal class MessageContainer {

        internal MessageContainer(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string CsClassName => $"{_aggregate.PhysicalName}Messages";
        internal string TsTypeName => $"{_aggregate.PhysicalName}Messages";

        /// <summary>
        /// DataModelの場合、ユーザーに対してDataModelの型ではなくQuery/CommandModelの型で通知する必要があるケースがあるため
        /// DataModel型のインターフェースを実装したQueryModelのメッセージコンテナを使用することがある。
        /// </summary>
        private bool UseInterface => _aggregate.GetRoot().Model is Models.DataModel;
        internal string InterfaceName => UseInterface
            ? $"I{_aggregate.PhysicalName}Messages"
            : throw new InvalidOperationException($"{_aggregate}はDataModelではないためメッセージコンテナのインターフェースを使えません。");

        internal string RenderCSharp() {
            return $$"""
                {{If(UseInterface, () => $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} のデータ構造と対応したメッセージの入れ物
                /// </summary>
                public interface {{InterfaceName}} {
                    // TODO ver.1
                }
                """)}}
                /// <summary>
                /// <see cref="{{InterfaceName}}"/> のデータ構造と対応したメッセージの入れ物
                /// </summary>
                public interface {{CsClassName}} {{(UseInterface ? $": {InterfaceName}" : "")}} {
                    // TODO ver.1
                }
                """;
        }
        internal string RenderTypeScript() {
            return $$"""
                /** {{_aggregate.DisplayName}} のデータ構造と対応したメッセージの入れ物 */
                export type {{TsTypeName}} = {
                    // TODO ver.1
                }
                """;
        }


        #region 基底クラス
        internal const string ABSTRACT_CLASS = "DisplayMessageContainerBase";
        internal const string CONCRETE_CLASS = "DisplayMessageContainer";
        internal const string CONCRETE_CLASS_IN_GRID = "DisplayMessageContainerInGrid";
        internal const string CONCRETE_CLASS_LIST = "DisplayMessageContainerList";
        internal const string LIST_INTERFACE = "IDisplayMessageContainerList";

        /// <summary>
        /// 基底クラスのレンダリング
        /// </summary>
        internal static SourceFile RenderCSharpBaseClass(CodeRenderingContext ctx) => new SourceFile {
            FileName = "MessageReceiver.cs",
            Contents = $$"""
                    using System.Collections;
                    using System.Text.Json;
                    using System.Text.Json.Nodes;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物
                    /// </summary>
                    public abstract partial class {{ABSTRACT_CLASS}} {
                        /// <inheritdoc cref="{{ABSTRACT_CLASS}}">
                        /// <param name="path">オブジェクトルートからこのインスタンスまでのパス</param>
                        public {{ABSTRACT_CLASS}}(IEnumerable<string> path) {
                            _path = path;
                        }
                        private readonly IEnumerable<string> _path;

                        private readonly List<string> _errors = new();
                        private readonly List<string> _warnings = new();
                        private readonly List<string> _informations = new();

                        /// <summary>エラーメッセージを付加します。</summary>
                        public virtual void AddError(string message) {
                            _errors.Add(message);
                        }
                        /// <summary>警告メッセージを付加します。</summary>
                        public virtual void AddWarn(string message) {
                            _warnings.Add(message);
                        }
                        /// <summary>インフォメーションメッセージを付加します。</summary>
                        public virtual void AddInfo(string message) {
                            _informations.Add(message);
                        }

                        /// <summary>このインスタンスまたはこのインスタンスの子孫が1件以上エラーを持っているか否かを返します。</summary>
                        public bool HasError() {
                            if (_errors.Count > 0) return true;
                            if (EnumerateDescendants().Any(container => container.HasError())) return true;
                            return false;
                        }

                        /// <summary>このインスタンスの直近の子を列挙します。</summary>
                        public abstract IEnumerable<{{ABSTRACT_CLASS}}> EnumerateChildren();

                        /// <summary>このインスタンスの子孫を列挙します。</summary>
                        public IEnumerable<{{ABSTRACT_CLASS}}> EnumerateDescendants() {
                            foreach (var child in EnumerateChildren()) {
                                yield return child;

                                foreach (var desc in child.EnumerateDescendants()) {
                                    yield return desc;
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// <see cref="{{ABSTRACT_CLASS}}"/> のもっとも単純な実装。
                    /// どの項目でエラーが発生したのかの情報に興味がなく、
                    /// とにかく何でもよいのでエラーの内容さえ捕捉できればよいという状況下で使う。
                    /// </summary>
                    public partial class {{CONCRETE_CLASS}} : {{ABSTRACT_CLASS}} {
                        public {{CONCRETE_CLASS}}() : base([]) { }

                        public override IEnumerable<{{ABSTRACT_CLASS}}> EnumerateChildren() {
                            yield break;
                        }
                    }

                    /// <summary>
                    /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物の配列
                    /// </summary>
                    public interface {{LIST_INTERFACE}}<out T> : {{ABSTRACT_CLASS}}, IReadOnlyList<T> where T : {{ABSTRACT_CLASS}} {
                    }

                    /// <inheritdoc cref="{{LIST_INTERFACE}}"/>
                    public partial class {{CONCRETE_CLASS_LIST}}<T> : {{ABSTRACT_CLASS}}, {{LIST_INTERFACE}}<T> where T : {{ABSTRACT_CLASS}} {
                        public {{CONCRETE_CLASS_LIST}}(IEnumerable<string> path, Func<int, T> createItem) : base(path) {
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

                        public override IEnumerable<{{ABSTRACT_CLASS}}> EnumerateChildren() {
                            return this.Cast<{{ABSTRACT_CLASS}}>();
                        }
                    }
                    """,
        };
        #endregion 基底クラス
    }
}
