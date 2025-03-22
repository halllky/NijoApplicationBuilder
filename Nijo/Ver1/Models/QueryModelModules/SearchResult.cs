using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.QueryModelModules {
    /// <summary>
    /// QueryModelの検索結果型。
    /// SQLのSELECT句の形と対応する。
    /// EFCoreによるWhere句の付加が可能。
    /// </summary>
    internal class SearchResult {

        internal SearchResult(AggregateBase aggregate) {
            _aggregate = aggregate;
        }
        private readonly AggregateBase _aggregate;

        internal string CsClassName => $"{_aggregate.PhysicalName}SearchResult";

        internal string RenderCSharpDeclaring() {
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}}の検索結果型。
                /// SQLのSELECT句の形と対応する。
                /// </summary>
                public partial class {{CsClassName}} {
                    // TODO ver.1
                }
                """;
        }
    }
}
