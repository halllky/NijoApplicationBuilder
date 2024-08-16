using Nijo.Core;
using Nijo.Models.ReadModel2Features;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// ほかの集約から参照されるときのためのデータクラス。
    /// 登録更新に必要なキー情報のみが定義される。
    /// </summary>
    internal class DataClassForRefTargetKeys {
        internal DataClassForRefTargetKeys(GraphNode<Aggregate> agg, GraphNode<Aggregate> refEntry) {
            _aggregate = agg;
            _refEntry = refEntry;
        }

        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<Aggregate> _refEntry;

        internal string CsClassName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefTargetKeys"
            : $"{_refEntry.Item.PhysicalName}RefTargetKeys_{_aggregate.Item.PhysicalName}";
        internal string TsTypeName => _refEntry == _aggregate
            ? $"{_refEntry.Item.PhysicalName}RefTargetKeys"
            : $"{_refEntry.Item.PhysicalName}RefTargetKeys_{_aggregate.Item.PhysicalName}";

        /// <summary>
        /// この集約自身がもつメンバーを列挙します。
        /// </summary>
        /// <returns></returns>
        private IEnumerable<KeyMember> GetOwnMembers() {
            foreach (var member in _aggregate.GetKeys()) {
                if (member is AggregateMember.RelationMember rm) {
                    if (_aggregate.Source?.Source.As<Aggregate>() == rm.MemberAggregate) continue;

                    yield return new KeyMember(member, _refEntry);

                } else {
                    if (member.DeclaringAggregate == _aggregate)
                        yield return new KeyMember(member, _refEntry);
                }
            }
        }
        /// <summary>
        /// 直近の子を列挙します。
        /// </summary>
        private IEnumerable<DescendantRefTargetKeys> GetChildMembers() {
            return _aggregate
                .GetKeys()
                .OfType<AggregateMember.RelationMember>()
                .Where(rm => rm.MemberAggregate != _aggregate.Source?.Source.As<Aggregate>())
                .Select(rm => new DescendantRefTargetKeys(rm, _refEntry));
        }
        /// <summary>
        /// 子を再帰的に列挙します。
        /// </summary>
        private IEnumerable<DescendantRefTargetKeys> GetChildMembersRecursively() {
            foreach (var child in GetChildMembers()) {
                yield return child;

                foreach (var grandChild in child.GetChildMembersRecursively()) {
                    yield return grandChild;
                }
            }
        }

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharpDeclaringRecursively(CodeRenderingContext context) {
            return $$"""
                #region {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの項目のうち、登録更新に必要なキー情報のみの部分
                {{RenderCSharp()}}
                {{GetChildMembersRecursively().SelectTextTemplate(desc => $$"""
                {{desc.RenderCSharp()}}
                """)}}
                #endregion {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの項目のうち、登録更新に必要なキー情報のみの部分
                """;
        }
        private string RenderCSharp() {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}} のキー
                /// </summary>
                public partial class {{CsClassName}} {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    public required {{m.CsType}}? {{m.MemberName}} { get; set; }
                """)}}
                }
                """;
        }

        /// <summary>
        /// データ構造を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScriptDeclaringRecursively(CodeRenderingContext context) {
            return $$"""
                // {{_aggregate.Item.DisplayName}} が他の集約から参照されるときの項目のうち、登録更新に必要なキー情報のみの部分
                {{RenderTypeScript()}}
                {{GetChildMembersRecursively().SelectTextTemplate(desc => $$"""
                {{desc.RenderTypeScript()}}
                """)}}
                """;
        }
        private string RenderTypeScript() {
            return $$"""
                /** {{_aggregate.Item.DisplayName}} のキー */
                export type {{TsTypeName}} = {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}: {{m.CsType}} | undefined
                """)}}
                }
                """;
        }

        /// <summary>
        /// 画面表示用データクラスを登録更新用データクラスに変換します。
        /// </summary>
        internal string RenderFromDisplayData(string sourceInstance, GraphNode<Aggregate> sourceInstanceAgg, bool renderNewClassName) {
            var @new = renderNewClassName
                ? $"new {CsClassName}"
                : $"new()";
            var depth = _aggregate.PathFromEntry().Count();
            var x = depth == 0 ? "x" : $"x{depth}";

            return $$"""
                {{@new}} {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    {{m.MemberName}} = {{sourceInstance}}.{{m.Member.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, sourceInstanceAgg).Join("?.")}},
                """)}}
                {{GetChildMembers().SelectTextTemplate(m => m.IsArray ? $$"""
                    {{m.MemberName}} = {{sourceInstance}}.{{m.MemberAggregate.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, sourceInstanceAgg).Join("?.")}}.Select({{x}} => {{WithIndent(m.RenderFromDisplayData(x, m.MemberAggregate, true), "    ")}}).ToList() ?? [],
                """ : $$"""
                    {{m.MemberName}} = {{WithIndent(m.RenderFromDisplayData(sourceInstance, sourceInstanceAgg, false), "    ")}},
                """)}}
                }
                """;
        }


        internal const string PARENT = "PARENT";

        private class DescendantRefTargetKeys : DataClassForRefTargetKeys {
            internal DescendantRefTargetKeys(AggregateMember.RelationMember rm, GraphNode<Aggregate> refEntry) : base(rm.MemberAggregate, refEntry) {
                _relationMember = rm;
            }

            private readonly AggregateMember.RelationMember _relationMember;
            internal GraphNode<Aggregate> MemberAggregate => _relationMember.MemberAggregate;
            internal string MemberName => _relationMember.MemberName;
            internal bool IsArray => _relationMember is AggregateMember.Children;
        }

        private class KeyMember {
            internal KeyMember(AggregateMember.AggregateMemberBase member, GraphNode<Aggregate> refEntry) {
                Member = member;
                _refEntry = refEntry;
            }
            internal AggregateMember.AggregateMemberBase Member { get; }
            private readonly GraphNode<Aggregate> _refEntry;

            internal string MemberName => Member is AggregateMember.Parent
                ? PARENT
                : Member.MemberName;
            internal string CsType => Member is AggregateMember.ValueMember vm
                ? vm.Options.MemberType.GetCSharpTypeName()
                : new DescendantRefTargetKeys((AggregateMember.RelationMember)Member, _refEntry).CsClassName;
            internal string TsType => Member is AggregateMember.ValueMember vm
                ? vm.Options.MemberType.GetTypeScriptTypeName()
                : new DescendantRefTargetKeys((AggregateMember.RelationMember)Member, _refEntry).TsTypeName;
        }
    }
}
