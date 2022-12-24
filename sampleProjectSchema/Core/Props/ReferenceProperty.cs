using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class ReferenceProperty : AggregatePropBase {

        public Aggregate ReferedAggregate => Context.GetAggregate(UnderlyingPropInfo.PropertyType);

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        public override IEnumerable<PropertyTemplate> ToDbEntityProperty() {
            foreach (var foreignKey in ReferedAggregate.GetDbTablePK()) {
                yield return new PropertyTemplate {
                    PropertyName = $"{this.Name}__{foreignKey.PropertyName}",
                    CSharpTypeName = foreignKey.CSharpTypeName,
                };
            }
        }

        public override IEnumerable<string> GenerateSearchConditionLayout(string modelPath) {
            var aspFor = $"{modelPath}.{Name}";

            // auto complete
            yield return $"<select asp-for=\"{aspFor}\">";
            yield return $"    <option selected=\"selected\" value=\"\"></option>";
            yield return $"</select>";
        }

        public override IEnumerable<PropertyTemplate> ToSearchConditionDtoProperty() {
            yield return new PropertyTemplate {
                PropertyName = Name,
                CSharpTypeName = "string", // 複合キーはJSONを格納
            };
        }
        public override string RenderSingleView(AggregateInstanceBuildContext renderingContext) {
            renderingContext.Push(Name);

            var template = new ReferencePropertyInstance {
                Property = this,
                RenderingContext = renderingContext,
            };
            var code = string.Join(Environment.NewLine, template
                .TransformText()
                .Split(Environment.NewLine)
                .Select((line, index) => index == 0
                    ? line // 先頭行だけは呼び出し元ttファイル内のインデントがそのまま反映されるので
                    : renderingContext.CurrentIndent + line));

            renderingContext.Pop();
            return code;
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

        public override IEnumerable<PropertyTemplate> ToInstanceDtoProperty() {
            yield return new PropertyTemplate {
                CSharpTypeName = typeof(haldoc.Runtime.ExternalObject).FullName,
                PropertyName = Name,
            };
        }
    }

    partial class ReferencePropertyInstance {
        public ReferenceProperty Property { get; init; }
        public AggregateInstanceBuildContext RenderingContext { get; init; }
    }
}
