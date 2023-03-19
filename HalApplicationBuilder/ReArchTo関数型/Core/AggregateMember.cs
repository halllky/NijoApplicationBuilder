using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
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

        internal string DisplayName => _underlyingProp.GetCustomAttribute<DisplayAttribute>()?.Name ?? _underlyingProp.Name;
        internal bool IsPrimary => _underlyingProp.GetCustomAttribute<KeyAttribute>() != null;

        internal abstract IEnumerable<Aggregate> GetChildAggregates();

        internal abstract IEnumerable<RenderedProperty> ToDbEntityMember();
        internal abstract IEnumerable<RenderedProperty> ToSearchConditionMember();
        internal abstract IEnumerable<RenderedProperty> ToSearchResultMember();
        internal abstract IEnumerable<RenderedProperty> ToInstanceModelMember();

        internal abstract void BuildSearchMethod(CodeRendering.SearchMethodDTO method);
        internal abstract void RenderMvcSearchConditionView(CodeRendering.RenderingContext context);
        internal abstract void RenderAspNetMvcPartialView(CodeRendering.RenderingContext context);

        internal abstract IEnumerable<object> GetInstanceKeysFromInstanceModel(object uiInstance);
        internal abstract IEnumerable<object> GetInstanceKeysFromSearchResult(object searchResult);
        internal abstract IEnumerable<object> GetInstanceKeysFromAutoCompleteItem(object autoCompelteItem);

        internal abstract bool TryDequeueSerializedInstanceKey(Queue<JsonElement> jsonElements, IList<object> objects);

        internal abstract void MapDbToUi(object dbInstance, object uiInstance, Runtime.IInstanceConvertingContext context);
        internal abstract void MapUiToDb(object uiInstance, object dbInstance, Runtime.IInstanceConvertingContext context);

        protected sealed override IEnumerable<object?> ValueObjectIdentifiers()
        {
            yield return Owner;
        }
    }
}

