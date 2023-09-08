using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal interface IAggregateInstance : IGraphNode {
        string ClassName { get; }
        string TypeScriptTypeName { get; }


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
            internal required IEFCoreEntity.IMember CorrespondingDbColumn { get; init; }
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
            internal required IEFCoreEntity.VariationGroupTypeIdentifier CorrespondingDbColumn { get; init; }

            internal required Config Config { get; init; }
            internal IEnumerable<VariationProperty> GetGroupItems() {
                foreach (var kv in Group.VariationAggregates) {
                    var sameRelationWithEFCoreEntity = kv.Value.Terminal.In
                        .Single(e => e.RelationName == kv.Value.RelationName)
                        .As<IEFCoreEntity>();
                    var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity, Config);

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
            internal required IEFCoreEntity.IMember[] CorrespondingDbColumns { get; init; }
        }

    }
}
