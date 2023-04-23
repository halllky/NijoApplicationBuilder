using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.Runtime;
using HalApplicationBuilder.Serialized;

namespace HalApplicationBuilder.Core.MemberImpl {
    internal class Children : AggregateMember {
        internal Children(Config config, string displayName, bool isPrimary, Aggregate owner, IAggregateDefine childType) : base(config, displayName, isPrimary, owner) {
            _childType = childType;
        }

        private readonly IAggregateDefine _childType;

        internal override IEnumerable<Aggregate> GetChildAggregates()
        {
            yield return Aggregate.AsChild(_config, _childType, this);
        }

        internal override void BuildSearchMethod(SearchMethodDTO method) {
            // 何もしない
        }

        internal override void RenderMvcSearchConditionView(RenderingContext context) {
            // 何もしない
        }
        internal override void RenderReactSearchCondition(RenderingContext context) {
            // 何もしない
        }

        internal override void RenderAspNetMvcPartialView(RenderingContext context) {

            var childAggregate = GetChildAggregates().Single();
            var i = new CodeRendering.AspNetMvc.LoopVar(context.ObjectPath);
            var children = context.ObjectPath.Nest(InstanceModelPropName).Path;
            var childrenAspFor = context.ObjectPath.Nest(InstanceModelPropName).AspForPath;
            var partialView = new CodeRendering.AspNetMvc.InstancePartialViewTemplate(_config, childAggregate).FileName;

            context.Template.WriteLine($"@for (var {i} = 0; {i} < {children}.Count; {i}++) {{");
            context.Template.WriteLine($"    <partial name=\"{partialView}\" for=\"{childrenAspFor}[{i}]\" />");
            context.Template.WriteLine($"}}");

            context.Template.WriteLine($"<input");
            context.Template.WriteLine($"    type=\"button\"");
            context.Template.WriteLine($"    value=\"追加\"");
            context.Template.WriteLine($"    class=\"halapp-btn-secondary {CodeRendering.AspNetMvc.JsTemplate.ADD_CHILD_BTN}\"");
            context.Template.WriteLine($"    {CodeRendering.AspNetMvc.JsTemplate.AGGREGATE_TREE_PATH_ATTR}=\"{childAggregate.GetUniquePath()}\"");
            context.Template.WriteLine($"    {CodeRendering.AspNetMvc.JsTemplate.AGGREGATE_MODEL_PATH_ATTR}=\"{childrenAspFor}\"");
            context.Template.WriteLine($"    />");
        }
        internal override void RenderReactComponent(RenderingContext context) {
            throw new NotImplementedException();
        }


