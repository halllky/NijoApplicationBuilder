using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class ChildrenProperty : AggregatePropBase {

        public Aggregate ChildAggregate => Context.GetOrCreateAggregate(
                UnderlyingPropInfo.PropertyType.GetGenericArguments()[0],
                this,
                asChildren: true);

        public override bool IsListProperty => true;

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public override IEnumerable<PropertyTemplate> ToDbEntityProperty() {
            //// navigation property
            //yield return new PropertyTemplate {
            //    CSharpTypeName = $"virtual ICollection<{ChildAggregate.ToDbTableModel().ClassName}>",
            //    PropertyName = ChildAggregate.ToDbTableModel().ClassName,
            //    Initializer = $"new HashSet<{ChildAggregate.ToDbTableModel().ClassName}>()",
            //};
            yield break;
        }

        public override IEnumerable<PropertyTemplate> ToSearchConditionDtoProperty() {
            yield break;
        }
        public override IEnumerable<string> GenerateSearchConditionLayout(string modelPath) {
            yield break;
        }
        public override string RenderSingleView(AggregateInstanceBuildContext renderingContext) {
            renderingContext.PushArrayMember(Name);

            var template = new ChildrenPropertyInstance {
                RenderingContext = renderingContext,
                Property = this,
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

        public override IEnumerable<PropertyTemplate> ToInstanceDtoProperty() {
            yield return new PropertyTemplate {
                CSharpTypeName = $"List<{Context.GetOutputNamespace(E_Namespace.MvcModel)}.{ChildAggregate.ToSingleItemModel().ClassName}>",
                PropertyName = Name,
                Initializer = "new()",
            };
        }

        public override IEnumerable<PropertyTemplate> ToListItemModel() {
            yield break;
        }

        public override IEnumerable<object> AssignMvcToDb(object mvcModel, object dbEntity) {
            var list = (IEnumerable)mvcModel.GetType().GetProperty(Name).GetValue(mvcModel);
            foreach (var mvcModelChild in list) {
                foreach (var dbEntityChild in ChildAggregate.TransformMvcModelToDbEntities(mvcModelChild)) {
                    yield return dbEntityChild;
                }
            }
        }
    }

    partial class ChildrenPropertyInstance {
        public AggregateInstanceBuildContext RenderingContext { get; init; }
        public ChildrenProperty Property { get; init; }
    }
}
