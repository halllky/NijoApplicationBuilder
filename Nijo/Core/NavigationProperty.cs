using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    /// <summary>
    /// ナビゲーションプロパティ
    /// </summary>
    internal class NavigationProperty : ValueObject {
        internal NavigationProperty(GraphEdge<Aggregate> relation) {
            _graphEdge = relation;

            var parent = relation.Terminal.GetParent();
            if (relation.Terminal.IsChildMember() && relation == parent) {
                Principal = new PrincipalOrRelevant(relation, relation.Initial);
                Relevant = new PrincipalOrRelevant(relation, relation.Terminal);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (relation.Terminal.IsVariationMember() && relation == parent) {
                Principal = new PrincipalOrRelevant(relation, relation.Initial);
                Relevant = new PrincipalOrRelevant(relation, relation.Terminal);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (relation.Terminal.IsChildrenMember() && relation == parent) {
                Principal = new PrincipalOrRelevant(relation, relation.Initial);
                Relevant = new PrincipalOrRelevant(relation, relation.Terminal);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade;

            } else if (relation.IsRef()) {
                Principal = new PrincipalOrRelevant(relation, relation.Terminal);
                Relevant = new PrincipalOrRelevant(relation, relation.Initial);
                OnPrincipalDeleted = Microsoft.EntityFrameworkCore.DeleteBehavior.NoAction;

            } else {
                throw new ArgumentException("Graph edge can not be converted to navigation property.", nameof(relation));
            }
        }
        private readonly GraphEdge _graphEdge;

        /// <summary>
        /// 主たるエンティティ側のナビゲーションプロパティ
        /// </summary>
        internal PrincipalOrRelevant Principal { get; }
        /// <summary>
        /// 従たるエンティティ側のナビゲーションプロパティ
        /// </summary>
        internal PrincipalOrRelevant Relevant { get; }

        internal class PrincipalOrRelevant : ValueObject {
            internal PrincipalOrRelevant(GraphEdge<Aggregate> relation, GraphNode<Aggregate> initialOrTerminal) {
                _relation = relation;
                Owner = initialOrTerminal;
                Opposite = initialOrTerminal == relation.Initial
                    ? relation.Terminal
                    : relation.Initial;
            }

            private readonly GraphEdge<Aggregate> _relation;
            internal GraphNode<Aggregate> Owner { get; }
            internal GraphNode<Aggregate> Opposite { get; }

            internal string PropertyName {
                get {
                    if (_relation.Terminal == Owner
                        && (_relation.Terminal.IsChildMember()
                        || _relation.Terminal.IsChildrenMember()
                        || _relation.Terminal.IsVariationMember())
                        && _relation.Terminal.GetParent() == _relation) {
                        return AggregateMember.PARENT_PROPNAME;

                    } else if (_relation.Terminal == Owner
                            && _relation.IsRef()) {
                        return $"RefferedBy_{_relation.Initial.Item.EFCoreEntityClassName}_{_relation.RelationName}";

                    } else {
                        return _relation.RelationName;
                    }
                }
            }
            internal bool OppositeIsMany {
                get {
                    if (_relation.Terminal == Owner && _relation.IsRef()) {

                        if (!_relation.IsPrimary()) {
                            return true;
                        }

                        // このRef以外の主キーがある（=複合キーである）ならば1対多
                        var keyGroups = Opposite
                            .GetKeys()
                            .OfType<AggregateMember.ValueMember>()
                            .GroupBy(vm => vm.Inherits?.Relation);
                        if (keyGroups.Any(group => group.Key != _relation)) {
                            return true;
                        }

                        return false;

                    } else if (_relation.Terminal.IsChildrenMember()
                            && _relation.Terminal.GetParent() == _relation
                            && _relation.Initial == Owner) {
                        return true;

                    } else {
                        return false;
                    }
                }
            }

            internal string CSharpTypeName => OppositeIsMany
                ? $"ICollection<{Opposite.Item.EFCoreEntityClassName}>"
                : $"{Opposite.Item.EFCoreEntityClassName}?";
            internal string? Initializer => OppositeIsMany
                ? $" = new HashSet<{Opposite.Item.EFCoreEntityClassName}>();"
                : null;

            internal IEnumerable<AggregateMember.ValueMember> GetForeignKeys() {
                var parent = _relation.Terminal.GetParent();
                if (_relation.Terminal == Owner
                    && parent == _relation
                    && (_relation.Terminal.IsChildMember()
                    || _relation.Terminal.IsChildrenMember()
                    || _relation.Terminal.IsVariationMember())) {

                    return _relation.Terminal
                        .GetKeys()
                        .OfType<AggregateMember.ValueMember>()
                        .Where(key => key.Inherits?.Relation == parent);

                } else if (_relation.Initial == Owner
                        && _relation.IsRef()) {

                    return _relation.Initial
                        .GetMembers()
                        .OfType<AggregateMember.Ref>()
                        .Where(refMember => refMember.Relation == _relation)
                        .SelectMany(refMember => refMember.GetForeignKeys());

                } else {
                    return Enumerable.Empty<AggregateMember.ValueMember>();
                }
            }

            internal IEnumerable<string> GetFullPath(GraphNode<Aggregate>? since = null) {
                var skip = since != null;
                foreach (var edge in Owner.PathFromEntry()) {
                    if (skip && edge.Source?.As<Aggregate>() == since) skip = false;
                    if (skip) continue;
                    yield return edge.RelationName;
                }
                yield return PropertyName;
            }
            protected override IEnumerable<object?> ValueObjectIdentifiers() {
                yield return Owner;
                yield return PropertyName;
            }
        }

        internal Microsoft.EntityFrameworkCore.DeleteBehavior OnPrincipalDeleted { get; init; }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return _graphEdge;
        }
    }
}
