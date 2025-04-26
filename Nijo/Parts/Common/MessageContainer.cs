using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.Common {
    /// <summary>
    /// エラーメッセージなどのメッセージの入れ物。
    /// 対応するモデルと同じ形の構造を持つ。
    /// 
    /// <code>インスタンス.プロパティ名.AddError(メッセージ)</code> のように直感的に書ける、
    /// 無駄なペイロードを避けるためにメッセージが無いときはJSON化されない、といった性質を持つ。
    /// </summary>
    internal abstract class MessageContainer {

        protected MessageContainer(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        protected readonly AggregateBase _aggregate;

        internal virtual string CsClassName => $"{_aggregate.PhysicalName}Messages";
        internal virtual string TsTypeName => $"{_aggregate.PhysicalName}Messages";

        /// <summary>
        /// C#クラスが何らかの基底クラスやインターフェースを実装するなら使う
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<string> GetCsClassImplements() {
            yield break;
        }

        /// <summary>
        /// このクラスに定義されるメンバーを列挙する。
        /// </summary>
        protected abstract IEnumerable<IMessageContainerMember> GetMembers();

        /// <summary>
        /// C#のクラス定義に追加で何かレンダリングが必要なコードがあればこれをオーバーライドして記載
        /// </summary>
        protected virtual string RenderCSharpAdditionalSource() {
            return SKIP_MARKER;
        }

        internal string RenderCSharp() {
            var impl = new List<string>() { CONCRETE_CLASS };
            impl.AddRange(GetCsClassImplements());

            var members = GetMembers().ToArray();

            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} のデータ構造と対応したメッセージの入れ物
                /// </summary>
                public class {{CsClassName}} : {{impl.Join(", ")}} {
                    public {{CsClassName}}(IEnumerable<string> path) : base(path) {
                {{members.SelectTextTemplate(m => $$"""
                {{If(m.NestedObject == null, () => $$"""
                        this.{{m.PhysicalName}} = new {{CONCRETE_CLASS}}([.. path, "{{m.PhysicalName}}"]);
                """).ElseIf(m.NestedObject?._aggregate is not ChildrenAggregate, () => $$"""
                        this.{{m.PhysicalName}} = new {{m.NestedObject?.CsClassName}}([.. path, "{{m.PhysicalName}}"]);
                """).Else(() => $$"""
                        this.{{m.PhysicalName}} = new {{CONCRETE_CLASS_LIST}}<{{m.NestedObject?.CsClassName}}>([.. path, "{{m.PhysicalName}}"], rowIndex => {
                            return new {{m.NestedObject?.CsClassName}}([.. path, "{{m.PhysicalName}}", rowIndex.ToString()]);
                        });
                """)}}
                """)}}
                    }

                {{members.SelectTextTemplate(m => $$"""
                    /// <summary>{{m.DisplayName}}に対して発生したメッセージの入れ物</summary>
                {{If(m.NestedObject == null, () => $$"""
                    public {{m.CsType ?? INTERFACE}} {{m.PhysicalName}} { get; }
                """).ElseIf(m.NestedObject?._aggregate is not ChildrenAggregate, () => $$"""
                    public {{m.CsType ?? m.NestedObject?.CsClassName}} {{m.PhysicalName}} { get; }
                """).Else(() => $$"""
                    public {{m.CsType ?? $"{INTERFACE_LIST}<{m.NestedObject?.CsClassName}>"}} {{m.PhysicalName}} { get; }
                """)}}
                """)}}
                    {{WithIndent(RenderCSharpAdditionalSource(), "    ")}}
                }
                """;
        }
        internal string RenderTypeScript() {
            var members = GetMembers().ToArray();

            return $$"""
                /** {{_aggregate.DisplayName}} のデータ構造と対応したメッセージの入れ物 */
                export type {{TsTypeName}} = {
                  {{WithIndent(RenderBody(this), "  ")}}
                }
                """;

            static IEnumerable<string> RenderBody(MessageContainer message) {
                foreach (var member in message.GetMembers()) {
                    if (member.NestedObject == null) {
                        yield return $$"""
                            {{member.PhysicalName}}?: Util.{{TS_CONTAINER}}
                            """;

                    } else if (member.NestedObject._aggregate is not ChildrenAggregate children) {
                        yield return $$"""
                            {{member.PhysicalName}}?: {
                              {{WithIndent(RenderBody(member.NestedObject), "  ")}}
                            }
                            """;

                    } else {
                        yield return $$"""
                            {{member.PhysicalName}}?: {
                              [key: `${number}`]: {
                                {{WithIndent(RenderBody(member.NestedObject), "    ")}}
                              }
                            }
                            """;
                    }
                }
            }
        }


        #region 基底クラス
        internal const string INTERFACE = "IMessageContainer";
        internal const string INTERFACE_LIST = "IMessageContainerList";
        internal const string CONCRETE_CLASS = "MessageContainer";
        internal const string CONCRETE_CLASS_LIST = "MessageContainerList";

        /// <summary>既定のクラスを探して返すstaticメソッド</summary>
        internal const string GET_DEFAULT_CLASS = "GetDefaultClass";

        internal const string TS_CONTAINER = "MessageContainer";
        private const string TS_ERROR = "error";
        private const string TS_WARN = "warn";
        private const string TS_INFO = "info";

        internal class BaseClass : IMultiAggregateSourceFile {

            private readonly Dictionary<string, string> _registered = new();
            /// <summary>
            /// <see cref="GET_DEFAULT_CLASS"/> の内容を登録する。
            /// </summary>
            /// <param name="interfaceName"></param>
            /// <param name="concreteClassName"></param>
            internal BaseClass Register(string interfaceName, string concreteClassName) {
                _registered.Add(interfaceName, concreteClassName);
                return this;
            }

            void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
                // 特になし
            }

            void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
                ctx.CoreLibrary(autoGenerated => {
                    autoGenerated.Directory("Util", dir => {
                        dir.Generate(RenderCSharpBaseClass(ctx));
                    });
                });
                ctx.ReactProject(autoGenerated => {
                    autoGenerated.Directory("util", dir => {
                        dir.Generate(RenderTypeScriptBaseFile());
                    });
                });
            }

            /// <summary>
            /// 基底クラスのレンダリング
            /// </summary>
            private SourceFile RenderCSharpBaseClass(CodeRenderingContext ctx) {
                var registered = new Dictionary<string, string>(_registered) {
                    { INTERFACE, CONCRETE_CLASS },
                    { CONCRETE_CLASS, CONCRETE_CLASS },
                };

                return new SourceFile {
                    FileName = "MessageContainer.cs",
                    Contents = $$"""
                        using System.Collections;
                        using System.Text.Json;
                        using System.Text.Json.Nodes;

                        namespace {{ctx.Config.RootNamespace}};

                        #region インターフェース
                        /// <summary>
                        /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物
                        /// </summary>
                        public interface {{INTERFACE}} {
                            /// <summary>エラーメッセージを付加します。</summary>
                            void AddError(string message);
                            /// <summary>警告メッセージを付加します。</summary>
                            void AddWarn(string message);
                            /// <summary>インフォメーションメッセージを付加します。</summary>
                            void AddInfo(string message);

                            /// <summary>このインスタンスまたはこのインスタンスの子孫が1件以上エラーを持っているか否かを返します。</summary>
                            bool HasError();
                            /// <summary>このインスタンスの直近の子を列挙します。</summary>
                            IEnumerable<{{INTERFACE}}> EnumerateChildren();

                            /// <summary>このインスタンスの子孫を列挙します。</summary>
                            public IEnumerable<{{INTERFACE}}> EnumerateDescendants() {
                                foreach (var child in EnumerateChildren()) {
                                    yield return child;

                                    foreach (var desc in child.EnumerateDescendants()) {
                                        yield return desc;
                                    }
                                }
                            }

                            /// <summary>このインスタンスをJsonNode型に変換します。</summary>
                            JsonObject ToJsonObject();
                        }
                        /// <summary>
                        /// 登録処理などで生じたエラーメッセージなどをHTTPレスポンスとして返すまでの入れ物の配列
                        /// </summary>
                        public interface {{INTERFACE_LIST}}<out T> : {{INTERFACE}}, IReadOnlyList<T> where T : {{INTERFACE}} {
                        }
                        #endregion インターフェース


                        #region 具象クラス
                        /// <inheritdoc cref="{{INTERFACE}}">
                        public partial class {{CONCRETE_CLASS}} : {{INTERFACE}} {
                            /// <inheritdoc cref="{{INTERFACE}}">
                            /// <param name="path">オブジェクトルートからこのインスタンスまでのパス</param>
                            public {{CONCRETE_CLASS}}(IEnumerable<string> path) {
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
                                if ((({{INTERFACE}})this).EnumerateDescendants().Any(container => container.HasError())) return true;
                                return false;
                            }

                            /// <summary>このインスタンスの直近の子を列挙します。</summary>
                            public virtual IEnumerable<{{INTERFACE}}> EnumerateChildren() {
                                yield break;
                            }

                            /// <summary>このインスタンスをJsonNode型に変換します。</summary>
                            public JsonObject ToJsonObject() {
                                throw new NotImplementedException(); // TODO ver.1
                            }

                            /// <summary>
                            /// 引数のメッセージのコンテナの形と対応する既定のインスタンスを作成して返します。
                            /// </summary>
                            public static T {{GET_DEFAULT_CLASS}}<T>(IEnumerable<string> path) where T : {{INTERFACE}} {
                                var type = typeof(T);
                        {{registered.SelectTextTemplate(kv => $$"""
                                if (type == typeof({{kv.Key}})) return (T)(object)new {{kv.Value}}(path);
                        """)}}
                                throw new InvalidOperationException($"{type.Name} には既定のメッセージコンテナクラスが存在しません。");
                            }
                        }

                        /// <inheritdoc cref="{{INTERFACE_LIST}}"/>
                        public partial class {{CONCRETE_CLASS_LIST}}<T> : {{CONCRETE_CLASS}}, {{INTERFACE_LIST}}<T> where T : {{INTERFACE}} {
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

                            public override IEnumerable<{{INTERFACE}}> EnumerateChildren() {
                                return this.Cast<{{INTERFACE}}>();
                            }
                        }
                        #endregion 具象クラス
                        """,
                };
            }
            private static SourceFile RenderTypeScriptBaseFile() {
                return new SourceFile {
                    FileName = "message-container.ts",
                    Contents = $$"""
                        /** サーバー側で発生したエラーメッセージ等の入れ物 */
                        export type {{TS_CONTAINER}} = {
                          /** エラーメッセージ */
                          {{TS_ERROR}}?: string[]
                          /** 警告メッセージ */
                          {{TS_WARN}}?: string[]
                          /** インフォメーション */
                          {{TS_INFO}}?: string[]
                        }
                        """,
                };
            }
        }
        #endregion 基底クラス


        #region メンバー
        internal interface IMessageContainerMember {
            string PhysicalName { get; }
            string DisplayName { get; }
            /// <summary>ChildまたはChildren</summary>
            MessageContainer? NestedObject { get; }
            /// <summary>未指定の場合はデフォルトの型になる</summary>
            string? CsType { get; }
        }
        #endregion メンバー
    }
}
