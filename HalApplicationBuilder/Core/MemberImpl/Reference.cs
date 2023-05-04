using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.CodeRendering.EFCore;
using HalApplicationBuilder.Runtime;
using HalApplicationBuilder.Serialized;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HalApplicationBuilder.Core.MemberImpl
{
    internal class Reference : AggregateMember {
        internal Reference(Config config, string displayName, bool isPrimary, bool isNullable, Aggregate owner, Func<Aggregate> getRefTarget) : base(config, displayName, isPrimary, owner) {
            _getRefTarget = getRefTarget;
            _isNullable = isNullable;
        }

        private readonly Func<Aggregate> _getRefTarget;
        private readonly bool _isNullable;

        internal Aggregate GetRefTarget() {
            return _getRefTarget.Invoke();
        }

        internal override IEnumerable<Aggregate> GetChildAggregates()
        {
            yield break;
        }

        internal override void BuildSearchMethod(SearchMethodDTO method) {
            // TODO: SELECT句の組み立て
            // TODO: WHERE句の組み立て
        }

        internal override void RenderReactSearchCondition(RenderingContext context) {
            context.Template.WriteLine($"<div>");
            context.Template.WriteLine($"    TODO autocomplete");
            context.Template.WriteLine($"</div>");
        }
        internal override void RenderReactComponent(RenderingContext context) {
            context.Template.WriteLine($"<div>");
            context.Template.WriteLine($"    TODO autocomplete");
            context.Template.WriteLine($"</div>");
        }

        internal override object? GetInstanceKeyFromDbInstance(object dbInstance) {
            var objType = dbInstance.GetType();
            var foreignKeyValues = GetRefTarget()
                .ToDbEntity()
                .PrimaryKeys
                .Select(fk => {
                    var prop = objType.GetProperty(ForeignKeyColumnPropName(fk));
                    if (prop == null) throw new ArgumentException(null, nameof(dbInstance));
                    return prop.GetValue(dbInstance);
                })
                .ToArray();
            return foreignKeyValues;
        }
        internal override void MapInstanceKeyToDbInstance(object? instanceKey, object dbInstance) {
            var objType = dbInstance.GetType();
            var objArray = (object?[])instanceKey!;
            var foreignKeys = GetRefTarget().ToDbEntity().PrimaryKeys.ToArray();

            for (int i = 0; i < foreignKeys.Length; i++) {
                var prop = objType.GetProperty(ForeignKeyColumnPropName(foreignKeys[i]));
                if (prop == null) throw new ArgumentException(null, nameof(dbInstance));
                prop.SetValue(dbInstance, objArray[i]);
            }
        }

        internal override object? GetInstanceKeyFromUiInstance(object uiInstance) {
            var prop = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (prop == null) throw new ArgumentException(null, nameof(uiInstance));

            var refDto = (Runtime.ReferenceDTO)prop.GetValue(uiInstance)!;
            var instanceKey = Runtime.InstanceKey
                .FromSerializedString(refDto.InstanceKey)
                ?? GetRefTarget().CreateEmptyInstanceKey();
            return instanceKey.ObjectValue;
        }
        internal override void MapInstanceKeyToUiInstance(object? instanceKey, object uiInstance) {
            var prop = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (prop == null) throw new ArgumentException(null, nameof(uiInstance));

            var refDto = (Runtime.ReferenceDTO)prop.GetValue(uiInstance)!;
            var objArray = (object?[]?)instanceKey;
            var instanceKeyObj = Runtime.InstanceKey.FromObjects(objArray ?? Enumerable.Empty<object?>());

            refDto.InstanceKey = instanceKeyObj.StringValue;
        }

        internal override object? GetInstanceKeyFromSearchResult(object searchResult) {
            var objType = searchResult.GetType();
            var foreignKeyValues = GetRefTarget()
                .ToDbEntity()
                .PrimaryKeys
                .Select(fk => {
                    var prop = objType.GetProperty(SearchResultForeignKeyPropName(fk));
                    if (prop == null) throw new ArgumentException(null, nameof(searchResult));
                    return prop.GetValue(searchResult);
                })
                .ToArray();
            return foreignKeyValues;
        }
        internal override void MapInstanceKeyToSearchResult(object? instanceKey, object searchResult) {
            var objType = searchResult.GetType();
            var objArray = (object?[])instanceKey!;
            var foreignKeys = GetRefTarget().ToDbEntity().PrimaryKeys.ToArray();

            for (int i = 0; i < foreignKeys.Length; i++) {
                var prop = objType.GetProperty(SearchResultForeignKeyPropName(foreignKeys[i]));
                if (prop == null) throw new ArgumentException(null, nameof(searchResult));
                prop.SetValue(searchResult, objArray[i]);
            }
        }

        internal override object? GetInstanceKeyFromAutoCompleteItem(object autoCompelteItem) {
            var navigationProp = autoCompelteItem.GetType().GetProperty(NavigationPropName);
            if (navigationProp == null) throw new ArgumentException(null, nameof(autoCompelteItem));

            var navigation = navigationProp.GetValue(autoCompelteItem);
            if (navigation == null) throw new ArgumentException(null, nameof(autoCompelteItem));

            var navigationType = navigation.GetType();
            var foreignKeyValues = GetRefTarget()
                .ToDbEntity()
                .PrimaryKeys
                .Select(fk => {
                    var prop = navigationType.GetProperty(SearchResultForeignKeyPropName(fk));
                    if (prop == null) throw new ArgumentException(null, nameof(autoCompelteItem));
                    return prop.GetValue(navigation);
                })
                .ToArray();
            return foreignKeyValues;
        }
        internal override void MapInstanceKeyToAutoCompleteItem(object? instanceKey, object autoCompelteItem) {
            var navigationProp = autoCompelteItem.GetType().GetProperty(NavigationPropName);
            if (navigationProp == null) throw new ArgumentException(null, nameof(autoCompelteItem));

            var navigation = navigationProp.GetValue(autoCompelteItem);
            if (navigation == null) throw new ArgumentException(null, nameof(autoCompelteItem));

            var navigationType = navigation.GetType();
            var foreignKeys = GetRefTarget().ToDbEntity().PrimaryKeys.ToArray();

            var objArray = (object?[])instanceKey!;

            for (int i = 0; i < foreignKeys.Length; i++) {
                var prop = navigationType.GetProperty(SearchResultForeignKeyPropName(foreignKeys[i]));
                if (prop == null) throw new ArgumentException(null, nameof(autoCompelteItem));
                prop.SetValue(navigation, objArray[i]);
            }
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance, IInstanceConvertingContext context) {
            var navigationProp = dbInstance.GetType().GetProperty(NavigationPropName);
            var refDtoProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (navigationProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (refDtoProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var refDto = (ReferenceDTO)refDtoProp.GetValue(uiInstance)!;
            var navigation = navigationProp.GetValue(dbInstance);

            if (navigation == null) {
                refDto.InstanceKey = GetRefTarget().CreateEmptyInstanceKey().StringValue;
                refDto.InstanceName = string.Empty;
            } else {
                var refTarget = GetRefTarget();
                refDto.InstanceKey = refTarget.CreateInstanceKeyFromDbInstnace(navigation).StringValue;
                refDto.InstanceName = Runtime.InstanceName.Create(navigation, refTarget).Value;
            }
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance, IInstanceConvertingContext context) {
            var refDtoProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (refDtoProp == null) throw new ArgumentException(null, nameof(uiInstance));
            var refDto = (ReferenceDTO)refDtoProp.GetValue(uiInstance)!;
            var instanceKey = Runtime.InstanceKey
                .FromSerializedString(refDto.InstanceKey)
                ?? GetRefTarget().CreateEmptyInstanceKey();

            this.MapInstanceKeyToDbInstance(instanceKey.ObjectValue, dbInstance);
        }

        internal override IEnumerable<RenderedProperty> ToDbEntityMember() {
            var refTarget = GetRefTarget();
            var refTargetDbEntity = refTarget.ToDbEntity();

            // 参照先DBの主キー
            var foreignKeys = refTargetDbEntity.PrimaryKeys
                .Select(foreignKey => new RenderedProperty {
                    CSharpTypeName = foreignKey.CSharpTypeName,
                    PropertyName = ForeignKeyColumnPropName(foreignKey),
                    Nullable = _isNullable,

                    TypeScriptTypeName = foreignKey.TypeScriptTypeName,
                })
                .ToArray();
            foreach (var foreignKey in foreignKeys) {
                yield return foreignKey;
            }

            // ナビゲーションプロパティ
            var relation = new ReferenceRelation(this);
            yield return new NavigationProperty {
                Virtual = true,
                CSharpTypeName = refTargetDbEntity.CSharpTypeName,
                PropertyName = NavigationPropName,
                OnModelCreating = new OnModelCreatingDTO {
                    Multiplicity = OnModelCreatingDTO.E_Multiplicity.HasOneWithMany,
                    OpponentName = relation.GetEFCoreEntiyHavingOnlyReferredNavigationProp().Properties.Single().PropertyName,
                    ForeignKeys = foreignKeys,
                    OnDelete = Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict,
                },

                TypeScriptTypeName = string.Empty, // 不要なプロパティ
            };
        }

        private string ForeignKeyColumnPropName(RenderedProperty fk) => $"{DisplayName}_{fk.PropertyName}";
        private string NavigationPropName => DisplayName;
        private string SearchConditonPropName => DisplayName;
        private string SearchResultInstanceNamePropName => DisplayName + "_名称";
        private string SearchResultForeignKeyPropName(RenderedProperty fk) => $"{DisplayName}_{fk.PropertyName}";
        private string InstanceModelPropName => DisplayName;

        internal override IEnumerable<RenderedProperty> ToInstanceModelMember() {
            yield return new RenderedProperty {
                CSharpTypeName = typeof(Runtime.ReferenceDTO).FullName!,
                PropertyName = InstanceModelPropName,
                Initializer = $"new() {{ {nameof(Runtime.ReferenceDTO.AggreageteGuid)} = new Guid(\"{GetRefTarget().GetGuid()}\") }}",

                TypeScriptTypeName = "string", // TSオブジェクトにはinstanceKeyだけを保持する。名称はReactQueryでクライアント側ローカルキャッシュから取得させる
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchConditionMember() {
            yield return new RenderedProperty {
                CSharpTypeName = typeof(Runtime.ReferenceDTO).FullName!,
                PropertyName = SearchConditonPropName,
                Initializer = $"new() {{ {nameof(Runtime.ReferenceDTO.AggreageteGuid)} = new Guid(\"{GetRefTarget().GetGuid()}\") }}",

                TypeScriptTypeName = "string", // TSオブジェクトにはinstanceKeyだけを保持する。名称はReactQueryでクライアント側ローカルキャッシュから取得させる
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchResultMember() {
            // 参照先の主キー
            foreach (var fk in GetRefTarget().ToDbEntity().PrimaryKeys) {
                yield return new RenderedProperty {
                    CSharpTypeName = fk.CSharpTypeName,
                    PropertyName = SearchResultForeignKeyPropName(fk),

                    TypeScriptTypeName = fk.TypeScriptTypeName,
                };
            }

            // 参照先のインスタンス名
            yield return new RenderedProperty {
                CSharpTypeName = "string",
                PropertyName = SearchResultInstanceNamePropName,

                TypeScriptTypeName = "string",
            };
        }

        internal const string JSON_KEY = "ref";
        internal override MemberJson ToJson() {
            return new MemberJson {
                Kind = JSON_KEY,
                Name = this.DisplayName,
                RefTarget = this.GetRefTarget().GetUniquePath(),
                IsPrimary = this.IsPrimary ? true : null,
                IsNullable = this._isNullable,
            };
        }
    }
}
