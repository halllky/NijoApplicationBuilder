using Nijo.Core;
using Nijo.Util.DotnetEx;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.Storing {
    /// <summary>
    /// DBに保存される、参照先の集約の情報。
    /// <see cref="TransactionScopeDataClass"/> のうち主キーのみピックアップされたもの。
    /// </summary>
    internal class TransactionScopeRefTargetClass {
        internal TransactionScopeRefTargetClass(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string CSharpClassName => $"{_aggregate.Item.ClassName}Keys";
        internal string TypeScriptTypeName => $"{_aggregate.Item.ClassName}Keys";

        /// <summary>
        /// このクラスで宣言されるメンバーのみを列挙する。
        /// つまり、親や参照先は入れ子オブジェクトとして列挙されるに留まり、親や参照先のキー等は列挙されない。
        /// </summary>
        internal IEnumerable<AggregateMember.AggregateMemberBase> GetOwnMembers() {
            return _aggregate
                .GetKeys()
                .Where(m => m is AggregateMember.Ref
                         || m is AggregateMember.Parent
                         || m is AggregateMember.ValueMember vm && vm.Declared.Owner == vm.Owner);
        }

        internal string RenderCSharpDeclaring() {
            static string GetAnnotations(AggregateMember.AggregateMemberBase member) {
                return member is AggregateMember.ValueMember v && v.IsKey
                    || member is AggregateMember.Ref r && r.Relation.IsPrimary()
                    || member is AggregateMember.Parent
                    ? "[Key]"
                    : string.Empty;
            }

            return $$"""
                public class {{CSharpClassName}} {
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                    {{GetAnnotations(member)}}
                    public {{member.CSharpTypeName}}? {{member.MemberName}} { get; set; }
                """)}}
                }
                """;
        }
        internal string RenderTypeScriptDeclaring() {
            return $$"""
                export type {{TypeScriptTypeName}} = {
                {{GetOwnMembers().SelectTextTemplate(member => $$"""
                  {{member.MemberName}}?: {{member.TypeScriptTypename}}
                """)}}
                }
                """;
        }
    }
}
