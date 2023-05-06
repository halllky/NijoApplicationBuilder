using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.CodeRendering.EFCore;
using HalApplicationBuilder.Runtime;
using HalApplicationBuilder.Serialized;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace HalApplicationBuilder.Core.MemberImpl
{
    internal class Child : AggregateMember {
        internal Child(Config config, string displayName, bool isPrimary, Aggregate owner, IAggregateDefine childType) : base(config, displayName, isPrimary, owner) {
            _childType = childType;
        }

        private readonly IAggregateDefine _childType;

        private string NavigationPropName => DisplayName;
        private string SearchConditionPropName => DisplayName;
        private string SearchResultPropName(RenderedProperty childProp) {
            // 親子の間でメンバー名が重複する場合の考慮
            var path = GetMemberPath().Path;
            return path.Any()
                ? $"{string.Join("_", path.Select(m => m.DisplayName))}_{childProp.PropertyName}"
                : childProp.PropertyName;
        }
        private string InstanceModelPropName => DisplayName;

        internal override IEnumerable<Aggregate> GetChildAggregates() {
            yield return Aggregate.AsChild(_config, _childType, this);
        }

        internal override void BuildSearchMethod(SearchMethodDTO method) {
            // TODO: SELECT句の組み立て
            // TODO: WHERE句の組み立て
        }

        internal override void RenderReactSearchCondition(RenderingContext context) {
            var nested = context.Nest(SearchConditionPropName);
            GetChildAggregates().Single().RenderReactSearchCondition(nested);
        }
        internal override void RenderReactComponent(RenderingContext context) {
            var nested = context.Nest(SearchConditionPropName);
            GetChildAggregates().Single().RenderReactComponent(nested);
        }

        internal override object? GetInstanceKeyFromDbInstance(object dbInstance) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }
        internal override void MapInstanceKeyToDbInstance(object? instanceKey, object dbInstance) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override object? GetInstanceKeyFromUiInstance(object uiInstance) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override object? GetInstanceKeyFromSearchResult(object searchResult) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override object? GetInstanceKeyFromAutoCompleteItem(object autoCompelteItem) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapInstanceKeyToUiInstance(object? instanceKey, object uiInstance) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapInstanceKeyToSearchResult(object? instanceKey, object searchResult) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapInstanceKeyToAutoCompleteItem(object? instanceKey, object autoCompelteItem) {
            throw new InvalidOperationException($"ChildをKeyに設定することはできない"); // ここが呼ばれることはない
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance, IInstanceConvertingContext context) {
            var child = GetChildAggregates().Single();
            var dbEntity = child.ToDbEntity();

            // ナビゲーションプロパティ => UI子要素
            var navigationProp = dbInstance.GetType().GetProperty(NavigationPropName);
            var uiChildProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (navigationProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiChildProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var uiChild = uiChildProp.GetValue(uiInstance);
            if (uiChild == null) {
                uiChild = context.CreateInstance(child.ToUiInstanceClass().CSharpTypeName);
                uiChildProp.SetValue(uiInstance, uiChild);
            }
            var navigation = navigationProp.GetValue(dbInstance);
            if (navigation == null) {
                navigation = context.CreateInstance(dbEntity.CSharpTypeName);
                navigationProp.SetValue(dbInstance, navigation);
            }

            child.MapDbToUi(navigation, uiChild, context);
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance, IInstanceConvertingContext context) {
            var child = GetChildAggregates().Single();
            var dbEntity = child.ToDbEntity();

            // UI子要素 => ナビゲーションプロパティ
            var navigationProp = dbInstance.GetType().GetProperty(NavigationPropName);
            var uiChildProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (navigationProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiChildProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var uiChild = uiChildProp.GetValue(uiInstance);
            if (uiChild == null) {
                uiChild = context.CreateInstance(child.ToUiInstanceClass().CSharpTypeName);
                uiChildProp.SetValue(uiInstance, uiChild);
            }
            var navigation = navigationProp.GetValue(dbInstance);
            if (navigation == null) {
                navigation = context.CreateInstance(dbEntity.CSharpTypeName);
                navigationProp.SetValue(dbInstance, navigation);
            }

            child.MapUiToDb(uiChild, navigation, context);
        }

        internal override IEnumerable<RenderedProperty> ToDbEntityMember() {
            // ナビゲーションプロパティ
            var childDbEntity = GetChildAggregates().Single().ToDbEntity();
            yield return new NavigationProperty {
                Virtual = true,
                CSharpTypeName = childDbEntity.CSharpTypeName,
                PropertyName = NavigationPropName,
                Initializer = null,
                OnModelCreating = new OnModelCreatingDTO {
                    Multiplicity = OnModelCreatingDTO.E_Multiplicity.HasOneWithOne,
                    OpponentName = Aggregate.PARENT_NAVIGATION_PROPERTY_NAME,
                    ForeignKeys = childDbEntity.PrimaryKeys.Where(pk => pk is RenderedParentPkProperty),
                    OnDelete = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade,
                },
            };
        }

        internal override IEnumerable<RenderedProperty> ToInstanceModelMember() {
            var childClass = GetChildAggregates().Single().ToUiInstanceClass();
            yield return new RenderedProperty {
                CSharpTypeName = childClass.CSharpTypeName,
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchConditionMember() {
            var childClass = GetChildAggregates().Single().ToSearchConditionClass();
            yield return new RenderedProperty {
                CSharpTypeName = childClass.CSharpTypeName,
                PropertyName = SearchConditionPropName,
                Initializer = "new()",
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchResultMember() {
            foreach (var childProp in GetChildAggregates().Single().ToSearchResultClass().Properties) {
                yield return new RenderedProperty {
                    PropertyName = SearchResultPropName(childProp),
                    CSharpTypeName = childProp.CSharpTypeName,
                    Initializer = childProp.Initializer,
                };
            }
        }

        internal const string JSON_KEY = "child";
        internal override MemberJson ToJson() {
            return new MemberJson {
                Kind = JSON_KEY,
                Name = this.DisplayName,
                Child = this.GetChildAggregates().Single().ToJson(),
                IsPrimary = this.IsPrimary ? true : null,
            };
        }
    }
}
