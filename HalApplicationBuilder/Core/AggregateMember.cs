using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.CodeRendering;

namespace HalApplicationBuilder.Core
{
    internal abstract class AggregateMember : ValueObject
    {
        internal AggregateMember(Config config, string displayName, bool isPrimary, Aggregate owner) {
            _config = config;
            DisplayName = displayName;
            IsPrimary = isPrimary;
            Owner = owner;
        }

        protected readonly Config _config;

        internal Aggregate Owner { get; }

        internal string DisplayName { get; }
        internal bool IsPrimary { get; }

        internal abstract IEnumerable<Aggregate> GetChildAggregates();

        // AggregateMember => ClassDef
        internal abstract IEnumerable<RenderedProperty> ToDbEntityMember();
        internal abstract IEnumerable<RenderedProperty> ToSearchConditionMember();
        internal abstract IEnumerable<RenderedProperty> ToSearchResultMember();
        internal abstract IEnumerable<RenderedProperty> ToInstanceModelMember();

        // AggregateMember => Code Template
        internal abstract void BuildSearchMethod(CodeRendering.SearchMethodDTO method);
        internal abstract void RenderMvcSearchConditionView(CodeRendering.RenderingContext context);
        internal abstract void RenderAspNetMvcPartialView(CodeRendering.RenderingContext context);

        // Runtime Instance <=> InstanceKey
        internal abstract object? GetInstanceKeyFromDbInstance(object dbInstance);
        internal abstract object? GetInstanceKeyFromUiInstance(object uiInstance);
        internal abstract object? GetInstanceKeyFromSearchResult(object searchResult);
        internal abstract object? GetInstanceKeyFromAutoCompleteItem(object autoCompelteItem);
        internal abstract void MapInstanceKeyToDbInstance(object? instanceKey, object dbInstance);
        internal abstract void MapInstanceKeyToUiInstance(object? instanceKey, object uiInstance);
        internal abstract void MapInstanceKeyToSearchResult(object? instanceKey, object searchResult);
        internal abstract void MapInstanceKeyToAutoCompleteItem(object? instanceKey, object autoCompelteItem);

        // Runtime Instance <=> Runtime Instance
        internal abstract void MapDbToUi(object dbInstance, object uiInstance, Runtime.IInstanceConvertingContext context);
        internal abstract void MapUiToDb(object uiInstance, object dbInstance, Runtime.IInstanceConvertingContext context);

        protected sealed override IEnumerable<object?> ValueObjectIdentifiers()
        {
            yield return Owner;
            yield return DisplayName;
        }
    }
}

