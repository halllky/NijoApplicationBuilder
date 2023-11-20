using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.InstanceHandling {
    internal class RefTargetKeyName {
        internal RefTargetKeyName(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CSharpClassName => $"{_aggregate.Item.ClassName}KeysAndNames";
        internal string TypeScriptTypeName => $"{_aggregate.Item.ClassName}KeysAndNames";

        internal IEnumerable<Member> GetKeysAndNames() {
            return GetKeys().Union(GetNames());
        }
        internal IEnumerable<Member> GetKeys() {
            return _aggregate
                .GetKeys()
                // 自身または祖先の主キーもしくはRef。参照先の主キー(ValueMember)を除く
                .Where(m => _aggregate.EnumerateAncestorsAndThis().Contains(m.Declaring))
                .Select(m => new Member(m));
        }
        internal IEnumerable<Member> GetNames() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(member => member.IsDisplayName)
                .Select(m => new Member(m));
        }

        internal string RenderCSharpDeclaring() {
            return $$"""
                public class {{CSharpClassName}} {
                {{GetKeysAndNames().SelectTextTemplate(member => $$"""
                    [{{member.GetAnnotations().Join(", ")}}]
                    public {{member.AggMember.CSharpTypeName}} {{member.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        internal string RenderTypeScriptDeclaring() {
            return $$"""
                export type {{TypeScriptTypeName}} = {
                {{GetKeysAndNames().SelectTextTemplate(member => $$"""
                  {{member.MemberName}}: {{member.AggMember.CSharpTypeName}}
                """)}}
                }
                """;
        }

        internal IEnumerable<string> GetPathOf(AggregateMember.ValueMember fk) {
            var skip = true;
            foreach (var path in fk.Declaring.PathFromEntry()) {
                // このクラスが定義しているメンバーに至るまでのパスは無視する
                if (skip) {
                    if (path.Terminal == _aggregate) skip = false;
                    continue;
                }

                // 親の主キーはこのクラスで定義されているので、ピリオドでつなぐのではなくアンスコでつなぐ
                if (fk.Declaring.EnumerateThisAndDescendants().Contains(path.Terminal.As<Aggregate>())
                    && fk.Declaring.EnumerateThisAndDescendants().Contains(path.Initial.As<Aggregate>())) {
                    break;
                }

                // RefTargetKeyName型プロパティのプロパティ名
                yield return path.RelationName;
            }
            yield return new Member(fk).MemberName;
        }

        internal class Member : ValueObject {
            internal Member(AggregateMember.AggregateMemberBase source) {
                AggMember = source;
            }
            internal AggregateMember.AggregateMemberBase AggMember { get; }

            internal string MemberName {
                get {
                    // 親の主キーと自身の主キーの名称重複を避けるためプレフィックスをつける
                    var prefix2 = AggMember is AggregateMember.ValueMember vm2
                        && vm2.InheritedTo != null
                        && vm2.InheritedTo != AggMember.Declaring
                        ? $"{AggMember.Declaring.Item.ClassName}_"
                        : string.Empty;
                    return prefix2 + AggMember.MemberName;
                }
            }
            internal IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null) {
                foreach (var path in AggMember.GetFullPath(since).SkipLast(1)) yield return path;
                yield return MemberName;
            }
            internal IEnumerable<string> GetAnnotations() {
                var list = new List<string>();
                if (AggMember is AggregateMember.ValueMember v && v.IsKey) list.Add("Key");
                if (AggMember is AggregateMember.Ref r && r.Relation.IsPrimary()) list.Add("Key");
                if (AggMember is AggregateMember.ValueMember v2 && v2.IsDisplayName) list.Add("DisplayName");
                return list;
            }
            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return AggMember;
            }
        }
    }
}
