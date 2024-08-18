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
        internal IEnumerable<KeyMember> GetValueMembers() {
            return _aggregate
                .GetKeys()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => vm.DeclaringAggregate == _aggregate)
                .Select(vm => new KeyMember(vm, _refEntry));
        }
        /// <summary>
        /// 直近の子を列挙します。
        /// </summary>
        internal IEnumerable<DescendantRefTargetKeys> GetRelationMembers() {
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
            foreach (var child in GetRelationMembers()) {
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
                {{GetValueMembers().SelectTextTemplate(m => $$"""
                    public required {{m.CsType}}? {{m.MemberName}} { get; set; }
                """)}}
                {{GetRelationMembers().SelectTextTemplate(m => $$"""
                    public required {{m.CsClassName}} {{m.MemberName}} { get; set; }
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
                {{GetValueMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}: {{m.TsType}} | undefined
                """)}}
                {{GetRelationMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}: {{m.TsTypeName}}
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
                {{GetValueMembers().SelectTextTemplate(m => $$"""
                    {{m.MemberName}} = {{sourceInstance}}.{{m.Member.Declared.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, sourceInstanceAgg).Join("?.")}},
                """)}}
                {{GetRelationMembers().SelectTextTemplate(m => m.IsArray ? $$"""
                    {{m.MemberName}} = {{sourceInstance}}.{{m.MemberAggregate.GetFullPathAsDataClassForDisplay(E_CsTs.CSharp, sourceInstanceAgg).Join("?.")}}.Select({{x}} => {{WithIndent(m.RenderFromDisplayData(x, m.MemberAggregate, true), "    ")}}).ToList() ?? [],
                """ : $$"""
                    {{m.MemberName}} = {{WithIndent(m.RenderFromDisplayData(sourceInstance, sourceInstanceAgg, false), "    ")}},
                """)}}
                }
                """;
        }


        internal const string PARENT = "PARENT";

        internal class DescendantRefTargetKeys : DataClassForRefTargetKeys {
            internal DescendantRefTargetKeys(AggregateMember.RelationMember rm, GraphNode<Aggregate> refEntry) : base(rm.MemberAggregate, refEntry) {
                _relationMember = rm;
            }

            private readonly AggregateMember.RelationMember _relationMember;
            internal GraphNode<Aggregate> MemberAggregate => _relationMember.MemberAggregate;
            internal string MemberName => _relationMember is AggregateMember.Parent
                ? PARENT
                : _relationMember.MemberName;
            internal bool IsArray => _relationMember is AggregateMember.Children;
        }

        internal class KeyMember {
            internal KeyMember(AggregateMember.ValueMember member, GraphNode<Aggregate> refEntry) {
                Member = member;
                _refEntry = refEntry;
            }
            internal AggregateMember.ValueMember Member { get; }
            private readonly GraphNode<Aggregate> _refEntry;

            internal string MemberName => Member.MemberName;
            internal string CsType => Member.Options.MemberType.GetCSharpTypeName();
            internal string TsType => Member.Options.MemberType.GetTypeScriptTypeName();
        }
    }
}
