using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.SchemaParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.StaticEnumModelModules {
    /// <summary>
    /// 静的区分の検索条件オブジェクト
    /// </summary>
    internal class StaticEnumSearchCondition {

        internal StaticEnumSearchCondition(StaticEnumDef staticEnum, EnumDefParser parser) {
            _staticEnum = staticEnum;
            _parser = parser;
        }

        private readonly StaticEnumDef _staticEnum;
        private readonly EnumDefParser _parser;
        internal string CsClassName => _parser.CsSearchConditionClassName;
        internal string TsTypeName => _parser.CsSearchConditionClassName;

        internal string ANY_CHECKED = "AnyChecked";

        internal string RenderCSharp() {
            return $$"""
                /// <summary>{{_staticEnum.DisplayName}}の検索条件クラス</summary>
                public class {{CsClassName}} {
                {{_staticEnum.GetValues().SelectTextTemplate(item => $$"""
                    public bool {{item.PhysicalName}} { get; set; }
                """)}}

                    /// <summary>いずれかの値が選択されているかを返します。</summary>
                    public bool {{ANY_CHECKED}}() {
                {{_staticEnum.GetValues().SelectTextTemplate(item => $$"""
                        if ({{item.PhysicalName}}) return true;
                """)}}
                        return false;
                    }
                }
                """;
        }

        internal string RenderTypeScript() {
            return $$"""
                /** {{_staticEnum.DisplayName}}の検索条件オブジェクト */
                export type {{TsTypeName}} = {
                {{_staticEnum.GetValues().SelectTextTemplate(item => $$"""
                  {{item.PhysicalName}}?: boolean
                """)}}
                }
                """;
        }
    }
}
