using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
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
            var fullpathStartsWithSeparator = fullpath.StartsWith(SEPARATOR)
                ? fullpath
                : SEPARATOR + fullpath;

            var separated = fullpathStartsWithSeparator.Split(SEPARATOR).Skip(1).ToArray();
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

            aggregatePath = new AggregatePath(fullpathStartsWithSeparator, separated);
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

        internal bool TryCreateChild(string childBasename, out AggregatePath childPath, out string error) {
            var child = new List<string>(_separated) { childBasename };
            return TryCreate(child, out childPath, out error);
        }

        protected override IEnumerable<object?> ValueObjectIdentifiers() {
            yield return Value;
        }
    }
}
