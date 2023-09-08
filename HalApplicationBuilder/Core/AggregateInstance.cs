using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static HalApplicationBuilder.Core.IAggregateInstance;

namespace HalApplicationBuilder.Core {
    internal static class AggregateInstanceExtensions {
        internal static IEnumerable<IAggregateInstance.Property> GetProperties(this GraphNode<Aggregate> aggregate, Config config) {
            foreach (var prop in GetSchalarProperties(aggregate)) yield return prop;
            foreach (var prop in GetChildrenProperties(aggregate, config)) yield return prop;
            foreach (var prop in GetChildProperties(aggregate, config)) yield return prop;
            foreach (var prop in GetVariationSwitchProperties(aggregate, config)) yield return prop;
            foreach (var prop in GetVariationProperties(aggregate, config)) yield return prop;
            foreach (var prop in GetRefProperties(aggregate, config)) yield return prop;
        }

        internal static IEnumerable<IAggregateInstance.SchalarProperty> GetSchalarProperties(this GraphNode<Aggregate> aggregate) {
            var dbEntityColumns = aggregate.GetColumns().ToArray();
            foreach (var member in aggregate.GetSchalarMembers()) {
                yield return new IAggregateInstance.SchalarProperty {
                    Owner = aggregate,
                    CorrespondingDbColumn = dbEntityColumns.Single(col => col.PropertyName == member.Item.Name),
                    CSharpTypeName = member.Item.Type.GetCSharpTypeName(),
                    TypeScriptTypename = member.Item.Type.GetTypeScriptTypeName(),
                    PropertyName = member.Item.Name,
                };
            }
        }

        internal static IEnumerable<IAggregateInstance.ChildrenProperty> GetChildrenProperties(this GraphNode<Aggregate> instance, Config config) {
            var initial = instance.Item.Id;
            foreach (var edge in instance.GetChildrenMembers()) {
                var sameRelationWithEFCoreEntity = edge.Terminal.In
                    .Single(e => e.RelationName == edge.RelationName)
                    .As<IEFCoreEntity>();
                var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity, config);

                yield return new IAggregateInstance.ChildrenProperty {
                    Owner = instance,
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = $"List<{edge.Terminal.Item.ClassName}>",
                    TypeScriptTypename = $"{edge.Terminal.Item.TypeScriptTypeName}[]",
                    PropertyName = edge.RelationName,
                };
            }
        }
        internal static IEnumerable<IAggregateInstance.ChildProperty> GetChildProperties(this GraphNode<Aggregate> instance, Config config) {
            var initial = instance.Item.Id;
            foreach (var edge in instance.GetChildMembers()) {
                var sameRelationWithEFCoreEntity = edge.Terminal.In
                    .Single(e => e.RelationName == edge.RelationName)
                    .As<IEFCoreEntity>();
                var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity, config);

                yield return new IAggregateInstance.ChildProperty {
                    Owner = instance,
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = edge.Terminal.Item.ClassName,
                    TypeScriptTypename = edge.Terminal.Item.TypeScriptTypeName,
                    PropertyName = edge.RelationName,
                };
            }
        }

        internal static IEnumerable<IAggregateInstance.VariationSwitchProperty> GetVariationSwitchProperties(this GraphNode<Aggregate> instance, Config config) {
            var dbEntityColumns = instance
                .GetColumns()
                .Where(col => col is IEFCoreEntity.VariationGroupTypeIdentifier)
                .Cast<IEFCoreEntity.VariationGroupTypeIdentifier>()
                .ToArray();

            foreach (var group in instance.GetVariationGroups()) {
                var correspondingDbColumn = dbEntityColumns.Single(col => col.PropertyName == group.GroupName);
                yield return new IAggregateInstance.VariationSwitchProperty {
                    Owner = instance,
                    Group = group,
                    CorrespondingDbColumn = correspondingDbColumn,
                    CSharpTypeName = "string",
                    TypeScriptTypename = "string",
                    PropertyName = group.GroupName,
                    Config = config,
                };
            }
        }
        internal static IEnumerable<IAggregateInstance.VariationProperty> GetVariationProperties(this GraphNode<Aggregate> node, Config config) {
            return node
                .GetVariationSwitchProperties(config)
                .SelectMany(group => group.GetGroupItems());
        }
        internal static IEnumerable<IAggregateInstance.RefProperty> GetRefProperties(this GraphNode<Aggregate> instance, Config config) {
            foreach (var edge in instance.GetRefMembers()) {
                var initialDbEntity = edge.Initial.As<IEFCoreEntity>();
                var terminalDbEntity = edge.Terminal.As<IEFCoreEntity>();
                var edgeAsEfCore = terminalDbEntity.In
                    .Single(x => x.RelationName == edge.RelationName
                              && x.Initial.As<IEFCoreEntity>() == initialDbEntity)
                    .As<IEFCoreEntity>();

                yield return new IAggregateInstance.RefProperty {
                    Owner = instance,
                    RefTarget = edge.Terminal,
                    CSharpTypeName = AggregateInstanceKeyNamePair.CLASSNAME,
                    TypeScriptTypename = AggregateInstanceKeyNamePair.TS_DEF,
                    PropertyName = edge.RelationName,
                    CorrespondingNavigationProperty = new NavigationProperty(edgeAsEfCore, config),
                    CorrespondingDbColumns = instance
                        .GetColumns()
                        .Where(col => col is IEFCoreEntity.RefTargetTablePrimaryKey refTargetPk
                                   && refTargetPk.Relation.Terminal == terminalDbEntity)
                        .ToArray(),
                };
            }
        }
    }
}
