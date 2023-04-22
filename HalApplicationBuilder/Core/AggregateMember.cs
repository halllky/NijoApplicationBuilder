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
        internal string GetCSharpSafeName() => DisplayName.ToCSharpSafe();
        internal bool IsPrimary { get; }

        internal class MemberPath {
            internal required RootAggregate Root { get; init; }
            /// <summary>よりルートに近いほうから順番</summary>
            internal required IReadOnlyList<AggregateMember> Path { get; init; }
        }
        internal MemberPath GetMemberPath() {
            var path = new List<AggregateMember>();
            var member = this;
            while (member != null) {
                path.Insert(0, member);
                member = member.Owner.Parent;
            }

            RootAggregate root;
            var aggregate = (Aggregate?)Owner;
            while (true) {
                if (aggregate is RootAggregate r) {
                    root = r;
                    break;
                }
                if (aggregate == null) throw new InvalidOperationException("ルート集約特定失敗");
                aggregate = aggregate?.Parent?.Owner;
            }

            return new MemberPath { Root = root, Path = path };
        }

        internal abstract IEnumerable<Aggregate> GetChildAggregates();

        // AggregateMember => ClassDef
        internal abstract IEnumerable<RenderedProperty> ToDbEntityMember();
        internal abstract IEnumerable<RenderedProperty> ToSearchConditionMember();
        internal abstract IEnumerable<RenderedProperty> ToSearchResultMember();
        internal abstract IEnumerable<RenderedProperty> ToInstanceModelMember();

        // AggregateMember => Code Template
        internal abstract void BuildSearchMethod(CodeRendering.SearchMethodDTO method);
        internal abstract void RenderMvcSearchConditionView(CodeRendering.RenderingContext context);
        internal abstract void RenderReactSearchConditionView(RenderingContext context);
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

        // Serialize
        internal abstract Serialized.MemberJson ToJson();

        protected sealed override IEnumerable<object?> ValueObjectIdentifiers()
        {
            yield return Owner;
            yield return DisplayName;
        }
    }
}

