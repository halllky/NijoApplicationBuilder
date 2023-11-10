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

        private IEnumerable<Member> GetMembers() {
            var descendantColumns = DbEntity
                .EnumerateThisAndDescendants()
                .Where(x => x.EnumerateAncestorsAndThis()
                    .All(ancestor => !ancestor.IsChildrenMember()
                                  && !ancestor.IsVariationMember()))
                .SelectMany(entity => entity.GetColumns());

            // 参照先のキーはdescendantColumnsの中に入っているが、名前は入っていないので、別途取得の必要あり
            IEnumerable<DbColumn> refTargetColumns;
            if (DbEntity.Item is Aggregate) {
                var pkRefTargets = DbEntity
                    .As<Aggregate>()
                    .EnumerateThisAndDescendants()
                    .SelectMany(entity => entity.GetRefEdge());
                refTargetColumns = pkRefTargets
                    .SelectMany(agg => agg.Terminal.GetNames())
                    .Where(member => !member.IsKey)
                    .Select(member => member.GetDbColumn());
            } else {
                refTargetColumns = Enumerable.Empty<DbColumn>();
            }
            var targetColumns = descendantColumns.Concat(refTargetColumns);

            var members = targetColumns.Select(col => new Member {
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
            });
            return members;
        }
        private IEnumerable<Member> VisibleMembers => GetMembers().Where(m => !m.DbColumn.Options.InvisibleInGui);
        internal class Member {
            internal required string ConditionPropName { get; init; }
            internal required string SearchResultPropName { get; init; }
            internal required DbColumn DbColumn { get; init; }
            internal required bool IsInstanceKey { get; init; }
            internal required bool IsInstanceName { get; init; }

            public override string ToString() {
                return DbColumn.ToString();
            }
        }
    }
}
