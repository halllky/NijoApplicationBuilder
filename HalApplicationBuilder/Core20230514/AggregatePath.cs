using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    internal class AggregatePath : ValueObject {
        internal static bool TryCreate(IEnumerable<string> names, out AggregatePath aggregatePath, out string error) {
            if (names.Any(name => name.Contains(SEPARATOR))) {
                aggregatePath = Empty;
                error = $"Aggregate path contains invalid character '{SEPARATOR}'.";
                return false;
            }
            return TryParse(SEPARATOR + string.Join(SEPARATOR, names), out aggregatePath, out error);
        }

        internal static bool TryParse(string fullpath, out AggregatePath aggregatePath, out string error) {
            if (!fullpath.StartsWith(SEPARATOR)) {
                aggregatePath = Empty;
                error = $"Aggregate path '{fullpath}' must start with '{SEPARATOR}'";
                return false;
            }

            var separated = fullpath.Split(SEPARATOR).Skip(1).ToArray();
            if (separated.Length == 0) {
                aggregatePath = Empty;
                error = $"Aggregate path '{fullpath}' must end with any physical name.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(separated.Last())) {
                aggregatePath = Empty;
                error = $"Aggregate path '{fullpath}' must end with any physical name.";
                return false;
            }

            aggregatePath = new AggregatePath(fullpath, separated);
            error = string.Empty;
            return true;
        }

        private const char SEPARATOR = '/';
        internal static AggregatePath Empty => new AggregatePath(new string(SEPARATOR, 1), Array.Empty<string>());

        private AggregatePath(string fullpath, string[] separated) {
            Value = fullpath;
            _separated = separated;
        }

        internal string Value { get; }
        internal string BaseName => _separated.Last();
        private readonly string[] _separated;

        /// <summary>
        /// ルート要素ならnull
        /// </summary>
        internal AggregatePath? GetParentPath() {
            if (_separated.Length == 1) return null;

            var parentArray = _separated.SkipLast(1).ToArray();
            var parentFullpath = SEPARATOR + string.Join(SEPARATOR, parentArray);
            return new AggregatePath(parentFullpath, parentArray);
        }
        internal AggregatePath GetChildAggregatePath(string childAggregateName) {
            var childArray = _separated.Union(new[] { childAggregateName }).ToArray();
            var childFullpath = Value + SEPARATOR + childAggregateName;
            return new AggregatePath(childFullpath, childArray);
        }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Value;
        }
    }
}
