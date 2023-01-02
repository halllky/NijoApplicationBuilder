using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;

namespace HalApplicationBuilder.MembersImpl {
    internal class Reference : AggregateMemberBase {
        public override bool IsCollection => false;

        private Aggregate _refTarget;
        private Aggregate RefTarget {
            get {
                if (_refTarget == null) {
                    var type = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    _refTarget = Schema.FindByType(type);
                    if (_refTarget == null) throw new InvalidOperationException($"{UnderlyingPropertyInfo.Name} の型 {type.FullName} の集約が定義されていません。");
                }
                return _refTarget;
            }
        }

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            foreach (var foreignKey in Schema.GetDbEntity(RefTarget).PKColumns) {
                yield return new DbColumn {
                    CSharpTypeName = foreignKey.CSharpTypeName,
                    PropertyName = $"{Name}_{foreignKey.PropertyName}",
                };
            }
        }

        public override IEnumerable<AspNetMvc.MvcModelProperty> CreateInstanceModels(IAggregateMember member) {
            yield return new AspNetMvc.MvcModelProperty {
                CSharpTypeName = typeof(Runtime.ReferenceDTO).FullName,
                PropertyName = Name,
                Initializer = "new()",
            };
        }

        public override IEnumerable<AspNetMvc.MvcModelProperty> CreateSearchConditionModels(IAggregateMember member) {
            yield return new AspNetMvc.MvcModelProperty {
                CSharpTypeName = typeof(Runtime.ReferenceDTO).FullName,
                PropertyName = Name,
                Initializer = "new()",
            };
        }

        public override IEnumerable<AspNetMvc.MvcModelProperty> CreateSearchResultModels(IAggregateMember member) {
            yield return new AspNetMvc.MvcModelProperty {
                CSharpTypeName = "string",
                PropertyName = UnderlyingPropertyInfo.Name,
            };
        }

        internal override string RenderSearchConditionView(AspNetMvc.ViewRenderingContext context) {
            var model = SearchConditionModels.Single();
            var nestedKey = context.Nest(model.PropertyName).Nest(nameof(Runtime.ReferenceDTO.InstanceKey));
            var nestedText = context.Nest(model.PropertyName).Nest(nameof(Runtime.ReferenceDTO.InstanceName));
            var template = new ReferenceInstanceTemplate {
                AspForKey = nestedKey.AspForPath,
                AspForText = nestedText.AspForPath,
            };
            return template.TransformText();
        }

        internal override string RenderSearchResultView(AspNetMvc.ViewRenderingContext context) {
            var model = SearchResultModels.Single();
            var nestedKey = context.Nest(model.PropertyName).Nest(nameof(Runtime.ReferenceDTO.InstanceKey));
            var nestedText = context.Nest(model.PropertyName).Nest(nameof(Runtime.ReferenceDTO.InstanceName));
            return $"<span>@{nestedText.Path}<input type=\"hidden\" asp-for=\"{nestedKey.AspForPath}\"></span>";
        }

        internal override string RenderInstanceView(AspNetMvc.ViewRenderingContext context) {
            var model = InstanceModels.Single();
            var nestedKey = context.Nest(model.PropertyName).Nest(nameof(Runtime.ReferenceDTO.InstanceKey));
            var nestedText = context.Nest(model.PropertyName).Nest(nameof(Runtime.ReferenceDTO.InstanceName));
            var template = new ReferenceInstanceTemplate {
                AspForKey = nestedKey.AspForPath,
                AspForText = nestedText.AspForPath,
            };
            return template.TransformText();
        }
    }

    partial class ReferenceInstanceTemplate {
        internal string AspForKey { get; set; }
        internal string AspForText { get; set; }
    }
}
