using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Searching {
    partial class SearchFeature {
        internal string RenderCSharpClassDef() {
            return $$"""
                #pragma warning disable CS8618 // null 非許容の変数には、コンストラクターの終了時に null 以外の値が入っていなければなりません

                namespace {{Context.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;

                    /// <summary>
                    /// {{DisplayName}}の一覧検索処理の検索条件を表すクラスです。
                    /// </summary>
                    public partial class {{SearchConditionClassName}} : {{SEARCHCONDITION_BASE_CLASS_NAME}} {
                {{Members.SelectTextTemplate(member => TemplateTextHelper.If(member.DbColumn.Options.MemberType.SearchBehavior == SearchBehavior.Range, () => $$"""
                        public {{Util.FromTo.CLASSNAME}}<{{member.DbColumn.Options.MemberType.GetCSharpTypeName()}}> {{member.ConditionPropName}} { get; set; } = new();
                """).Else(() => $$"""
                        public {{member.DbColumn.Options.MemberType.GetCSharpTypeName()}} {{member.ConditionPropName}} { get; set; }
                """))}}
                    }

                    /// <summary>
                    /// {{DisplayName}}の一覧検索処理の検索結果1件を表すクラスです。
                    /// </summary>
                    public partial class {{SearchResultClassName}} : {{SEARCHRESULT_BASE_CLASS_NAME}} {
                {{Members.SelectTextTemplate(member => $$"""
                        public {{member.DbColumn.Options.MemberType.GetCSharpTypeName()}} {{member.SearchResultPropName}} { get; set; }
                """)}}
                    }
                }
                """;
        }

        internal static ITemplate CreateSearchConditionBaseClassTemplate(CodeRenderingContext ctx) {
            return new SearchConditionBase { Context = ctx };
        }
        private class SearchConditionBase : TemplateBase {
            internal required CodeRenderingContext Context { get; init; }
            public override string FileName => "SearchConditionBase.cs";
            protected override string Template() {
                return $$"""
                    namespace {{Context.Config.RootNamespace}} {
                        public abstract class {{SEARCHCONDITION_BASE_CLASS_NAME}} {
                            public int? {{SEARCHCONDITION_PAGE_PROP_NAME}} { get; set; }
                        }
                    }
                    """;
            }
        }

        internal static ITemplate CreateSearchResultBaseClassTemplate(CodeRenderingContext ctx) {
            return new SearchResultBase { Context = ctx };
        }
        private class SearchResultBase : TemplateBase {
            internal required CodeRenderingContext Context { get; init; }
            public override string FileName => "SearchResultBase.cs";
            protected override string Template() {
                return $$"""
                    namespace {{Context.Config.RootNamespace}} {
                        public abstract class {{SEARCHRESULT_BASE_CLASS_NAME}} {
                            public string {{SEARCHRESULT_INSTANCE_KEY_PROP_NAME}} { get; set; } = string.Empty;
                            public string {{SEARCHRESULT_INSTANCE_NAME_PROP_NAME}} { get; set; } = string.Empty;
                        }
                    }
                    """;
            }
        }
    }
}
