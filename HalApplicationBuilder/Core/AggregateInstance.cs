using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core
{
    internal class AggregateInstance : IGraphNode {
        internal AggregateInstance(Aggregate aggregate) {
            // TODO aggregateIdにコロンが含まれるケースの対策
            Id = new NodeId($"INSTANCE::{aggregate.Id}");
            _aggregate = aggregate;
        }

        private readonly Aggregate _aggregate;

        internal string ClassName => $"{_aggregate.DisplayName.ToCSharpSafe()}Instance";
        internal string TypeScriptTypeName => _aggregate.DisplayName.ToCSharpSafe();

        public NodeId Id { get; }

        internal const string BASE_CLASS_NAME = "AggregateInstanceBase";
        internal const string TO_DB_ENTITY_METHOD_NAME = "ToDbEntity";
        internal const string FROM_DB_ENTITY_METHOD_NAME = "FromDbEntity";

        internal class Property {
            internal required string CSharpTypeName { get; init; }
            internal required string TypeScriptTypename { get; init; }
            internal required string PropertyName { get; init; }
        }
        internal class SchalarProperty : Property {
            internal required EFCoreEntity.Member CorrespondingDbColumn { get; init; }
        }
        internal class ChildrenProperty : Property {
            internal required GraphNode<AggregateInstance> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class ChildProperty : Property {
            internal required GraphNode<AggregateInstance> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class VariationProperty : Property {
            internal required VariationGroup<AggregateInstance> Group { get; init; }
            internal required string Key { get; init; }
            internal required GraphNode<AggregateInstance> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class RefProperty : Property {
            internal required GraphNode<AggregateInstance> RefTarget { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
            internal required EFCoreEntity.Member[] CorrespondingDbColumns { get; init; }
        }
    }

    internal static class AggregateInstanceExtensions {
        internal static IEnumerable<AggregateInstance.Property> GetProperties(this GraphNode<AggregateInstance> node, Config config) {
            foreach (var prop in GetSchalarProperties(node, config)) yield return prop;
            foreach (var prop in GetChildrenProperties(node, config)) yield return prop;
            foreach (var prop in GetChildProperties(node, config)) yield return prop;
            foreach (var prop in GetVariationProperties(node, config)) yield return prop;
            foreach (var prop in GetRefProperties(node, config)) yield return prop;
        }

        internal static IEnumerable<AggregateInstance.SchalarProperty> GetSchalarProperties(this GraphNode<AggregateInstance> instance, Config config) {
            var dbEntityColumns = instance.GetDbEntity().GetColumns().ToArray();
            foreach (var member in instance.GetCorrespondingAggregate().Item.Members) {
                yield return new AggregateInstance.SchalarProperty {
                    CorrespondingDbColumn = dbEntityColumns.Single(col => col.PropertyName == member.Name),
                    CSharpTypeName = member.Type.GetCSharpTypeName(),
                    TypeScriptTypename = member.Type.GetTypeScriptTypeName(),
                    PropertyName = member.Name,
                };
            }

            // variationのスイッチ
            foreach (var group in instance.GetVariationGroups()) {
                yield return new AggregateInstance.SchalarProperty {
                    CorrespondingDbColumn = dbEntityColumns.Single(col => col.PropertyName == group.GroupName),
                    CSharpTypeName = "string",
                    TypeScriptTypename = "string",
                    PropertyName = group.GroupName,
                };
            }
        }

        internal static IEnumerable<AggregateInstance.ChildrenProperty> GetChildrenProperties(this GraphNode<AggregateInstance> node, Config config) {
            var initial = node.GetDbEntity().Item.Id;
            foreach (var edge in node.GetChildrenMembers()) {
                var navigationProperty = new NavigationProperty(edge.Terminal.GetDbEntity().In.Single(e => e.Initial.Item.Id == initial), config);
                yield return new AggregateInstance.ChildrenProperty {
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = $"List<{edge.Terminal.Item.ClassName}>",
                    TypeScriptTypename = $"{edge.Terminal.Item.TypeScriptTypeName}[]",
                    PropertyName = edge.RelationName,
                };
            }
        }
        internal static IEnumerable<AggregateInstance.ChildProperty> GetChildProperties(this GraphNode<AggregateInstance> node, Config config) {
            var initial = node.GetDbEntity().Item.Id;
            foreach (var edge in node.GetChildMembers()) {
                var navigationProperty = new NavigationProperty(edge.Terminal.GetDbEntity().In.Single(e => e.Initial.Item.Id == initial), config);
                yield return new AggregateInstance.ChildProperty {
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = edge.Terminal.Item.ClassName,
                    TypeScriptTypename = edge.Terminal.Item.TypeScriptTypeName,
                    PropertyName = edge.RelationName,
                };
            }
        }
        internal static IEnumerable<AggregateInstance.VariationProperty> GetVariationProperties(this GraphNode<AggregateInstance> node, Config config) {
            var initial = node.GetDbEntity().Item.Id;
            foreach (var group in node.GetVariationGroups()) {
                foreach (var kv in group.VariationAggregates) {
                    var navigationProperty = new NavigationProperty(kv.Value.Terminal.GetDbEntity().In.Single(e => e.Initial.Item.Id == initial), config);
                    yield return new AggregateInstance.VariationProperty {
                        Group = group,
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
        internal static IEnumerable<AggregateInstance.RefProperty> GetRefProperties(this GraphNode<AggregateInstance> node, Config config) {
            foreach (var edge in node.GetRefMembers()) {
                var initialDbEntity = edge.Initial.GetDbEntity();
                var terminalDbEntity = edge.Terminal.GetDbEntity();
                var edgeAsEfCore = terminalDbEntity.In.Single(x => x.RelationName == edge.RelationName
                                                                && x.Initial.As<EFCoreEntity>() == initialDbEntity);
                yield return new AggregateInstance.RefProperty {
                    RefTarget = edge.Terminal,
                    CSharpTypeName = AggregateInstanceKeyNamePair.CLASSNAME,
                    TypeScriptTypename = AggregateInstanceKeyNamePair.TS_DEF,
                    PropertyName = edge.RelationName,
                    CorrespondingNavigationProperty = new NavigationProperty(edgeAsEfCore, config),
                    CorrespondingDbColumns = node
                        .GetDbEntity()
                        .GetColumns()
                        .Where(col => col.CorrespondingRefTargetColumn?.Owner == terminalDbEntity)
                        .ToArray(),
                };
            }
        }
    }
}
