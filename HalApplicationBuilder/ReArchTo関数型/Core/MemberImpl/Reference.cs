using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Reference : AggregateMember {
        internal Reference(PropertyInfo propertyInfo, Aggregate owner) : base(propertyInfo, owner) { }

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
