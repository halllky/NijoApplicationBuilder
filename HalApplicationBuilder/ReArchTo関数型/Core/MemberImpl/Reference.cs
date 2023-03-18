using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Reference : AggregateMember {
        internal Reference(Config config, PropertyInfo propertyInfo, Aggregate owner) : base(config, propertyInfo, owner) { }

        internal ReferredAggregate GetRefTarget() {
            return new ReferredAggregate(_config, _underlyingProp.PropertyType.GetGenericArguments()[0], this);
        }

        internal override IEnumerable<Aggregate> GetChildAggregates()
        {
            yield break;
        }

        internal override void BuildSearchMethod(SearchMethodDTO method) {
            // TODO: SELECT句の組み立て
            // TODO: WHERE句の組み立て
        }

        internal override void RenderMvcSearchConditionView(RenderingContext context) {
            var key = context.ObjectPath
                .Nest(SearchConditonPropName)
                .Nest(nameof(Runtime.ReferenceDTO.InstanceKey))
                .AspForPath;
            var text = context.ObjectPath
                .Nest(SearchConditonPropName)
                .Nest(nameof(Runtime.ReferenceDTO.InstanceName))
                .AspForPath;
            var guid = context.ObjectPath
                .Nest(SearchConditonPropName)
                .Nest(nameof(Runtime.ReferenceDTO.AggreageteGuid))
                .AspForPath;

            context.Template.WriteLine($"<div>");
            context.Template.WriteLine($"    <input type=\"hidden\" asp-for=\"{key}\" class=\"{AspNetMvc.JsTemplate.AUTOCOMPLETE_VALUE}\" />");
            context.Template.WriteLine($"    <input type=\"hidden\" asp-for=\"{guid}\" class=\"{AspNetMvc.JsTemplate.AGGREGATE_GUID}\" />");
            context.Template.WriteLine($"    <input asp-for=\"{text}\" class=\"{AspNetMvc.JsTemplate.AUTOCOMPLETE_INPUT}\" />");
            context.Template.WriteLine($"<div>");
        }

        internal override void RenderAspNetMvcPartialView(RenderingContext context) {
            var key = context.ObjectPath
                .Nest(SearchConditonPropName)
                .Nest(nameof(Runtime.ReferenceDTO.InstanceKey))
                .AspForPath;
            var text = context.ObjectPath
                .Nest(SearchConditonPropName)
                .Nest(nameof(Runtime.ReferenceDTO.InstanceName))
                .AspForPath;
            var guid = context.ObjectPath
                .Nest(SearchConditonPropName)
                .Nest(nameof(Runtime.ReferenceDTO.AggreageteGuid))
                .AspForPath;

            context.Template.WriteLine($"<div>");
            context.Template.WriteLine($"    <input type=\"hidden\" asp-for=\"{key}\" class=\"{AspNetMvc.JsTemplate.AUTOCOMPLETE_VALUE}\" />");
            context.Template.WriteLine($"    <input type=\"hidden\" asp-for=\"{guid}\" class=\"{AspNetMvc.JsTemplate.AGGREGATE_GUID}\" />");
            context.Template.WriteLine($"    <input asp-for=\"{text}\" class=\"{AspNetMvc.JsTemplate.AUTOCOMPLETE_INPUT}\" />");
            context.Template.WriteLine($"<div>");
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

        internal override IEnumerable<RenderedProperty> ToDbEntityMember() {
            var refTarget = GetRefTarget();
            var refTargetDbEntity = refTarget.ToDbEntity();
            // 参照先DBの主キー
            foreach (var foreignKey in refTargetDbEntity.PrimaryKeys) {
                yield return foreignKey;
            }
            // ナビゲーションプロパティ
            yield return new NavigationProperty {
                Virtual = true,
                CSharpTypeName = refTargetDbEntity.CSharpTypeName,
                PropertyName = _underlyingProp.Name,
                Multiplicity = NavigationProperty.E_Multiplicity.HasOneWithMany,
                IsPrincipal = true,
                OpponentName = refTarget.GetEFCoreEntiyHavingOnlyReferredNavigationProp().Properties.Single().PropertyName,
            };
        }

        private string SearchConditonPropName => _underlyingProp.Name;
        private string SearchResultPropName => _underlyingProp.Name;
        private string InstanceModelPropName => _underlyingProp.Name;

        internal override IEnumerable<RenderedProperty> ToInstanceModelMember() {
            yield return new RenderedProperty {
                CSharpTypeName = typeof(Runtime.ReferenceDTO).FullName!,
                PropertyName = InstanceModelPropName,
                Initializer = $"new() {{ {nameof(Runtime.ReferenceDTO.AggreageteGuid)} = new Guid(\"{GetRefTarget().GUID}\") }}",
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchConditionMember() {
            yield return new RenderedProperty {
                CSharpTypeName = typeof(Runtime.ReferenceDTO).FullName!,
                PropertyName = SearchConditonPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchResultMember() {
            yield return new RenderedProperty {
                CSharpTypeName = "string",
                PropertyName = SearchResultPropName,
            };
        }
    }
}
