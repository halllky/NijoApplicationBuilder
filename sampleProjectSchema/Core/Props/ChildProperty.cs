using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;
using haldoc.SqlQuery;

namespace haldoc.Core.Props {
    public class ChildProperty : AggregatePropBase {

        public Aggregate ChildAggregate => Context.GetOrCreateAggregate(
                UnderlyingPropInfo.PropertyType.GetGenericArguments()[0],
                this);

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public override IEnumerable<PropertyTemplate> ToDbEntityProperty() {
            return ChildAggregate.GetDbTablePK();
        }

        public override IEnumerable<PropertyTemplate> ToSearchConditionDtoProperty() {
            return ChildAggregate
                .GetProperties()
                .Where(p => !p.IsPrimaryKey)
                .SelectMany(p => p.ToSearchConditionDtoProperty());
        }
        public override IEnumerable<string> GenerateSearchConditionLayout(string modelPath) {
            var props = ChildAggregate
                .GetProperties()
                .Where(p => !p.IsPrimaryKey);
            yield return new ChildPropertySearchCondition
            {
                ModelPath = modelPath,
                Props = props,
            }.TransformText();
        }

        public override IEnumerable<PropertyTemplate> ToInstanceDtoProperty() {
            yield return new PropertyTemplate {
                CSharpTypeName = $"{Context.GetOutputNamespace(E_Namespace.MvcModel)}.{ChildAggregate.ToSingleItemModel().ClassName}",
                PropertyName = Name,
                Initializer = "new()",
            };
        }
        public override string RenderSingleView(AggregateInstanceBuildContext renderingContext) {
            renderingContext.Push(Name);

            var template = new ChildPropertyInstance {
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
            return ChildAggregate
                .GetProperties()
                .Where(p => !p.IsPrimaryKey)
                .SelectMany(p => p.ToListItemModel());
        }
    }

    partial class ChildPropertySearchCondition {
        public string ModelPath { get; init; }
        public IEnumerable<AggregatePropBase> Props { get; init; }
    }
    partial class ChildPropertyInstance {
        public ChildProperty Property { get; init; }
        public AggregateInstanceBuildContext RenderingContext { get; init; }
    }
}
