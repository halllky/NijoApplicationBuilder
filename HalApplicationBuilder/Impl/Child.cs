using System;
using System.Collections;
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
            // ナビゲーションプロパティ
            yield return new DbColumn {
                Virtual = true,
                CSharpTypeName = DbSchema.GetDbEntity(ChildAggregate).RuntimeFullName,
                PropertyName = NavigationPropName,
            };
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
            yield return new MvcModelProperty {
                CSharpTypeName = ViewModelProvider.GetInstanceModel(ChildAggregate).RuntimeFullName,
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        private string NavigationPropName => Name;
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
            var nested = context.Nest(InstanceModelPropName);
            return ViewModelProvider.GetInstanceModel(ChildAggregate).Render(nested);
        }

        public override void MapUIToDB(object uiInstance, object dbInstance, RuntimeContext context) {
            var chlidUiInstance = uiInstance
                .GetType()
                .GetProperty(InstanceModelPropName)
                .GetValue(uiInstance);
            var childDbEntity = context.DbSchema
                .GetDbEntity(ChildAggregate);
            var childDbInstance = childDbEntity
                .ConvertUiInstanceToDbInstance(chlidUiInstance, context);
            var navigationProperty = dbInstance
                .GetType()
                .GetProperty(NavigationPropName);

            navigationProperty.SetValue(dbInstance, childDbInstance);
        }

        public override void MapDBToUI(object dbInstance, object uiInstance, RuntimeContext context) {
            var dbProperty = dbInstance
                .GetType()
                .GetProperty(NavigationPropName);
            var childDbInstance = dbProperty
                .GetValue(dbInstance);

            if (childDbInstance != null) {
                var childUiInstance = context.DbSchema
                    .GetDbEntity(ChildAggregate)
                    .ConvertDbInstanceToUiInstance(childDbInstance, context);
                var childUiProperty = uiInstance
                    .GetType()
                    .GetProperty(InstanceModelPropName);

                childUiProperty.SetValue(uiInstance, childUiInstance);
            }
        }
    }
}
