using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Search {
    partial class SearchFeature {
        internal void RenderCSharpClassDef(ITemplate template) {
            template.WriteLine($$"""
                namespace {{Context.Config.RootNamespace}} {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;

                    /// <summary>
                    /// {{DisplayName}}の一覧検索処理の検索条件を表すクラスです。
                    /// </summary>
                    public partial class {{SearchConditionClassName}} : {{SearchCondition.BASE_CLASS_NAME}} {
                {{Members.Select(member => $$"""
                        public {{member.Type.GetCSharpTypeName()}} {{member.ConditionPropName}} { get; set; }
                """)}}
                    }

                    /// <summary>
                    /// {{DisplayName}}の一覧検索処理の検索結果1件を表すクラスです。
                    /// </summary>
                    public partial class {{SearchResultClassName}} : {{SearchResult.BASE_CLASS_NAME}} {
                {{Members.Select(member => $$"""
                        public {{member.Type.GetCSharpTypeName()}} {{member.SearchResultPropName}} { get; set; }
                """)}}
                    }
                }
                """);
        }
    }
}
