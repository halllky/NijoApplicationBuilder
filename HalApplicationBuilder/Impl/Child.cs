using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;
using HalApplicationBuilder.Runtime;

namespace HalApplicationBuilder.Impl {
    internal class Child : AggregateMemberBase {
        internal Child(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => false;

        private Aggregate _child;
        private Aggregate ChildAggregate {
            get {
                if (_child == null) {
                    var childType = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    _child = new Aggregate(childType, this, MemberFactory);
                }
                return _child;
            }
        }

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            yield break;
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = ViewModelProvider.GetSearchConditionModel(ChildAggregate).RuntimeFullName,
                PropertyName = SearchConditionPropName,
                Initializer = "new()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels() {
            foreach (var childProp in ViewModelProvider.GetSearchResultModel(ChildAggregate).Properties) {
                yield return new MvcModelProperty {
                    PropertyName = childProp.PropertyName,
                    CSharpTypeName = childProp.CSharpTypeName,
                    Initializer = childProp.Initializer,
                };
            }
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            var vm = $"{typeof(Instance<>).Namespace}.{nameof(Instance<object>)}";
            var item = ViewModelProvider.GetInstanceModel(ChildAggregate).RuntimeFullName;
            yield return new MvcModelProperty {
                CSharpTypeName = $"{vm}<{item}>",
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        private string SearchConditionPropName => Name;
        private string InstanceModelPropName => Name;

        public override string RenderSearchConditionView(ViewRenderingContext context) {
            var nested = context.Nest(SearchConditionPropName);
            return ViewModelProvider.GetSearchConditionModel(ChildAggregate).Render(nested);
        }

        public override string RenderSearchResultView(ViewRenderingContext context) {
            return string.Empty;
        }

        public override string RenderInstanceView(ViewRenderingContext context) {
            var nested = context.Nest(InstanceModelPropName).Nest(nameof(Instance<object>.Item));
            return ViewModelProvider.GetInstanceModel(ChildAggregate).Render(nested);
        }
    }
}
