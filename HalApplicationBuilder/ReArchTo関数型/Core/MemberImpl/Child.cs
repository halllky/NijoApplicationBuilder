using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Child : AggregateMember {
        internal Child(Config config, PropertyInfo propertyInfo, Aggregate owner) : base(config, propertyInfo, owner) { }

        private string NavigationPropName => _underlyingProp.Name;
        private string SearchConditionPropName => _underlyingProp.Name;
        private string SearchResultPropName(RenderedProerty childProp) => childProp.PropertyName; // TODO 親子でプロパティ名が重複する場合を考慮する
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

        internal override IEnumerable<string> GetInstanceKeysFromInstanceModel(object uiInstance)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<string> GetInstanceKeysFromSearchResult(object searchResult)
        {
            throw new NotImplementedException();
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance)
        {
            throw new NotImplementedException();
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<RenderedProerty> ToDbEntityMember() {
            // ナビゲーションプロパティ
            yield return new RenderedProerty {
                Virtual = true,
                CSharpTypeName = GetChildAggregates().Single().ToDbEntity().CSharpTypeName,
                PropertyName = NavigationPropName,
                Initializer = null,
            };
        }

        internal override IEnumerable<RenderedProerty> ToInstanceModelMember() {
            yield return new RenderedProerty {
                CSharpTypeName = GetChildAggregates().Single().ToUiInstanceClass().CSharpTypeName,
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProerty> ToSearchConditionMember() {
            yield return new RenderedProerty {
                CSharpTypeName = GetChildAggregates().Single().ToSearchConditionClass().CSharpTypeName,
                PropertyName = SearchConditionPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProerty> ToSearchResultMember() {
            foreach (var childProp in GetChildAggregates().Single().ToSearchResultClass().Properties) {
                yield return new RenderedProerty {
                    PropertyName = SearchResultPropName(childProp),
                    CSharpTypeName = childProp.CSharpTypeName,
                    Initializer = childProp.Initializer,
                };
            }
        }
    }
}
