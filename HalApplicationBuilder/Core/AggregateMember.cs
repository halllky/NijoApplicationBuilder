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

        internal static IEnumerable<AggregateMemberBase> GetProperties(this GraphNode<Aggregate> aggregate) {
            foreach (var member in aggregate.GetMemberNodes()) {
                yield return new SchalarProperty(member);
            }
            foreach (var edge in aggregate.GetChildrenMembers()) {
                yield return new ChildrenProperty(edge);
            }
            foreach (var edge in aggregate.GetChildMembers()) {
                yield return new ChildProperty(edge);
            }
            foreach (var group in aggregate.GetVariationGroups()) {
                yield return new VariationSwitchProperty(group);
            }
            foreach (var edge in aggregate.GetRefMembers()) {
                yield return new RefProperty(edge);
            }
        }
        internal static IEnumerable<AggregateMemberBase> GetKeyProperties(this GraphNode<Aggregate> aggregate) {
            foreach (var prop in aggregate.GetProperties()) {
                if (prop is SchalarProperty schalarProperty && schalarProperty.IsPrimary)
                    yield return schalarProperty;
                else if (prop is RefProperty refProp && refProp.IsPrimary)
                    yield return refProp;
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

        internal abstract class AggregateMemberBase : ValueObject {
            internal abstract GraphNode<Aggregate> Owner { get; }
            internal abstract string PropertyName { get; }
            internal abstract string CSharpTypeName { get; }
            internal abstract string TypeScriptTypename { get; }

            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return PropertyName;
            }
        }

        internal class SchalarProperty : AggregateMemberBase {
            internal SchalarProperty(GraphNode<AggregateMemberNode> aggregateMemberNode) {
                _node = aggregateMemberNode;
            }
            private readonly GraphNode<AggregateMemberNode> _node;

            internal override GraphNode<Aggregate> Owner => _node.Source!.Initial.As<Aggregate>();
            internal override string PropertyName => _node.Item.Name;
            internal override string CSharpTypeName => _node.Item.Type.GetCSharpTypeName();
            internal override string TypeScriptTypename => _node.Item.Type.GetTypeScriptTypeName();

            internal bool IsPrimary => _node.Item.IsPrimary;
            internal bool IsInstanceName => _node.Item.IsInstanceName;
            internal bool RequiredAtDB => _node.Item.IsPrimary || !_node.Item.Optional;
            internal IAggregateMemberType MemberType => _node.Item.Type;
            internal DbColumn.DbColumnBase CorrespondingDbColumn => new DbColumn.SchalarColumnDefniedInAggregate(this);
        }

        internal class ChildrenProperty : AggregateMemberBase {
            internal ChildrenProperty(GraphEdge<Aggregate> edge) {
                _edge = edge;
            }
            private readonly GraphEdge<Aggregate> _edge;

            internal override GraphNode<Aggregate> Owner => _edge.Initial;
            internal override string PropertyName => _edge.RelationName;
            internal override string CSharpTypeName => $"List<{_edge.Terminal.Item.ClassName}>";
            internal override string TypeScriptTypename => $"{_edge.Terminal.Item.TypeScriptTypeName}[]";
            internal GraphNode<Aggregate> ChildAggregateInstance => _edge.Terminal;
            internal NavigationProperty CorrespondingNavigationProperty => new NavigationProperty(_edge.As<IEFCoreEntity>());
        }

        internal class ChildProperty : AggregateMemberBase {
            internal ChildProperty(GraphEdge<Aggregate> edge) {
                _edge = edge;
            }
            private readonly GraphEdge<Aggregate> _edge;

            internal override GraphNode<Aggregate> Owner => _edge.Initial;
            internal override string PropertyName => _edge.RelationName;
            internal override string CSharpTypeName => _edge.Terminal.Item.ClassName;
            internal override string TypeScriptTypename => _edge.Terminal.Item.TypeScriptTypeName;
            internal GraphNode<Aggregate> ChildAggregateInstance => _edge.Terminal;
            internal NavigationProperty CorrespondingNavigationProperty => new NavigationProperty(_edge.As<IEFCoreEntity>());
        }

        internal class VariationSwitchProperty : AggregateMemberBase {
            internal VariationSwitchProperty(VariationGroup<Aggregate> group) {
                _group = group;
            }
            private readonly VariationGroup<Aggregate> _group;

            internal override GraphNode<Aggregate> Owner => _group.Owner;
            internal override string PropertyName => _group.GroupName;
            internal override string CSharpTypeName => "string";
            internal override string TypeScriptTypename => "string";
            internal DbColumn.DbColumnBase CorrespondingDbColumn => new DbColumn.VariationGroupTypeIdentifier(this);

            internal IEnumerable<VariationProperty> GetGroupItems() {
                foreach (var kv in _group.VariationAggregates) {
                    yield return new VariationProperty(this, kv.Key, kv.Value);
                }
            }
        }

        internal class VariationProperty : AggregateMemberBase {
            internal VariationProperty(VariationSwitchProperty group, string key, GraphEdge<Aggregate> edge) {
                Group = group;
                Key = key;
                _edge = edge;
            }
            private readonly GraphEdge<Aggregate> _edge;

            internal VariationSwitchProperty Group { get; }
            internal string Key { get; }

            internal override GraphNode<Aggregate> Owner => _edge.Initial;
            internal override string PropertyName => _edge.RelationName;
            internal override string CSharpTypeName => _edge.Terminal.Item.ClassName;
            internal override string TypeScriptTypename => _edge.Terminal.Item.TypeScriptTypeName;
            internal GraphNode<Aggregate> ChildAggregateInstance => _edge.Terminal;
            internal NavigationProperty CorrespondingNavigationProperty => new NavigationProperty(_edge.As<IEFCoreEntity>());
        }

        internal class RefProperty : AggregateMemberBase {
            internal RefProperty(GraphEdge<Aggregate> edge) {
                Relation = edge;
            }
            internal GraphEdge<Aggregate> Relation { get; }

            internal override GraphNode<Aggregate> Owner => Relation.Initial;
            internal override string PropertyName => Relation.RelationName;
            internal override string CSharpTypeName => AggregateInstanceKeyNamePair.CLASSNAME;
            internal override string TypeScriptTypename => AggregateInstanceKeyNamePair.TS_DEF;
            internal bool IsPrimary => Relation.IsPrimary();
            internal bool IsInstanceName => Relation.IsInstanceName();
            internal bool RequiredAtDB => Relation.IsPrimary() || Relation.IsRequired();
            internal GraphNode<Aggregate> RefTarget => Relation.Terminal;
            internal NavigationProperty CorrespondingNavigationProperty => new NavigationProperty(Relation.As<IEFCoreEntity>());
            internal IEnumerable<DbColumn.DbColumnBase> CorrespondingDbColumns => Relation.Initial
                .GetColumns()
                .Where(col => col is DbColumn.RefTargetTablePrimaryKey refTargetPk
                           && refTargetPk.Relation.Terminal == Relation.Terminal);
        }
    }
}
