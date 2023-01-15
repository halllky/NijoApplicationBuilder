using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Core {
    public class Aggregate {

        internal Aggregate(Type underlyingType, IAggregateMember parent, IAggregateMemberFactory memberFactory) {
            UnderlyingType = underlyingType;
            Parent = parent;
            _memberFactory = memberFactory;
        }

        private readonly IAggregateMemberFactory _memberFactory;
        internal Type UnderlyingType { get; }

        public string Name => UnderlyingType.Name;
        public Guid GUID => UnderlyingType.GUID;


        public IAggregateMember Parent { get; }

        private List<IAggregateMember> _members;
        public IReadOnlyList<IAggregateMember> Members {
            get {
                if (_members == null) {
                    _members = _memberFactory.CreateMembers(this).ToList();
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
        public IEnumerable<Aggregate> GetChildren() {
            return Members.SelectMany(member => member.GetChildAggregates());
        }
        public IEnumerable<Aggregate> GetAncestors() {
            var aggregate = Parent?.Owner;
            while (aggregate != null) {
                yield return aggregate;
                aggregate = aggregate.Parent?.Owner;
            }
        }
        public IEnumerable<Aggregate> GetDescendants() {
            foreach (var child in GetChildren()) {
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
}
