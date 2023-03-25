using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.Runtime;
using HalApplicationBuilder.Serialized;

namespace HalApplicationBuilder.Core.MemberImpl {
    internal class Variation : AggregateMember {
        internal Variation(Config config, string displayName, bool isPrimary, Aggregate owner, IEnumerable<KeyValuePair<int, IAggregateDefine>> variations) : base(config, displayName, isPrimary, owner) {
            if (!variations.Any())
                throw new InvalidOperationException($"{displayName} の派生型が1つも定義されていない");

            _variations = variations;
        }

        private readonly IEnumerable<KeyValuePair<int, IAggregateDefine>> _variations;

        private IReadOnlyDictionary<int, Aggregate> GetVariations() {
            //var childType = _underlyingProp.PropertyType.GetGenericArguments()[0];
            //var attrs = _underlyingProp.GetCustomAttributes<VariationAttribute>();

            //// バリエーションが存在するかチェック
            //if (!attrs.Any())
            //    throw new InvalidOperationException($"{childType.Name} の派生型が定義されていない");

            //// 型の妥当性チェック
            //var cannotAssignable = attrs.Where(x => !childType.IsAssignableFrom(x.Type)).ToArray();
            //if (cannotAssignable.Any()) {
            //    var typeNames = string.Join(", ", cannotAssignable.Select(x => x.Type.Name));
            //    throw new InvalidOperationException($"{childType.Name} の派生型でない: {typeNames}");
            //}

            //return attrs.ToDictionary(attr => attr.Key, attr => Aggregate.AsChild(_config, attr.Type, this));

            return _variations.ToDictionary(v => v.Key, v => Aggregate.AsChild(_config, v.Value, this));
        }

        private string DbPropName => DisplayName;
        private string NavigationPropName(RenderedEFCoreEntity variationDbEntity) => $"{DbPropName}__{variationDbEntity.ClassName}";

        /// <summary>アンダースコア2連だと ArgumentException: The name of an HTML field cannot be null or empty... になる</summary>
        private string SearchConditionPropName(KeyValuePair<int, Aggregate> variation) => $"{DisplayName}_{variation.Value.GetCSharpSafeName()}";
        private string SearchResultPropName => DisplayName;
        private string InstanceModelTypeSwitchPropName => DisplayName;
        private string InstanceModelTypeDetailPropName(KeyValuePair<int, Aggregate> variation) => $"{DisplayName}_{variation.Value.GetCSharpSafeName()}";


        internal override IEnumerable<Aggregate> GetChildAggregates() {
            return GetVariations().Values;
        }

        internal override void BuildSearchMethod(SearchMethodDTO method) {
            method.SelectClause.Add($"{SearchResultPropName} = {method.SelectLambdaVarName}.{SearchResultPropName},");

            // TODO: WHERE句の組み立て
        }

        internal override void RenderMvcSearchConditionView(RenderingContext context) {
            foreach (var variation in GetVariations()) {
                var key = context.ObjectPath.Nest(SearchConditionPropName(variation)).AspForPath;

                context.Template.WriteLine($"<label>");
                context.Template.WriteLine($"    <input type=\"checkbox\" asp-for=\"{key}\">");
                context.Template.WriteLine($"    {variation.Value.GetDisplayName()}");
                context.Template.WriteLine($"</label>");
            }
        }

        internal override void RenderAspNetMvcPartialView(RenderingContext context) {

            foreach (var variation in GetVariations()) {
                var type = context.ObjectPath.Nest(InstanceModelTypeSwitchPropName).AspForPath;
                var nested = context.Nest(SearchConditionPropName(variation));

                context.Template.WriteLine($"<div>");
                context.Template.WriteLine($"    <label>");
                context.Template.WriteLine($"        <input type=\"radio\" asp-for=\"{type}\" value=\"{variation.Key}\" />");
                context.Template.WriteLine($"        {variation.Value.GetDisplayName()}");
                context.Template.WriteLine($"    </label>");

                context.Template.PushIndent($"    ");
                variation.Value.RenderAspNetMvcPartialView(nested);
                context.Template.PopIndent();

                context.Template.WriteLine($"</div>");
            }
        }

