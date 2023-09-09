using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core {

    internal class AggregateMemberNode : IGraphNode {
        public required NodeId Id { get; init; }
        internal required string Name { get; init; }
        internal required IAggregateMemberType Type { get; init; }
        internal required bool IsPrimary { get; init; }
        internal required bool IsInstanceName { get; init; }
        internal required bool Optional { get; init; }

        public override string ToString() => Id.Value;
    }

    internal static class AggregateMember {

        internal static IEnumerable<Property> GetProperties(this GraphNode<Aggregate> aggregate) {

            // スカラー値
            var dbEntityColumns = aggregate.GetColumns().ToArray();
            foreach (var member in aggregate.GetMemberNodes()) {
                yield return new SchalarProperty {
                    Owner = aggregate,
                    CorrespondingDbColumn = dbEntityColumns.Single(col => col.PropertyName == member.Item.Name),
                    CSharpTypeName = member.Item.Type.GetCSharpTypeName(),
                    TypeScriptTypename = member.Item.Type.GetTypeScriptTypeName(),
                    PropertyName = member.Item.Name,
                };
            }

            // 子要素複数
            foreach (var edge in aggregate.GetChildrenMembers()) {
                var sameRelationWithEFCoreEntity = edge.Terminal.In
                    .Single(e => e.RelationName == edge.RelationName)
                    .As<IEFCoreEntity>();
                var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity);

                yield return new ChildrenProperty {
                    Owner = aggregate,
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = $"List<{edge.Terminal.Item.ClassName}>",
                    TypeScriptTypename = $"{edge.Terminal.Item.TypeScriptTypeName}[]",
                    PropertyName = edge.RelationName,
                };
            }

            // 子要素単数
            foreach (var edge in aggregate.GetChildMembers()) {
                var sameRelationWithEFCoreEntity = edge.Terminal.In
                    .Single(e => e.RelationName == edge.RelationName)
                    .As<IEFCoreEntity>();
                var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity);

                yield return new ChildProperty {
                    Owner = aggregate,
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = edge.Terminal.Item.ClassName,
                    TypeScriptTypename = edge.Terminal.Item.TypeScriptTypeName,
                    PropertyName = edge.RelationName,
                };
            }

            // variation
            var switchColumns = aggregate
                .GetColumns()
                .Where(col => col is DbColumn.VariationGroupTypeIdentifier)
                .Cast<DbColumn.VariationGroupTypeIdentifier>()
                .ToArray();
            foreach (var group in aggregate.GetVariationGroups()) {
                var correspondingDbColumn = switchColumns.Single(col => col.PropertyName == group.GroupName);
                yield return new VariationSwitchProperty {
                    Owner = aggregate,
                    Group = group,
                    CorrespondingDbColumn = correspondingDbColumn,
                    CSharpTypeName = "string",
                    TypeScriptTypename = "string",
                    PropertyName = group.GroupName,
                };
            }

            // 参照
            foreach (var edge in aggregate.GetRefMembers()) {
                var initialDbEntity = edge.Initial.As<IEFCoreEntity>();
                var terminalDbEntity = edge.Terminal.As<IEFCoreEntity>();
                var edgeAsEfCore = terminalDbEntity.In
                    .Single(x => x.RelationName == edge.RelationName
                              && x.Initial.As<IEFCoreEntity>() == initialDbEntity)
                    .As<IEFCoreEntity>();

                yield return new RefProperty {
                    Owner = aggregate,
                    RefTarget = edge.Terminal,
                    CSharpTypeName = AggregateInstanceKeyNamePair.CLASSNAME,
                    TypeScriptTypename = AggregateInstanceKeyNamePair.TS_DEF,
                    PropertyName = edge.RelationName,
                    CorrespondingNavigationProperty = new NavigationProperty(edgeAsEfCore),
                    CorrespondingDbColumns = aggregate
                        .GetColumns()
                        .Where(col => col is DbColumn.RefTargetTablePrimaryKey refTargetPk
                                   && refTargetPk.Relation.Terminal == terminalDbEntity)
                        .ToArray(),
                };
            }
        }

        internal static IEnumerable<SchalarProperty> GetSchalarProperties(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetProperties()
                .Where(prop => prop is SchalarProperty)
                .Cast<SchalarProperty>();
        }

        internal static IEnumerable<VariationSwitchProperty> GetVariationSwitchProperties(this GraphNode<Aggregate> aggregate) {
            return aggregate
                .GetProperties()
                .Where(prop => prop is VariationSwitchProperty)
                .Cast<VariationSwitchProperty>();
        }


        internal const string BASE_CLASS_NAME = "AggregateInstanceBase";
        internal const string TO_DB_ENTITY_METHOD_NAME = "ToDbEntity";
        internal const string FROM_DB_ENTITY_METHOD_NAME = "FromDbEntity";

        internal class Property {
            internal required GraphNode<Aggregate> Owner { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string TypeScriptTypename { get; init; }
            internal required string PropertyName { get; init; }
        }
        internal class SchalarProperty : Property {
            internal required DbColumn.IDbColumn CorrespondingDbColumn { get; init; }
        }
        internal class ChildrenProperty : Property {
            internal required GraphNode<Aggregate> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class ChildProperty : Property {
            internal required GraphNode<Aggregate> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class VariationSwitchProperty : Property {
            internal required VariationGroup<Aggregate> Group { get; init; }
            internal required DbColumn.VariationGroupTypeIdentifier CorrespondingDbColumn { get; init; }

            internal IEnumerable<VariationProperty> GetGroupItems() {
                foreach (var kv in Group.VariationAggregates) {
                    var sameRelationWithEFCoreEntity = kv.Value.Terminal.In
                        .Single(e => e.RelationName == kv.Value.RelationName)
                        .As<IEFCoreEntity>();
                    var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity);

                    yield return new VariationProperty {
                        Owner = Owner,
                        Group = this,
                        Key = kv.Key,
                        ChildAggregateInstance = kv.Value.Terminal,
                        CorrespondingNavigationProperty = navigationProperty,
                        CSharpTypeName = kv.Value.Terminal.Item.ClassName,
                        TypeScriptTypename = kv.Value.Terminal.Item.TypeScriptTypeName,
                        PropertyName = kv.Value.RelationName,
                    };
                }
            }
        }
        internal class VariationProperty : Property {
            internal required VariationSwitchProperty Group { get; init; }
            internal required string Key { get; init; }
            internal required GraphNode<Aggregate> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class RefProperty : Property {
            internal required GraphNode<Aggregate> RefTarget { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
            internal required DbColumn.IDbColumn[] CorrespondingDbColumns { get; init; }
        }
    }
}
