using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class Aggregate : ValueObject, IGraphNode {
        internal Aggregate(AggregatePath path, IReadOnlyCollection<Member> members) {
            Id = new NodeId(path.Value);
            Path = path;
            Members = members;

            foreach (var member in members) {
                member.Owner = this;
            }
        }

        public NodeId Id { get; }
        internal AggregatePath Path { get; }
        internal string DisplayName => Path.BaseName;
        internal string UniqueId => new HashedString(Path.Value).Guid.ToString().Replace("-", "");
        internal IReadOnlyCollection<Member> Members { get; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Id;
        }

        public override string ToString() => $"Aggregate[{Path.Value}]";

        internal class Member {
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
            internal Aggregate Owner { get; set; }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

            internal required string Name { get; init; }
            internal required IAggregateMemberType Type { get; init; }
            internal required bool IsPrimary { get; init; }
            internal required bool IsInstanceName { get; init; }

            public override string ToString() => Name;
        }
    }
}
