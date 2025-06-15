using Microsoft.AspNetCore.Components.Rendering;
using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.QueryModelModules {
    /// <summary>
    /// <see cref="SearchCondition.Entry"/> のデータ構造と対応するメッセージの入れ物
    /// </summary>
    internal class SearchConditionMessageContainer : MessageContainer {
        public SearchConditionMessageContainer(AggregateBase aggregate) : base(aggregate) {
            _filter = new SearchCondition.Filter(aggregate);
        }

        private readonly SearchCondition.Filter _filter;

        internal override string CsClassName => $"{_aggregate.PhysicalName}SearchConditionMessages";
        internal override string TsTypeName => $"{_aggregate.PhysicalName}SearchConditionMessages";

        protected override IEnumerable<IMessageContainerMember> GetMembers() {
            return _filter
                .GetOwnMembers()
                .Select(m => new ContainerMemberImpl {
                    PhysicalName = m.GetPropertyName(E_CsTs.CSharp),
                    DisplayName = m.DisplayName,
                    NestedObject = m is IRelationalMember rm
                        ? new SearchConditionMessageContainer(rm.MemberAggregate)
                        : null,
                    CsType = null,
                });
        }

        private class ContainerMemberImpl : IMessageContainerMember {
            public required string PhysicalName { get; init; }
            public required string DisplayName { get; init; }
            public required MessageContainer? NestedObject { get; init; }
            public required string? CsType { get; init; }
        }

        internal static string RenderCSharpRecursively(RootAggregate rootAggregate) {
            var tree = rootAggregate
                .EnumerateThisAndDescendants()
                .Select(agg => new SearchConditionMessageContainer(agg))
                .ToArray();

            return $$"""
                #region 検索条件クラスのデータ構造と対応するメッセージの入れ物クラス
                {{tree.SelectTextTemplate(container => $$"""
                {{container.RenderCSharp()}}
                """)}}
                #endregion 検索条件クラスのデータ構造と対応するメッセージの入れ物クラス
                """;
        }
    }
}
