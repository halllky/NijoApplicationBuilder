using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 検索条件クラス
    /// </summary>
    internal class SearchCondition {
        internal SearchCondition(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        protected readonly GraphNode<Aggregate> _aggregate;

        internal virtual string CsClassName => $"{_aggregate.Item.PhysicalName}SearchCondition";
        internal virtual string TsTypeName => $"{_aggregate.Item.PhysicalName}SearchCondition";

        /// <summary>
        /// この集約自身がもつ検索条件を列挙します。
        /// </summary>
        private IEnumerable<SearchConditionMember> GetOwnMembers() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(vm => vm.DeclaringAggregate == _aggregate)
                .Select(vm => new SearchConditionMember(vm));
        }
        /// <summary>
        /// 直近の子を列挙します。
        /// </summary>
        private IEnumerable<DescendantSearchCondition> GetChildMembers() {
            return _aggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(rm => rm is AggregateMember.Child
                          || rm is AggregateMember.Children
                          || rm is AggregateMember.VariationItem
                          || rm is AggregateMember.Ref)
                .Select(rm => new DescendantSearchCondition(rm));
        }

        internal string RenderCSharpDeclaringRecursively(CodeRenderingContext context) {
            var descendants = _aggregate
                .EnumerateDescendants()
                .Select(agg => new SearchCondition(agg));
            var refToConditions = _aggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers())
                .OfType<AggregateMember.Ref>()
                .Select(@ref => new DescendantSearchCondition(@ref));

            return $$"""
                #region 検索条件クラス（{{_aggregate.Item.DisplayName}}）
                {{RenderCSharpDeclaring(context)}}
                {{descendants.SelectTextTemplate(sc => $$"""
                {{sc.RenderCSharpDeclaring(context)}}
                """)}}
                {{refToConditions.SelectTextTemplate(sc => $$"""
                {{sc.RenderCSharpDeclaring(context)}}
                """)}}
                #endregion 検索条件クラス（{{_aggregate.Item.DisplayName}}）

                """;
        }
        protected virtual string RenderCSharpDeclaring(CodeRenderingContext context) {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}の一覧検索条件
                /// </summary>
                public partial class {{CsClassName}} {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                    public virtual {{m.CsTypeName}}? {{m.MemberName}} { get; set; }
                """)}}
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                    public virtual {{m.CsClassName}} {{m.MemberName}} { get; set; } = new();
                """)}}
                }
                """;
        }

        internal string RenderTypeScriptDeclaringRecursively(CodeRenderingContext context) {
            var descendants = _aggregate
                .EnumerateDescendants()
                .Select(agg => new SearchCondition(agg));
            var refToConditions = _aggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers())
                .OfType<AggregateMember.Ref>()
                .Select(@ref => new DescendantSearchCondition(@ref));

            return $$"""
                {{RenderTypeScriptDeclaring(context)}}
                {{descendants.SelectTextTemplate(sc => $$"""
                {{sc.RenderTypeScriptDeclaring(context)}}
                """)}}
                {{refToConditions.SelectTextTemplate(sc => $$"""
                {{sc.RenderTypeScriptDeclaring(context)}}
                """)}}

                """;
        }
        protected virtual string RenderTypeScriptDeclaring(CodeRenderingContext context) {
            return $$"""
                /** {{_aggregate.Item.DisplayName}}の一覧検索条件 */
                export type {{TsTypeName}} = {
                {{GetOwnMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}?: {{m.TsTypeName}}
                """)}}
                {{GetChildMembers().SelectTextTemplate(m => $$"""
                  {{m.MemberName}}: {{m.TsTypeName}}
                """)}}
                }
                """;
        }
    }


    /// <summary>
    /// ルート集約ではない検索条件クラス
    /// </summary>
    internal class DescendantSearchCondition : SearchCondition {
        internal DescendantSearchCondition(AggregateMember.RelationMember relationMember) : base(relationMember.MemberAggregate) {
            _relationMember = relationMember;
        }

        private readonly AggregateMember.RelationMember _relationMember;
        internal string MemberName => _relationMember.MemberName;

        // Refの場合は複数のReadModelから1つのWriteModelへの参照がある可能性があり名前衝突するかもしれないので"RefFrom～"をつける
        internal override string CsClassName => _relationMember.MemberAggregate.IsOutOfEntryTree()
            ? $"{base.CsClassName}_RefFrom{_aggregate.GetEntry().As<Aggregate>().Item.PhysicalName}の{_relationMember.MemberAggregate.GetRefEdge().RelationName}"
            : base.CsClassName;
        internal override string TsTypeName => _relationMember.MemberAggregate.IsOutOfEntryTree()
            ? $"{base.TsTypeName}_RefFrom{_aggregate.GetEntry().As<Aggregate>().Item.PhysicalName}の{_relationMember.MemberAggregate.GetRefEdge().RelationName}"
            : base.TsTypeName;
    }


    internal class SearchConditionMember {
        internal SearchConditionMember(AggregateMember.ValueMember vm) {
            _vm = vm;
        }
        private readonly AggregateMember.ValueMember _vm;

        internal string MemberName => _vm.MemberName;
        internal string CsTypeName => _vm.Options.MemberType.GetSearchConditionCSharpType();
        internal string TsTypeName => _vm.Options.MemberType.GetSearchConditionTypeScriptType();
    }
}
