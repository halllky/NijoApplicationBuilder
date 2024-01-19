using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.CodeGenerating {
    /// <summary>
    /// 集約の主キーを配列で引数にとりたい場合が結構あるのでその時に使う。
    /// 変数名が重複しないようにするなどの考慮も行なっている。
    /// </summary>
    internal class KeyArray {

        internal static IReadOnlyList<KeyArray> Create(GraphNode<Aggregate> aggregate, bool nullable = true) {
            var keys = aggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .ToArray();
            var sameNameGroups = keys
                .GroupBy(key => key.MemberName)
                .ToDictionary(g => g.Key, g => g.ToList());
            var list = keys.Select(key => {
                var ix = sameNameGroups[key.MemberName].IndexOf(key);
                return new KeyArray {
                    CsType = key.CSharpTypeName + (nullable ? "?" : string.Empty),
                    VarName = key.MemberName + (ix >= 1 ? (ix + 1).ToString() : string.Empty)
                };
            }).ToArray();
            return list;
        }

        private KeyArray() { }

        internal required string CsType { get; init; }
        internal required string VarName { get; init; }
    }
}
