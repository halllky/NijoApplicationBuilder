using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Searching {
    /// <summary>
    /// 検索機能
    /// </summary>
    internal partial class SearchFeature {
        internal SearchFeature(GraphNode<IEFCoreEntity> dbEntity, CodeRenderingContext ctx) {
            DbEntity = dbEntity;
            Context = ctx;

            if (dbEntity.Item is Aggregate aggregate) {
                DisplayName = aggregate.DisplayName;
                ReactPageUrl = $"/{aggregate.UniqueId}";
            } else {
                DisplayName = DbEntity.Item.ClassName;
                ReactPageUrl = $"/{DbEntity.Item.ClassName}";
            }
        }

        internal CodeRenderingContext Context { get; }
        internal GraphNode<IEFCoreEntity> DbEntity { get; }

        internal string DisplayName { get; }
        internal string PhysicalName => DbEntity.Item.ClassName;

        internal string SearchConditionClassName => $"{PhysicalName}SearchCondition";
        internal string SearchResultClassName => $"{PhysicalName}SearchResult";
        internal string DbContextSearchMethodName => $"Search{PhysicalName}";

        internal string ReactPageUrl { get; }
        internal const string REACT_FILENAME = "list.tsx";

        private const string SEARCHCONDITION_BASE_CLASS_NAME = "SearchConditionBase";
        private const string SEARCHCONDITION_PAGE_PROP_NAME = "__halapp__Page";

        private ICollection<Member>? _members;
        private ICollection<Member> Members {
            get {
                _members ??= DbEntity
                    .SelectThisAndNeighbors(entity => entity.GetChildEdges().Select(edge => edge.Terminal)
                        .Concat(entity.GetRefEdge().Where(edge => edge.IsPrimary()).Select(edge => edge.Terminal)))
                    .SelectMany(entity => entity.GetColumns())
                    .Select(col => new Member {
                        DbColumn = col,
                        IsInstanceKey = col.Options.IsKey && col.Owner == DbEntity,
                        IsInstanceName = col.Options.IsDisplayName && col.Owner == DbEntity,
                        ConditionPropName = col.Owner
                            .PathFromEntry()
                            .Select(edge => edge.RelationName)
                            .Concat(new[] { col.Options.MemberName })
                            .Join("_"),
                        SearchResultPropName = col.Owner
                            .PathFromEntry()
                            .Select(edge => edge.RelationName)
                            .Concat(new[] { col.Options.MemberName })
                            .Join("_"),
                    })
                    .ToArray();
                return _members;
            }
        }
        private IEnumerable<Member> VisibleMembers => Members.Where(m => !m.DbColumn.Options.InvisibleInGui);
        internal class Member {
            internal required string ConditionPropName { get; init; }
            internal required string SearchResultPropName { get; init; }
            internal required DbColumn DbColumn { get; init; }
            internal required bool IsInstanceKey { get; init; }
            internal required bool IsInstanceName { get; init; }
        }
    }
}
