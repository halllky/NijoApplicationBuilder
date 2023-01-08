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
            // ナビゲーションプロパティ
            var item = DbSchema.GetDbEntity(ChildAggregate).RuntimeFullName;
            yield return new DbColumn {
                Virtual = true,
                CSharpTypeName = $"ICollection<{item}>",
                PropertyName = NavigationPropName,
                Initializer = $"new HashSet<{item}>()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            var item = ViewModelProvider.GetInstanceModel(ChildAggregate).RuntimeFullName;
            yield return new AspNetMvc.MvcModelProperty {
                CSharpTypeName = $"List<{item}>",
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

        private string NavigationPropName => Name;
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

        public override void MapUIToDB(object uiInstance, object dbInstance, RuntimeContext context) {
            var childDbEntity = context.DbSchema
                .GetDbEntity(ChildAggregate);
            var childDbProperty = dbInstance
                .GetType()
                .GetProperty(NavigationPropName);

            // Addメソッドはジェネリック型の方のICollectionにしかないのでリフレクションを使って呼び出す
            var collection = (IEnumerable)childDbProperty
                .GetValue(dbInstance);
            var add = collection
                .GetType()
                .GetMethod(nameof(ICollection<object>.Add));

            // キーを比較して重複あるものは上書き、ないものは新規追加、という動きを実現するためのdictionary
            var keymaps = new Dictionary<InstanceKey, object>();
            foreach (var childDbInstance in collection) {
                keymaps.Add(InstanceKey.Create(childDbInstance, childDbEntity), childDbInstance);
            }

            var chlidrenUiInstances = (IEnumerable)uiInstance
                .GetType()
                .GetProperty(InstanceModelPropName)
                .GetValue(uiInstance);

            foreach (var childUiInstance in chlidrenUiInstances) {
                var newChildDbInstance = childDbEntity.ConvertUiInstanceToDbInstance(childUiInstance, context);
                var pk = InstanceKey.Create(newChildDbInstance, childDbEntity);

                if (keymaps.TryGetValue(pk, out var existDbEntity)) {
                    childDbEntity.MapUiInstanceToDbInsntace(childUiInstance, existDbEntity, context);
                } else {
                    add.Invoke(collection, new[] { newChildDbInstance });
                }
            }
        }

        public override void MapDBToUI(object dbInstance, object uiInstance, RuntimeContext context) {
            var childDbProperty = dbInstance
                .GetType()
                .GetProperty(NavigationPropName);
            var childDbInstanceList = (IEnumerable)childDbProperty
                .GetValue(dbInstance);

            var childUiProperty = uiInstance
                .GetType()
                .GetProperty(InstanceModelPropName);
            var childUiType = childUiProperty
                .PropertyType
                .GetGenericArguments()[0];
            var childUiInstanceList = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(childUiType));

            var childDbEntity = context.DbSchema.GetDbEntity(ChildAggregate);
            foreach (var childDbInstance in childDbInstanceList) {
                var childUiInstance = childDbEntity.ConvertDbInstanceToUiInstance(childDbInstance, context);
                childUiInstanceList.Add(childUiInstance);
            }

            childUiProperty.SetValue(uiInstance, childUiInstanceList);
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
        internal static string AddButtonModelIdentifier => JsTemplate.AGGREGATE_MODEL_PATH_ATTR;
        internal static string AddButtonCssClass => JsTemplate.ADD_CHILD_BTN;
    }
}
