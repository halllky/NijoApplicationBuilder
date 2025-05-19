using Nijo.CodeGenerating;
using Nijo.Parts.CSharp;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo.Parts.Common {
    /// <summary>
    /// 自動生成されるコードの中で必要となる、ユーザーに見せるメッセージ。
    /// 具体的な文言は環境により異なるため、DIで注入したものを使用する。
    /// </summary>
    public partial class MsgFactory : IMultiAggregateSourceFile {
        /// <summary>
        /// C#側はアプリケーションサービスにこの名前のプロパティが存在するのでそれ経由で使用する。
        /// </summary>
        public const string MSG = "MSG";

        /// <summary>
        /// 自動生成されるコードの中で <see cref="MsgFactory"/> のインスタンスにアクセスするためのインタフェース名。
        /// </summary>
        public const string CS_INTERFACE = "IDisplayMessageFactory";
        /// <summary>
        /// 自動生成されるコードの中で <see cref="MsgFactory"/> のインスタンスにアクセスするためのインタフェース名。
        /// </summary>
        public const string TS_TYPE_NAME = "DisplayMessageFactory";

        public const string CS_DEFAULT_CLASS_NAME = "DefaultMessageFactory";
        public const string GET_TS_DEFAULT_IMPL = "DefaultMessageFactory";


        #region Add
        /// <summary>
        /// 自動生成されるコードの中で使用されるメッセージを登録します。
        /// </summary>
        /// <param name="id">プログラム中からアクセスするときの識別子。アプリケーション全体で一意である必要があります。またソースコード上で使用可能な文字しか使用できません。</param>
        /// <param name="comment">説明文</param>
        /// <param name="template">メッセージのテンプレート。<code>{0}</code>などの変数を含めることができます。</param>
        private readonly Lock _lock = new();
        public MsgFactory AddMessage(string id, string comment, string template) {
            lock (_lock) {
                // ID不正チェック
                if (id.ToCSharpSafe() != id) {
                    throw new InvalidOperationException($"ソースコード上で使用できない文字列が含まれています: {id}");
                }
                // ID重複チェック
                if (_messages.TryGetValue(id, out var msg)) {
                    throw new InvalidOperationException($"識別子 '{id}' が '{msg.Template}' と '{template}' とで重複しています。");
                }
                _messages.Add(id, new MessageTemplate {
                    Id = id,
                    Comment = comment,
                    Template = template,
                });
                return this;
            }
        }
        private readonly Dictionary<string, MessageTemplate> _messages = [];
        private partial class MessageTemplate {
            public required string Id { get; init; }
            public required string Comment { get; init; }
            public required string Template { get; init; }

            /// <summary>
            /// テンプレート中に含まれる変数 {0}, {1}, ... の変数名
            /// </summary>
            public IEnumerable<string> GetParameterVarNames() {
                var match = NumberInsideCurlyBrace().Match(Template);
                if (!match.Success) yield break;

                for (int i = 0; i < match.Groups.Count; i++) {
                    yield return $"arg{i}";
                }
            }

            /// <summary>
            /// パラメータ変数をソースコードの形式に変換する。
            /// 例
            /// <list>
            /// <item>テンプレート: {0}文字以下で入力してください。</item>
            /// <item>ソースコード(C#): $"{arg0}文字以下で入力してください。"</item>
            /// <item>ソースコード(TS): `${arg0}文字以下で入力してください。`</item>
            /// </list>
            /// </summary>
            public string GetTemplateLiteral(E_CsTs csts) {
                var replaced = Template;
                var parameters = GetParameterVarNames().ToArray();
                for (int i = 0; i < parameters.Length; i++) {
                    var varName = parameters[i];
                    var before = $"{{{i}}}";
                    var after = csts == E_CsTs.CSharp
                        ? $"{{{varName}}}"
                        : $"${{{varName}}}";
                    replaced = replaced.Replace(before, after);
                }

                return csts == E_CsTs.CSharp
                    ? replaced.Replace("\"", "\\\"")
                    : replaced.Replace("`", "\\`");
            }

            [GeneratedRegex(@"\{[0-9]+\}", RegexOptions.Multiline)]
            private static partial Regex NumberInsideCurlyBrace();
        }
        #endregion Add


        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            ctx.Use<ApplicationService>().Add($$"""
                /// <summary>
                /// ユーザーに見せるメッセージの文言を返します。
                /// </summary>
                public {{CS_INTERFACE}} {{MSG}} => _displayMessageFactory ??= ServiceProvider.GetRequiredService<{{CS_INTERFACE}}>();
                private {{CS_INTERFACE}}? _displayMessageFactory;
                """);

            ctx.Use<ApplicationConfigure>()
                .AddCoreConfigureServices(services => $$"""
                    ConfigureDisplayMessageFactory({{services}});
                    """)
                .AddCoreMethod($$"""
                    /// <summary>
                    /// ユーザーに見せるメッセージの文言を構成します。
                    /// </summary>
                    protected virtual void ConfigureDisplayMessageFactory(IServiceCollection services) {
                        services.AddScoped<{{CS_INTERFACE}}, {{CS_DEFAULT_CLASS_NAME}}>();
                    }
                    """);
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            var messagesOrderById = _messages.Values.OrderBy(m => m.Id).ToArray();

            // C#側定数
            ctx.CoreLibrary(dir => {
                dir.Directory("Util", utilDir => {
                    utilDir.Generate(new SourceFile {
                        FileName = "MSG.cs",
                        Contents = $$"""
                            namespace {{ctx.Config.RootNamespace}};

                            /// <summary>
                            /// 自動生成されるコードの中で登場する、ユーザーに表示するメッセージ。
                            /// 具体的な文言はDI経由で入れ替えることができる。
                            /// </summary>
                            public interface {{CS_INTERFACE}} {
                            {{messagesOrderById.SelectTextTemplate(msg => $$"""
                                /// <summary>
                                /// {{msg.Comment}}
                                /// </summary>
                                string {{msg.Id}}({{msg.GetParameterVarNames().Select(p => $"string {p}").Join(", ")}});
                            """)}}
                            }

                            /// <summary>
                            /// <see cref="{{CS_INTERFACE}}"/> の既定の実装。
                            /// </summary>
                            public class {{CS_DEFAULT_CLASS_NAME}} : {{CS_INTERFACE}} {
                            {{messagesOrderById.SelectTextTemplate(msg => $$"""
                                public string {{msg.Id}}({{msg.GetParameterVarNames().Select(p => $"string {p}").Join(", ")}}) => $"{{msg.GetTemplateLiteral(E_CsTs.CSharp)}}";
                            """)}}
                            }
                            """,
                    });
                });
            });

            // TypeScript側定数
            ctx.ReactProject(dir => {
                dir.Directory("util", utilDir => {
                    utilDir.Generate(new SourceFile {
                        FileName = "MSG.ts",
                        Contents = $$"""
                            /**
                             * 自動生成されるコードの中で登場する、ユーザーに表示するメッセージ。
                             * 具体的な文言は React Context 経由で入れ替えることができる。
                             */
                            export type {{TS_TYPE_NAME}} = {
                            {{messagesOrderById.SelectTextTemplate(msg => $$"""
                              /**
                               * {{msg.Comment}}
                               */
                              {{msg.Id}}: ({{msg.GetParameterVarNames().Select(p => $"{p}: string").Join(", ")}}) => string
                            """)}}
                            }

                            /**
                             * {{TS_TYPE_NAME}} の既定の実装。
                             */
                            export const {{GET_TS_DEFAULT_IMPL}} = (): {{TS_TYPE_NAME}} => ({
                            {{messagesOrderById.SelectTextTemplate(msg => $$"""
                              {{msg.Id}}: ({{msg.GetParameterVarNames().Select(p => $"{p}: string").Join(", ")}}) => `{{msg.GetTemplateLiteral(E_CsTs.TypeScript)}}`,
                            """)}}
                            })
                            """,
                    });
                });
            });
        }

    }
}
