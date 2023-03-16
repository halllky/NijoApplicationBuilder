using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.Core.UIModel;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Variation : AggregateMember {
        internal Variation(PropertyInfo propertyInfo, Aggregate owner) : base(propertyInfo, owner) { }

        internal override IEnumerable<Aggregate> GetChildAggregates() {
            var childType = _underlyingProp.PropertyType.GetGenericArguments()[0];
            var attrs = _underlyingProp.GetCustomAttributes<VariationAttribute>().ToArray();

            // バリエーションが存在するかチェック
            if (!attrs.Any())
                throw new InvalidOperationException($"{childType.Name} の派生型が定義されていない");

            // 型の妥当性チェック
            var cannotAssignable = attrs.Where(x => !childType.IsAssignableFrom(x.Type)).ToArray();
            if (cannotAssignable.Any()) {
                var typeNames = string.Join(", ", cannotAssignable.Select(x => x.Type.Name));
                throw new InvalidOperationException($"{childType.Name} の派生型でない: {typeNames}");
            }

            foreach (var attr in attrs) {
                yield return Aggregate.AsChild(attr.Type, this);
            }
        }

        internal override void BuildSearchMethod(SearchMethodDTO method)
        {
            throw new NotImplementedException();
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

        internal override IEnumerable<RenderedProerty> ToDbEntityMember()
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<RenderedProerty> ToInstanceModelMember()
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<RenderedProerty> ToSearchConditionMember()
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<RenderedProerty> ToSearchResultMember()
        {
            throw new NotImplementedException();
        }
    }
}
