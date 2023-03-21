using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.Members;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;
using HalApplicationBuilder.ReArchTo関数型.Runtime;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Child : AggregateMember {
        internal Child(Config config, PropertyInfo propertyInfo, Aggregate owner) : base(config, propertyInfo, owner) { }

        private string NavigationPropName => _underlyingProp.Name;
        private string SearchConditionPropName => _underlyingProp.Name;
        private string SearchResultPropName(RenderedProperty childProp) => childProp.PropertyName; // TODO 親子でプロパティ名が重複する場合を考慮する
        private string InstanceModelPropName => _underlyingProp.Name;

        internal override IEnumerable<Aggregate> GetChildAggregates()
        {
            yield return Aggregate.AsChild(
                _config,
                _underlyingProp.PropertyType.GetGenericArguments()[0],
                this);
        }

        internal override void BuildSearchMethod(SearchMethodDTO method) {
            // TODO: SELECT句の組み立て
            // TODO: WHERE句の組み立て
        }

        internal override void RenderMvcSearchConditionView(RenderingContext context) {
            var nested = context.Nest(SearchConditionPropName);
            GetChildAggregates().Single().RenderSearchCondition(nested);
        }

        internal override void RenderAspNetMvcPartialView(RenderingContext context) {
            var nested = context.Nest(InstanceModelPropName);
            GetChildAggregates().Single().RenderAspNetMvcPartialView(nested);
        }

        internal override object? GetInstanceKeyFromDbInstance(object dbInstance) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override void MapInstanceKeyToDbInstance(object? instanceKey, object dbInstance) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override object? GetInstanceKeyFromUiInstance(object uiInstance) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override object? GetInstanceKeyFromSearchResult(object searchResult) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override object? GetInstanceKeyFromAutoCompleteItem(object autoCompelteItem) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapInstanceKeyToUiInstance(object? instanceKey, object uiInstance) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapInstanceKeyToSearchResult(object? instanceKey, object searchResult) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapInstanceKeyToAutoCompleteItem(object? instanceKey, object autoCompelteItem) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance, IInstanceConvertingContext context) {
            var child = GetChildAggregates().Single();
            var dbEntity = child.ToDbEntity();

            // ナビゲーションプロパティ => UI子要素
            var navigationProp = dbInstance.GetType().GetProperty(NavigationPropName);
            var uiChildProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (navigationProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiChildProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var uiChild = uiChildProp.GetValue(uiInstance);
            if (uiChild == null) {
                uiChild = context.CreateInstance(child.ToUiInstanceClass().CSharpTypeName);
                uiChildProp.SetValue(uiInstance, uiChild);
            }
            var navigation = navigationProp.GetValue(dbInstance);
            if (navigation == null) {
                navigation = context.CreateInstance(dbEntity.CSharpTypeName);
                navigationProp.SetValue(dbInstance, navigation);
            }

            child.MapDbToUi(navigation, uiChild, context);
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance, IInstanceConvertingContext context) {
            var child = GetChildAggregates().Single();
            var dbEntity = child.ToDbEntity();

            // UI子要素 => ナビゲーションプロパティ
            var navigationProp = dbInstance.GetType().GetProperty(NavigationPropName);
            var uiChildProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (navigationProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiChildProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var uiChild = uiChildProp.GetValue(uiInstance);
            if (uiChild == null) {
                uiChild = context.CreateInstance(child.ToUiInstanceClass().CSharpTypeName);
                uiChildProp.SetValue(uiInstance, uiChild);
            }
            var navigation = navigationProp.GetValue(dbInstance);
            if (navigation == null) {
                navigation = context.CreateInstance(dbEntity.CSharpTypeName);
                navigationProp.SetValue(dbInstance, navigation);
            }

            child.MapUiToDb(uiChild, navigation, context);
        }

        internal override IEnumerable<RenderedProperty> ToDbEntityMember() {
            // ナビゲーションプロパティ
            yield return new RenderedProperty {
                Virtual = true,
                CSharpTypeName = GetChildAggregates().Single().ToDbEntity().CSharpTypeName,
                PropertyName = NavigationPropName,
                Initializer = null,
            };
        }

        internal override IEnumerable<RenderedProperty> ToInstanceModelMember() {
            yield return new RenderedProperty {
                CSharpTypeName = GetChildAggregates().Single().ToUiInstanceClass().CSharpTypeName,
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchConditionMember() {
            yield return new RenderedProperty {
                CSharpTypeName = GetChildAggregates().Single().ToSearchConditionClass().CSharpTypeName,
                PropertyName = SearchConditionPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchResultMember() {
            foreach (var childProp in GetChildAggregates().Single().ToSearchResultClass().Properties) {
                yield return new RenderedProperty {
                    PropertyName = SearchResultPropName(childProp),
                    CSharpTypeName = childProp.CSharpTypeName,
                    Initializer = childProp.Initializer,
                };
            }
        }
    }
}
