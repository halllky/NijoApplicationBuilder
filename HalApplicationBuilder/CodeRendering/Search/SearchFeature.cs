using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Search {
    /// <summary>
    /// 検索機能
    /// </summary>
    internal partial class SearchFeature {

        internal required CodeRenderingContext Context { get; init; }
        internal required GraphNode<EFCoreEntity> DbEntity { get; init; }

        internal string? CreateLinkUrl { get; init; }
        internal Func<DetailLinkUrlArgs, string>? DetailLinkUrl { get; init; }
        internal class DetailLinkUrlArgs {
            internal required string EncodedInstanceKey { get; init; }
        }

        internal string DisplayName => DbEntity.GetCorrespondingAggregate().Item.DisplayName;
        internal string PhysicalName => DbEntity.Item.ClassName;

        internal string SearchConditionClassName => $"{PhysicalName}SearchCondition";
        internal string SearchResultClassName => $"{PhysicalName}SearchResult";
        internal string DbContextSearchMethodName => $"Search{PhysicalName}";

        private ICollection<Member>? _members;
        private ICollection<Member> Members {
            get {
                _members ??= DbEntity
                    .EnumerateThisAndDescendants()
                    .Where(entity => entity == DbEntity
                                    || entity.IsChildMember()
                                    || entity.Source?.IsRef() == true)
                    .SelectMany(entity => entity.GetColumns())
                    .Select(col => new Member {
                        CorrespondingDbColumn = col,
                        IsInstanceKey = col.IsPrimary && col.Owner == DbEntity,
                        IsInstanceName = col.IsInstanceName && col.Owner == DbEntity,
                        Type = col.MemberType,
                        ConditionPropName = col.Owner
                            .PathFromEntry()
                            .Select(edge => edge.RelationName)
                            .Concat(new[] { col.PropertyName })
                            .Join("_"),
                        SearchResultPropName = col.Owner
                            .PathFromEntry()
                            .Select(edge => edge.RelationName)
                            .Concat(new[] { col.PropertyName })
                            .Join("_"),
                    })
                    .ToArray();
                return _members;
            }
        }
        internal class Member {
            internal required string ConditionPropName { get; init; }
            internal required string SearchResultPropName { get; init; }
            internal required EFCoreEntity.Member CorrespondingDbColumn { get; init; }
            internal required IAggregateMemberType Type { get; init; }
            internal required bool IsInstanceKey { get; init; }
            internal required bool IsInstanceName { get; init; }
        }
    }
}
