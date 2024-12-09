using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    public class EnumDefinition {
        internal static bool TryCreate(string name, IEnumerable<Item> items, out EnumDefinition enumDefinition, out ICollection<string> errors) {
            var sorted = items.OrderBy(x => x.Value).Select(x => new Item {
                Value = x.Value,
                PhysicalName = x.PhysicalName.ToCSharpSafe(),
                DisplayName = x.DisplayName,
            }).ToArray();

            errors = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(name)) {
                errors.Add("列挙体の名称が指定されていません。");
            }
            if (!sorted.Any()) {
                errors.Add($"'{name}' の要素が空です。");
            }
            var duplicates1 = sorted.GroupBy(x => x.Value).Where(group => group.Count() >= 2).ToArray();
            if (duplicates1.Any()) {
                errors.Add($"'{name}' の要素の値に重複があります: {duplicates1.First().Key}");
            }
            var duplicates2 = sorted.GroupBy(x => x.PhysicalName).Where(group => group.Count() >= 2).ToArray();
            if (duplicates2.Any()) {
                errors.Add($"'{name}' の要素の名称に重複があります: {duplicates2.First().Key}");
            }

            enumDefinition = errors.Any()
                ? new EnumDefinition(string.Empty, new List<Item>())
                : new EnumDefinition(name.ToCSharpSafe(), sorted);
            return !errors.Any();
        }

        private EnumDefinition(string name, IList<Item> items) {
            Name = name;
            Items = items;
        }

        public string Name { get; }
        public IList<Item> Items { get; }

        public class Item {
            public required int Value { get; init; }
            public required string PhysicalName { get; init; }
            public required string DisplayName { get; init; }
        }
    }
}