        internal override object? GetInstanceKeyFromDbInstance(object dbInstance) {
            var prop = dbInstance.GetType().GetProperty(DbPropName);
            if (prop == null) throw new ArgumentException(null, nameof(dbInstance));
            return prop.GetValue(dbInstance);
        }
        internal override void MapInstanceKeyToDbInstance(object? instanceKey, object dbInstance) {
            var prop = dbInstance.GetType().GetProperty(DbPropName);
            if (prop == null) throw new ArgumentException(null, nameof(dbInstance));
            prop.SetValue(dbInstance, instanceKey);
        }

        internal override object? GetInstanceKeyFromUiInstance(object uiInstance) {
            var prop = uiInstance.GetType().GetProperty(InstanceModelTypeSwitchPropName);
            if (prop == null) throw new ArgumentException(null, nameof(uiInstance));
            return prop.GetValue(uiInstance);
        }
        internal override void MapInstanceKeyToUiInstance(object? instanceKey, object uiInstance) {
            var prop = uiInstance.GetType().GetProperty(InstanceModelTypeSwitchPropName);
            if (prop == null) throw new ArgumentException(null, nameof(uiInstance));
            prop.SetValue(uiInstance, instanceKey);
        }

        internal override object? GetInstanceKeyFromSearchResult(object searchResult) {
            var prop = searchResult.GetType().GetProperty(SearchResultPropName);
            if (prop == null) throw new ArgumentException(null, nameof(searchResult));
            return prop.GetValue(searchResult);
        }
        internal override void MapInstanceKeyToSearchResult(object? instanceKey, object searchResult) {
            var prop = searchResult.GetType().GetProperty(SearchResultPropName);
            if (prop == null) throw new ArgumentException(null, nameof(searchResult));
            prop.SetValue(searchResult, instanceKey);
        }