        internal override object? GetInstanceKeyFromDbInstance(object dbInstance) {
            throw new InvalidOperationException($"ChildrenをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override object? GetInstanceKeyFromUiInstance(object uiInstance) {
            throw new InvalidOperationException($"ChildrenをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override object? GetInstanceKeyFromSearchResult(object searchResult) {
            throw new InvalidOperationException($"ChildrenをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override object? GetInstanceKeyFromAutoCompleteItem(object autoCompelteItem) {
            throw new InvalidOperationException($"ChildrenをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override void MapInstanceKeyToDbInstance(object? instanceKey, object dbInstance) {
            throw new InvalidOperationException($"ChildrenをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override void MapInstanceKeyToUiInstance(object? instanceKey, object uiInstance) {
            throw new InvalidOperationException($"ChildrenをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override void MapInstanceKeyToSearchResult(object? instanceKey, object searchResult) {
            throw new InvalidOperationException($"ChildrenをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override void MapInstanceKeyToAutoCompleteItem(object? instanceKey, object autoCompelteItem) {
            throw new InvalidOperationException($"ChildrenをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance, IInstanceConvertingContext context) {
            var dbProp = dbInstance.GetType().GetProperty(NavigationPropName);
            var uiProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (dbProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiProp == null) throw new ArgumentException(null, nameof(uiInstance));

            // Clear,Addメソッドはジェネリック型の方のICollectionにしかないのでリフレクションを使って呼び出す
            var clear = uiProp.PropertyType.GetMethod(nameof(ICollection<object>.Clear));
            var add = uiProp.PropertyType.GetMethod(nameof(ICollection<object>.Add));
            if (clear == null) throw new ArgumentException(null, nameof(uiInstance));
            if (add == null) throw new ArgumentException(null, nameof(uiInstance));

            var uiChildren = uiProp.GetValue(uiInstance)!;
            clear.Invoke(uiChildren, Array.Empty<object>());

            var dbChildren = (IEnumerable)dbProp.GetValue(dbInstance)!;
            var childAggregate = GetChildAggregates().Single();
            foreach (var dbChild in dbChildren) {
                var uiChild = context.CreateInstance(childAggregate.ToUiInstanceClass().CSharpTypeName);
                childAggregate.MapDbToUi(dbChild, uiChild, context);
                add.Invoke(uiChildren, new[] { uiChild });
            }
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance, IInstanceConvertingContext context) {
            var dbProp = dbInstance.GetType().GetProperty(NavigationPropName);
            var uiProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (dbProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiProp == null) throw new ArgumentException(null, nameof(uiInstance));

            // Clear,Addメソッドはジェネリック型の方のICollectionにしかないのでリフレクションを使って呼び出す
            var clear = dbProp.PropertyType.GetMethod(nameof(ICollection<object>.Clear));
            var add = dbProp.PropertyType.GetMethod(nameof(ICollection<object>.Add));
            if (clear == null) throw new ArgumentException(null, nameof(dbInstance));
            if (add == null) throw new ArgumentException(null, nameof(dbInstance));

            var dbChildren = dbProp.GetValue(dbInstance)!;
            clear.Invoke(dbChildren, Array.Empty<object>());

            var childAggregate = GetChildAggregates().Single();
            var uiChildren = (IEnumerable)uiProp.GetValue(uiInstance)!;
            foreach (var uiChild in uiChildren) {
                var dbChild = context.CreateInstance(childAggregate.ToDbEntity().CSharpTypeName);
                childAggregate.MapUiToDb(uiChild, dbChild, context);
                add.Invoke(dbChildren, new[] { dbChild });
            }
        }

        private string NavigationPropName => DisplayName;
        private string InstanceModelPropName => DisplayName;

        internal override IEnumerable<RenderedProperty> ToDbEntityMember() {
            // ナビゲーションプロパティ
            var childDbEntity = GetChildAggregates().Single().ToDbEntity();
            yield return new NavigationProperty {
                Virtual = true,
                CSharpTypeName = $"ICollection<{childDbEntity.CSharpTypeName}>",
                PropertyName = NavigationPropName,
                Initializer = $"new HashSet<{childDbEntity.CSharpTypeName}>()",
                OnModelCreating = new OnModelCreatingDTO {
                    Multiplicity = OnModelCreatingDTO.E_Multiplicity.HasManyWithOne,
                    OpponentName = Aggregate.PARENT_NAVIGATION_PROPERTY_NAME,
                    ForeignKeys = childDbEntity.PrimaryKeys.Where(pk => pk is RenderedParentPkProperty),
                    OnDelete = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade,
                },
                TypeScriptTypeName = string.Empty, // 不要なプロパティ
            };
        }

        internal override IEnumerable<RenderedProperty> ToInstanceModelMember() {
            var child = GetChildAggregates().Single().ToUiInstanceClass();
            yield return new RenderedProperty {
                TypeScriptTypeName = $"{child.TypeScriptTypeName}[]",
                CSharpTypeName = $"List<{child.CSharpTypeName}>",
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchConditionMember() {
            yield break;
        }

        internal override IEnumerable<RenderedProperty> ToSearchResultMember() {
            yield break;
        }

        internal const string JSON_KEY = "children";
        internal override MemberJson ToJson() {
            return new MemberJson {
                Kind = JSON_KEY,
                Name = this.DisplayName,
                Children = this.GetChildAggregates().Single().ToJson(),
                IsPrimary = this.IsPrimary ? true : null,
            };
        }
    }
}
