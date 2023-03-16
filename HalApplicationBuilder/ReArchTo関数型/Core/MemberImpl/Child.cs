using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.Core.UIModel;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Child : AggregateMember {
        internal Child(PropertyInfo propertyInfo, Aggregate owner) : base(propertyInfo, owner) { }

        internal override IEnumerable<Aggregate> GetChildAggregates()
        {
            yield return Aggregate.AsChild(
                _underlyingProp.PropertyType.GetGenericArguments()[0],
                this);
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
