using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Children : AggregateMember {
        internal Children(Config config, PropertyInfo propertyInfo, Aggregate owner) : base(config, propertyInfo, owner) { }

        internal override IEnumerable<Aggregate> GetChildAggregates()
        {
            yield return Aggregate.AsChild(
                _config,
                _underlyingProp.PropertyType.GetGenericArguments()[0],
                this);
        }

        internal override void BuildSearchMethod(SearchMethodDTO method) {
            // 何もしない
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

        private string NavigationPropName => _underlyingProp.Name;
        private string InstanceModelPropName => _underlyingProp.Name;

        internal override IEnumerable<RenderedProerty> ToDbEntityMember() {
            // ナビゲーションプロパティ
            var childType = GetChildAggregates().Single().ToDbEntity().CSharpTypeName;
            yield return new NavigationProerty {
                Virtual = true,
                CSharpTypeName = $"ICollection<{childType}>",
                PropertyName = NavigationPropName,
                Initializer = $"new HashSet<{childType}>()",
                IsManyToOne = true,
                IsPrincipal = true,
                OpponentName = Aggregate.PARENT_NAVIGATION_PROPERTY_NAME,
            };
        }

        internal override IEnumerable<RenderedProerty> ToInstanceModelMember() {
            var item = GetChildAggregates().Single().ToUiInstanceClass().CSharpTypeName;
            yield return new RenderedProerty {
                CSharpTypeName = $"List<{item}>",
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProerty> ToSearchConditionMember() {
            yield break;
        }

        internal override IEnumerable<RenderedProerty> ToSearchResultMember() {
            yield break;
        }
    }
}
