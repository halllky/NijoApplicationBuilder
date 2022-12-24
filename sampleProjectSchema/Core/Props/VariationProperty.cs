using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class VariationProperty : AggregatePropBase {

        private Dictionary<int, Aggregate> _variations;
        private IReadOnlyDictionary<int, Aggregate> GetVariations() {
            if (_variations == null) {
                var parent = Context.GetAggregate(UnderlyingPropInfo.DeclaringType);
                var variations = UnderlyingPropInfo.GetCustomAttributes<VariationAttribute>();

                // 型の妥当性チェック
                var childType = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0];
                var cannotAssignable = variations.Where(x => !childType.IsAssignableFrom(x.Type)).ToArray();
                if (cannotAssignable.Any()) {
                    var typeNames = string.Join(", ", cannotAssignable.Select(x => x.Type.Name));
                    throw new InvalidOperationException($"{UnderlyingPropInfo.PropertyType.Name} の派生型でない: {typeNames}");
                }

                _variations = variations.ToDictionary(v => v.Key, v => Context.GetOrCreateAggregate(v.Type, this));
            }
            return _variations;
        }

        public override IEnumerable<Aggregate> GetChildAggregates() {
            return GetVariations().Select(v => v.Value);
        }

        public override IEnumerable<PropertyTemplate> ToDbColumnModel() {
            yield return new PropertyTemplate {
                CSharpTypeName = "int?",
                PropertyName = Name,
            };
        }

        private string SearchConditionPropName(Aggregate variation) => $"{Name}__{variation.Name}";
        public override IEnumerable<PropertyTemplate> ToSearchConditionModel() {
            foreach (var variation in GetVariations()) {
                yield return new PropertyTemplate {
                    PropertyName = SearchConditionPropName(variation.Value),
                    CSharpTypeName = "bool",
                };
            }
        }
        public override IEnumerable<string> GenerateSearchConditionLayout(string modelPath) {
            yield return $"<div>";
            foreach (var variation in GetVariations()) {
                yield return $"    <input type=\"checkbox\" asp-for=\"{modelPath}.{SearchConditionPropName(variation.Value)}\" />";
            }
            yield return $"</div>";
        }

        public override IEnumerable<PropertyTemplate> ToListItemModel() {
            yield return new PropertyTemplate {
                PropertyName = Name,
                CSharpTypeName = "string", // Variation集約名を表示する
            };
        }
    }
}
