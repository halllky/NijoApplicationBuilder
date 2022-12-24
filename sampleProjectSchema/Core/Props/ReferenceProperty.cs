using System;
using System.Collections.Generic;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class ReferenceProperty : AggregatePropBase {

        public Aggregate ReferedAggregate => Context.GetAggregate(UnderlyingPropInfo.PropertyType);

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        public override IEnumerable<PropertyTemplate> ToDbColumnModel() {
            foreach (var foreignKey in ReferedAggregate.GetDbTablePK()) {
                yield return new PropertyTemplate {
                    PropertyName = $"{this.Name}__{foreignKey.PropertyName}",
                    CSharpTypeName = foreignKey.CSharpTypeName,
                };
            }
        }

        public override IEnumerable<PropertyTemplate> ToSearchConditionModel() {
            yield return new PropertyTemplate {
                PropertyName = Name,
                CSharpTypeName = "string", // 複合キーはJSONを格納
            };
        }
        public override IEnumerable<string> GenerateSearchConditionLayout(string modelPath) {
            var aspFor = $"{modelPath}.{Name}";

            yield return $"<select asp-for=\"{aspFor}\">";
            yield return $"    <option selected=\"selected\" value=\"\"></option>";
            yield return $"</select>";
        }

        public override IEnumerable<PropertyTemplate> ToListItemModel() {
            // UI上に表示する名前
            yield return new PropertyTemplate {
                PropertyName = Name,
                CSharpTypeName = "string",
            };
            // リンク用
            yield return new PropertyTemplate {
                PropertyName = Name + "__ID",
                CSharpTypeName = "string", // 複合キーはJSONを格納
            };
        }
    }
}
