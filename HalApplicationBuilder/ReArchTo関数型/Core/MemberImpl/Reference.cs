using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Reference : AggregateMember {
        internal Reference(Config config, PropertyInfo propertyInfo, Aggregate owner) : base(config, propertyInfo, owner) { }

        private Aggregate GetRefTarget() {
            return Aggregate.AsRef(_config, _underlyingProp.PropertyType.GetGenericArguments()[0], this);
        }

        internal override IEnumerable<Aggregate> GetChildAggregates()
        {
            yield break;
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

        internal override IEnumerable<RenderedProerty> ToDbEntityMember() {
            var refTargetDbEntity = GetRefTarget().ToDbEntity();
            // 参照先DBの主キー
            foreach (var foreignKey in refTargetDbEntity.PrimaryKeys) {
                yield return foreignKey;
            }
            // ナビゲーションプロパティ
            yield return new NavigationProerty {
                Virtual = true,
                CSharpTypeName = refTargetDbEntity.CSharpTypeName,
                PropertyName = _underlyingProp.Name,
                Initializer = null,
                IsManyToOne = false,
                IsPrincipal = true,
                OpponentName = $"{_underlyingProp.Name}_Refered",
            };
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
