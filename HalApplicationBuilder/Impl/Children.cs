using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;
using HalApplicationBuilder.Runtime;

namespace HalApplicationBuilder.Impl {
    internal class Children : AggregateMemberBase {
        internal Children(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => true;

        private Aggregate _child;
        internal Aggregate ChildAggregate {
            get {
                if (_child == null) {
                    var type = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    _child = new Aggregate(type, this, MemberFactory);
                }
                return _child;
            }
        }

        public override IEnumerable<Core.Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            yield break;
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            var vm = $"{typeof(Runtime.Instance<>).Namespace}.{nameof(Runtime.Instance<object>)}";
            var item = ViewModelProvider.GetInstanceModel(ChildAggregate).RuntimeFullName;
            yield return new AspNetMvc.MvcModelProperty {
                CSharpTypeName = $"List<{vm}<{item}>>",
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels() {
            yield break;
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels() {
            yield break;
        }

        public override string RenderSearchConditionView(ViewRenderingContext context) {
            return string.Empty;
        }

        public override string RenderSearchResultView(ViewRenderingContext context) {
            return string.Empty;
        }

        private string InstanceModelPropName => Name;

        public override string RenderInstanceView(ViewRenderingContext context) {
            var nested = context.Nest(InstanceModelPropName, isCollection: true);
            var template = new ChildrenInstanceTemplate {
                i = context.LoopVar,
                Count = $"{nested.CollectionPath}.{nameof(ICollection<object>.Count)}",
                PartialViewName = new InstancePartialView(ChildAggregate, Config).FileName,
                PartialViewBoundObjectName = nested.AspForPath,
                AspForAddChild = new AggregatePath(ChildAggregate).Value,
                AddButtonBoundObjectName = nested.AspForCollectionPath,
            };
            return template.TransformText();
        }

        public override void MapUIToDB(object instance, object dbEntity, RuntimeContext context, HashSet<object> dbEntities) {
            var prop = instance.GetType().GetProperty(InstanceModelPropName);
            var instanceChildren = (IEnumerable)prop.GetValue(instance);
            foreach (var childInstance in instanceChildren) {
                foreach (var descendantDbEntity in context.ConvertUIToDB(childInstance, instance)) {
                    dbEntities.Add(descendantDbEntity);
                }
            }
        }
    }

    partial class ChildrenInstanceTemplate {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<意図して小文字>")]
        internal string i { get; set; }
        internal string Count { get; set; }
        internal string PartialViewName { get; set; }
        internal string PartialViewBoundObjectName { get; set; }
        internal string AspForAddChild { get; set; }
        internal string AddButtonBoundObjectName { get; set; }

        internal static string AddButtonSenderIdentifier => JsTemplate.AGGREGATE_TREE_PATH_ATTR;
        internal static string AddButtonCssClass => JsTemplate.ADD_CHILD_BTN;
        internal static string ModelPathAttrName => JsTemplate.AGGREGATE_MODEL_PATH_ATTR;
    }
}
