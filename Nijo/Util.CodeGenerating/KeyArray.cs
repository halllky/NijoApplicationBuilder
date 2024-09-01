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

            var list = keys.Select((key, index) => {

                // 物理名が重複する場合に名前の最後に連番を振ることで区別するためのインデックス
                var indexInSameNameGroup = sameNameGroups[key.MemberName].IndexOf(key);

                return new KeyArray {
                    Member = key,
                    CsType = key.Options.MemberType.GetCSharpTypeName() + (nullable ? "?" : string.Empty),
                    TsType = key.Options.MemberType.GetTypeScriptTypeName(),
                    VarName = key.MemberName + (indexInSameNameGroup >= 1 ? (indexInSameNameGroup + 1).ToString() : string.Empty),
                    Index = index,
                };
            }).ToArray();
            return list;
        }

        private KeyArray() { }

        internal required AggregateMember.ValueMember Member { get; init; }
        internal required string CsType { get; init; }
        internal required string TsType { get; init; }
        internal required string VarName { get; init; }
        internal required int Index { get; init; }
    }
}