        internal override object? GetInstanceKeyFromAutoCompleteItem(object autoCompelteItem) {
            var prop = autoCompelteItem.GetType().GetProperty(DbPropName);
            if (prop == null) throw new ArgumentException(null, nameof(autoCompelteItem));
            return prop.GetValue(autoCompelteItem);
        }
        internal override void MapInstanceKeyToAutoCompleteItem(object? instanceKey, object autoCompelteItem) {
            var prop = autoCompelteItem.GetType().GetProperty(DbPropName);
            if (prop == null) throw new ArgumentException(null, nameof(autoCompelteItem));
            prop.SetValue(autoCompelteItem, instanceKey);
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance, IInstanceConvertingContext context) {
            // 区分値
            var dbIntProp = dbInstance.GetType().GetProperty(DbPropName);
            var uiIntProp = uiInstance.GetType().GetProperty(InstanceModelTypeSwitchPropName);
            if (dbIntProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiIntProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var value = dbIntProp.GetValue(dbInstance);
            uiIntProp.SetValue(uiInstance, value);

            // ナビゲーションプロパティ => UI子要素
            foreach (var variation in GetVariations()) {
                var dbEntity = variation.Value.ToDbEntity();
                var navigationProp = dbInstance.GetType().GetProperty(NavigationPropName(dbEntity));
                var uiChildProp = uiInstance.GetType().GetProperty(InstanceModelTypeDetailPropName(variation));
                if (navigationProp == null) throw new ArgumentException(null, nameof(dbInstance));
                if (uiChildProp == null) throw new ArgumentException(null, nameof(uiInstance));

                var uiChild = uiChildProp.GetValue(uiInstance);
                if (uiChild == null) {
                    uiChild = context.CreateInstance(variation.Value.ToUiInstanceClass().CSharpTypeName);
                    uiChildProp.SetValue(uiInstance, uiChild);
                }
                var navigation = navigationProp.GetValue(dbInstance);
                if (navigation == null) {
                    navigation = context.CreateInstance(dbEntity.CSharpTypeName);
                    navigationProp.SetValue(dbInstance, navigation);
                }

                variation.Value.MapDbToUi(navigation, uiChild, context);
            }
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance, IInstanceConvertingContext context) {
            // 区分値
            var dbIntProp = dbInstance.GetType().GetProperty(DbPropName);
            var uiIntProp = uiInstance.GetType().GetProperty(InstanceModelTypeSwitchPropName);
            if (dbIntProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiIntProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var value = uiIntProp.GetValue(uiInstance);
            dbIntProp.SetValue(dbInstance, value);

            // UI子要素 => ナビゲーションプロパティ
            foreach (var variation in GetVariations()) {
                var dbEntity = variation.Value.ToDbEntity();
                var navigationProp = dbInstance.GetType().GetProperty(NavigationPropName(dbEntity));
                var uiChildProp = uiInstance.GetType().GetProperty(InstanceModelTypeDetailPropName(variation));
                if (navigationProp == null) throw new ArgumentException(null, nameof(dbInstance));
                if (uiChildProp == null) throw new ArgumentException(null, nameof(uiInstance));

                var uiChild = uiChildProp.GetValue(uiInstance);
                if (uiChild == null) {
                    uiChild = context.CreateInstance(variation.Value.ToUiInstanceClass().CSharpTypeName);
                    uiChildProp.SetValue(uiInstance, uiChild);
                }
                var navigation = navigationProp.GetValue(dbInstance);
                if (navigation == null) {
                    navigation = context.CreateInstance(dbEntity.CSharpTypeName);
                    navigationProp.SetValue(dbInstance, navigation);
                }

                variation.Value.MapUiToDb(uiChild, navigation, context);
            }
        }

        internal override IEnumerable<RenderedProperty> ToDbEntityMember() {
            // 区分値
            yield return new RenderedProperty {
                Virtual = false,
                CSharpTypeName = "int?",
                PropertyName = DbPropName,
            };
            // ナビゲーションプロパティ
            foreach (var variation in GetChildAggregates()) {
                var variationDbEntity = variation.ToDbEntity();
                yield return new NavigationProperty {
                    Virtual = true,
                    CSharpTypeName = variationDbEntity.CSharpTypeName,
                    PropertyName = NavigationPropName(variationDbEntity),
                    OnModelCreating = new OnModelCreatingDTO {
                        Multiplicity = OnModelCreatingDTO.E_Multiplicity.HasOneWithOne,
                        OpponentName = Aggregate.PARENT_NAVIGATION_PROPERTY_NAME,
                        ForeignKeys = variationDbEntity.PrimaryKeys.Where(pk => pk is RenderedParentPkProperty),
                        OnDelete = Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade,
                    },
                };
            }
        }

        internal override IEnumerable<RenderedProperty> ToInstanceModelMember() {
            // 区分値
            yield return new RenderedProperty {
                CSharpTypeName = "int?",
                PropertyName = InstanceModelTypeSwitchPropName,
            };
            // 各区分の詳細値
            foreach (var variation in GetVariations()) {
                yield return new RenderedProperty {
                    CSharpTypeName = variation.Value.ToUiInstanceClass().CSharpTypeName,
                    PropertyName = InstanceModelTypeDetailPropName(variation),
                    Initializer = "new()",
                };
            }
        }

        internal override IEnumerable<RenderedProperty> ToSearchConditionMember() {
            foreach (var variation in GetVariations()) {
                yield return new RenderedProperty {
                    PropertyName = SearchConditionPropName(variation),
                    CSharpTypeName = "bool",
                    Initializer = "true",
                };
            }
        }

        internal override IEnumerable<RenderedProperty> ToSearchResultMember() {
            yield return new RenderedProperty {
                CSharpTypeName = "int?",
                PropertyName = SearchResultPropName,
            };
        }

        internal const string JSON_KEY = "variation";
        internal override MemberJson ToJson() {
            return new MemberJson {
                Kind = JSON_KEY,
                Name = this.DisplayName,
                Variations = this.GetVariations().Select(v => v.Value.ToJson()).ToArray(),
                IsPrimary = this.IsPrimary ? true : null,
            };
        }
    }
}
