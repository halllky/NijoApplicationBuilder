using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Core {
    public class Aggregate {

        internal Aggregate() { }

        internal ApplicationSchema Schema { get; init; }
        internal Type UnderlyingType { get; init; }

        public string Name => UnderlyingType.Name;

        public IAggregateMember Parent { get; init; }

        private List<IAggregateMember> _members;
        public IReadOnlyList<IAggregateMember> Members {
            get {
                if (_members == null) {
                    _members = Schema.MemberFactory.CreateMembers(this).ToList();
                }
                return _members;
            }
        }

        public Aggregate GetRoot() {
            var aggregate = this;
            while (aggregate.Parent != null) {
                aggregate = aggregate.Parent.Owner;
            }
            return aggregate;
        }
        public IEnumerable<Aggregate> GetDescendants() {
            var children = Members.SelectMany(member => member.GetChildAggregates());
            foreach (var child in children) {
                yield return child;
                foreach (var descendant in child.GetDescendants()) {
                    yield return descendant;
                }
            }
        }

        public override string ToString() {
            var path = new List<string>();
            var parent = Parent;
            while (parent != null) {
                path.Insert(0, parent.Name);
                parent = parent.Owner.Parent;
            }
            path.Insert(0, GetRoot().Name);
            return $"{nameof(Aggregate)}[{string.Join(".", path)}]";
        }
    }

    partial class AggregateVerticalViewTemplate {
        internal IEnumerable<KeyValuePair<string, string>> Members { get; set; }
    }
}
