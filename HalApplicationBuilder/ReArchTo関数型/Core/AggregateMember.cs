using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core
{
    internal abstract class AggregateMember : ValueObject
    {
        internal AggregateMember(Config config, PropertyInfo underlyingProp, Aggregate owner) {
            _config = config;
            _underlyingProp = underlyingProp;
            Owner = owner;
        }

        protected readonly Config _config;
        protected readonly PropertyInfo _underlyingProp;

        internal Aggregate Owner { get; }

        internal bool IsPrimary => _underlyingProp.GetCustomAttribute<KeyAttribute>() != null;

        internal abstract IEnumerable<Aggregate> GetChildAggregates();

        internal abstract IEnumerable<RenderedProerty> ToDbEntityMember();
        internal abstract IEnumerable<RenderedProerty> ToSearchConditionMember();
        internal abstract IEnumerable<RenderedProerty> ToSearchResultMember();
        internal abstract IEnumerable<RenderedProerty> ToInstanceModelMember();

        internal abstract void BuildSearchMethod(CodeRendering.SearchMethodDTO method);

        internal abstract IEnumerable<string> GetInstanceKeysFromInstanceModel(object uiInstance);
        internal abstract IEnumerable<string> GetInstanceKeysFromSearchResult(object searchResult);

        internal abstract void MapDbToUi(object dbInstance, object uiInstance);
        internal abstract void MapUiToDb(object uiInstance, object dbInstance);

        protected sealed override IEnumerable<object?> ValueObjectIdentifiers()
        {
            yield return Owner;
        }
    }
}

