using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Variation : AggregateMember {
        internal Variation(Config config, PropertyInfo propertyInfo, Aggregate owner) : base(config, propertyInfo, owner) { }

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
                yield return Aggregate.AsChild(_config, attr.Type, this);
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

        private string DbPropName => _underlyingProp.Name;
        internal string NavigationPropName(RenderedEFCoreEntity variationDbEntity) => $"{DbPropName}__{variationDbEntity.ClassName}";

        internal override IEnumerable<RenderedProerty> ToDbEntityMember() {
            // 区分値
            yield return new RenderedProerty {
                Virtual = false,
                CSharpTypeName = "int?",
                PropertyName = DbPropName,
                Initializer = null,
            };
            // ナビゲーションプロパティ
            foreach (var variation in GetChildAggregates()) {
                var variationDbEntity = variation.ToDbEntity();
                yield return new NavigationProerty {
                    Virtual = true,
                    CSharpTypeName = variationDbEntity.CSharpTypeName,
                    PropertyName = NavigationPropName(variationDbEntity),
                    Initializer = null,
                    IsManyToOne = false,
                    IsPrincipal = true,
                    OpponentName = Aggregate.PARENT_NAVIGATION_PROPERTY_NAME,
                };
            }
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
