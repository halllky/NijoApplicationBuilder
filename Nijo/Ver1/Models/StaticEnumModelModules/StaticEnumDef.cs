using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.StaticEnumModelModules {
    /// <summary>
    /// 静的区分の種類
    /// </summary>
    internal class StaticEnumDef {

        internal StaticEnumDef(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }

        private readonly RootAggregate _rootAggregate;

        internal string DisplayName => _rootAggregate.DisplayName;
        internal string CsEnumName => _rootAggregate.PhysicalName;
        internal string TsTypeName => _rootAggregate.PhysicalName;

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
