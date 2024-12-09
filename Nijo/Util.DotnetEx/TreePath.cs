using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx {
    internal class TreePath : ValueObject {
        public static TreePath FromString(string str) {
            var path = str
                .Split(SEPARATOR)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim());
            return new TreePath(path);
        }

        public TreePath(IEnumerable<string> value) {
            _value = value.ToArray();
        }
        private readonly string[] _value;
        private TreePath? _parentCache;

        public string BaseName => _value.LastOrDefault() ?? string.Empty;
        public bool IsRoot => _value.Length <= 1;
        public TreePath Parent => _parentCache ??= _value.Length == 0
            ? new TreePath(Enumerable.Empty<string>())
            : new TreePath(_value.SkipLast(1));

        public TreePath CreateChild(string name) {
            return new TreePath(_value.Concat(new[] { name }));
        }
        public NodeId ToGraphNodeId() {
            return new NodeId(ToString());
        }
        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            foreach (var item in _value) yield return item;
        }
        public override string ToString() {
            return SEPARATOR + _value.Join(SEPARATOR.ToString());
        }

        private const char SEPARATOR = '/';
    }
}
