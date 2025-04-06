using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.StaticEnumModelModules {
    /// <summary>
    /// 静的区分の種類
    /// </summary>
    internal class StaticEnumDef {

        internal StaticEnumDef(EnumDefParser parser, RootAggregate rootAggregate) {
            _parser = parser;
            _rootAggregate = rootAggregate;
        }

        private readonly EnumDefParser _parser;
        private readonly RootAggregate _rootAggregate;

        internal string DisplayName => _parser.DisplayName;
        internal string CsEnumName => _parser.CsEnumName;
        internal string TsTypeName => _parser.TsTypeName;

        internal IEnumerable<StaticEnumValueDef> GetValues() {
            return _rootAggregate
                .GetMembers()
                .Cast<StaticEnumValueDef>();
        }

        internal string RenderCSharp() {
            return $$"""
                /// <summary>
                /// {{DisplayName}}
                /// </summary>
                public enum {{CsEnumName}} {
                {{GetValues().SelectTextTemplate(item => $$"""
                    [Display(Name = "{{item.DisplayName.Replace("\"", "\\\"")}}")]
                    {{item.PhysicalName}} = {{item.EnumValue}},
                """)}}
                }
                """;
        }

        internal string RenderTypeScript() {
            return $$"""
                /** {{DisplayName}} */
                export type {{TsTypeName}} = {{GetValues().Select(v => $"'{v.DisplayName.Replace("'", "\'")}'").Join(" | ")}}
                """;
        }
    }
}
