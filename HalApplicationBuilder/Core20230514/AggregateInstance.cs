using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class AggregateInstance : IGraphNode {
        internal AggregateInstance(GraphNode<EFCoreEntity> dbEntity) {
            CorrespondingDbEntity = dbEntity;
        }

        internal GraphNode<EFCoreEntity> CorrespondingDbEntity { get; }

        internal string ClassName => $"{CorrespondingDbEntity.Item.Aggregate.Item.DisplayName.ToCSharpSafe()}Instance";

        public NodeId Id => CorrespondingDbEntity.Item.Id;

        internal const string BASE_CLASS_NAME = "AggregateInstanceBase";
        internal const string TO_DB_ENTITY_METHOD_NAME = "ToDbEntity";
        internal const string FROM_DB_ENTITY_METHOD_NAME = "FromDbEntity";

        internal class Property {
            internal required string CSharpTypeName { get; init; }
            internal required string PropertyName { get; init; }
        }
        internal class SchalarProperty : Property {
            internal required EFCoreEntity.Member CorrespondingDbColumn { get; init; }
        }
        internal class ChildAggregateProperty : Property {
            internal required GraphNode<AggregateInstance> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
            internal required bool Multiple { get; init; }
        }
        internal class RefProperty : Property {
            internal required AggregateInstance RefTarget { get; init; }
        }
    }

    internal static class AggregateInstanceExtensions {
        internal static IEnumerable<AggregateInstance.Property> GetProperties(this GraphNode<AggregateInstance> node, Config config) {
            foreach (var prop in GetSchalarProperties(node, config)) yield return prop;
            foreach (var prop in GetChildAggregateProperties(node, config)) yield return prop;
            foreach (var prop in GetRefProperties(node, config)) yield return prop;
        }

        internal static IEnumerable<AggregateInstance.SchalarProperty> GetSchalarProperties(this GraphNode<AggregateInstance> node, Config config) {
            foreach (var column in node.Item.CorrespondingDbEntity.Item.GetColumns()) {
                yield return new AggregateInstance.SchalarProperty {
                    CorrespondingDbColumn = column,
                    CSharpTypeName = column.CSharpTypeName,
                    PropertyName = column.PropertyName,
                };
            }
        }

        internal static IEnumerable<AggregateInstance.ChildAggregateProperty> GetChildAggregateProperties(this GraphNode<AggregateInstance> node, Config config) {
            foreach (var edge in node.GetChildMembers()) {
                var navigationProperty = new NavigationProperty(edge.Terminal.Item.CorrespondingDbEntity.In.Single(e => e == edge), config);
                yield return new AggregateInstance.ChildAggregateProperty {
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = edge.Terminal.Item.ClassName,
                    PropertyName = edge.RelationName,
                    Multiple = false,
                };
            }
            foreach (var edge in node.GetVariationMembers()) {
                var navigationProperty = new NavigationProperty(edge.Terminal.Item.CorrespondingDbEntity.In.Single(e => e == edge), config);
                yield return new AggregateInstance.ChildAggregateProperty {
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = edge.Terminal.Item.ClassName,
                    PropertyName = edge.RelationName,
                    Multiple = false,
                };
            }
            foreach (var edge in node.GetChildrenMembers()) {
                var navigationProperty = new NavigationProperty(edge.Terminal.Item.CorrespondingDbEntity.In.Single(e => e == edge), config);
                yield return new AggregateInstance.ChildAggregateProperty {
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = $"List<{edge.Terminal.Item.ClassName}>",
                    PropertyName = edge.RelationName,
                    Multiple = true,
                };
            }
        }

        internal static IEnumerable<AggregateInstance.RefProperty> GetRefProperties(this GraphNode<AggregateInstance> node, Config config) {
            foreach (var edge in node.GetRefMembers()) {
                var refTarget = new AggregateInstance(edge.Terminal.Item.CorrespondingDbEntity);
                yield return new AggregateInstance.RefProperty {
                    RefTarget = refTarget,
                    CSharpTypeName = CodeRendering20230514.Presentation.AggregateInstanceKeyNamePair.CLASSNAME,
                    PropertyName = edge.RelationName,
                };
            }
        }
    }
}
