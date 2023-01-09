using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;
using HalApplicationBuilder.Runtime;

namespace HalApplicationBuilder.Impl {
    public class Variation : AggregateMemberBase {
        internal Variation(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => false;

        private Dictionary<int, Aggregate> _variations;
        private IReadOnlyDictionary<int, Aggregate> Variations {
            get {
                if (_variations == null) {
                    var childType = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    var attrs = UnderlyingPropertyInfo.GetCustomAttributes<VariationAttribute>();

                    // 型の妥当性チェック
                    var cannotAssignable = attrs.Where(x => !childType.IsAssignableFrom(x.Type)).ToArray();
                    if (cannotAssignable.Any()) {
                        var typeNames = string.Join(", ", cannotAssignable.Select(x => x.Type.Name));
                        throw new InvalidOperationException($"{childType.Name} の派生型でない: {typeNames}");
                    }

                    _variations = attrs.ToDictionary(
                        v => v.Key,
                        v => new Aggregate(v.Type, this, MemberFactory));
                }
                return _variations;
            }
        }

        public override IEnumerable<Aggregate> GetChildAggregates() {
            return Variations.Values;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            // 区分値
            yield return new DbColumn {
                CSharpTypeName = "int?",
                PropertyName = DbPropName,
            };
            // ナビゲーションプロパティ
            foreach (var variation in Variations) {
                var variationDbEntity = DbSchema.GetDbEntity(variation.Value);
                yield return new DbColumn {
                    Virtual = true,
                    CSharpTypeName = variationDbEntity.RuntimeFullName,
                    PropertyName = NavigationPropName(variationDbEntity),
                };
            }
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels() {
            foreach (var variation in Variations) {
                yield return new MvcModelProperty {
                    PropertyName = SearchConditionPropName(variation),
                    CSharpTypeName = "bool",
                    Initializer = "true",
                };
            }
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = "string",
                PropertyName = SearchResultPropName,
            };
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            // 区分値
            yield return new MvcModelProperty {
                CSharpTypeName = "int?",
                PropertyName = InstanceModelTypeSwitchPropName,
            };
            // 各区分の詳細値
            foreach (var child in Variations) {
                yield return new MvcModelProperty {
                    CSharpTypeName = ViewModelProvider.GetInstanceModel(child.Value).RuntimeFullName,
                    PropertyName = InstanceModelTypeDetailPropName(child),
                    Initializer = "new()",
                };
            }
        }

        private string DbPropName => Name;
        private string NavigationPropName(DbEntity variationDbEntity) => $"{DbPropName}__{variationDbEntity.ClassName}";

        /// <summary>アンダースコア2連だと ArgumentException: The name of an HTML field cannot be null or empty... になる</summary>
        private string SearchConditionPropName(KeyValuePair<int, Aggregate> variation) => $"{Name}_{variation.Value.Name}";
        private string SearchResultPropName => Name;
        private string InstanceModelTypeSwitchPropName => Name;
        private string InstanceModelTypeDetailPropName(KeyValuePair<int, Aggregate> variation) => $"{Name}_{variation.Value.Name}";

        public override string RenderSearchConditionView(ViewRenderingContext context) {
            var childrenViews = CreateSearchConditionModels()
                .Select(child => {
                    var nested = context.Nest(child.PropertyName);
                    var template = new VariationSearchConditionTemplate {
                        PropertyName = child.PropertyName,
                        AspFor = nested.AspForPath,
                    };
                    return template.TransformText();
                });
            return string.Join(Environment.NewLine, childrenViews);
        }

        public override string RenderSearchResultView(ViewRenderingContext context) {
            var nested = context.Nest(SearchResultPropName);
            return $"<span>@{nested.Path}</span>";
        }

        public override string RenderInstanceView(ViewRenderingContext context) {
            var nested1 = context.Nest(InstanceModelTypeSwitchPropName); // 区分値(ラジオボタン用)
            var childrenViews = Variations
                .Select(child => {
                    var nested2 = context.Nest(InstanceModelTypeDetailPropName(child));
                    var template = new VariationInstanceTemplate {
                        Key = child.Key,
                        Name = child.Value.Name,
                        RadioButtonAspFor = nested1.AspForPath,
                        ChildAggregateView = ViewModelProvider.GetInstanceModel(child.Value).Render(nested2),
                    };
                    return template.TransformText();
                });
            return string.Join(Environment.NewLine, childrenViews);
        }

        public override void MapUIToDB(object uiInstance, object dbInstance, RuntimeContext context) {
            // 区分値(int)の設定
            var dbProp = dbInstance.GetType().GetProperty(DbPropName);
            var uiProp = uiInstance.GetType().GetProperty(InstanceModelTypeSwitchPropName);
            var value = uiProp.GetValue(uiInstance);
            dbProp.SetValue(dbInstance, value);

            // Variation子要素の設定
            foreach (var variation in Variations) {
                var childUiInstance = uiInstance
                    .GetType()
                    .GetProperty(InstanceModelTypeDetailPropName(variation))
                    .GetValue(uiInstance);
                var childDbEntity = context.DbSchema
                    .GetDbEntity(variation.Value);
                var navigationProperty = dbInstance
                    .GetType()
                    .GetProperty(NavigationPropName(childDbEntity));

                var childDbInstance = navigationProperty.GetValue(dbInstance);
                if (childDbInstance != null) {
                    childDbEntity.MapUiInstanceToDbInsntace(childUiInstance, childDbInstance, context);

                } else {
                    var newChildDbInstance = childDbEntity
                        .ConvertUiInstanceToDbInstance(childUiInstance, context);

                    navigationProperty.SetValue(dbInstance, newChildDbInstance);
                }
            }
        }

        public override void MapDBToUI(object dbInstance, object uiInstance, RuntimeContext context) {
            // 区分値(int)の設定
            var dbProp = dbInstance.GetType().GetProperty(DbPropName);
            var uiProp = uiInstance.GetType().GetProperty(InstanceModelTypeSwitchPropName);
            var value = dbProp.GetValue(dbInstance);
            uiProp.SetValue(uiInstance, value);

            // Variation子要素の設定
            foreach (var variation in Variations) {
                var childDbEntity = context.DbSchema
                    .GetDbEntity(variation.Value);
                var childDbProperty = dbInstance
                    .GetType()
                    .GetProperty(NavigationPropName(childDbEntity));
                var childDbInstance = childDbProperty
                    .GetValue(dbInstance);

                if (childDbInstance != null) {
                    var childUiProperty = uiInstance
                        .GetType()
                        .GetProperty(InstanceModelTypeDetailPropName(variation));
                    var childUiInstance = childDbEntity
                        .ConvertDbInstanceToUiInstance(childDbInstance, context);

                    childUiProperty.SetValue(uiInstance, childUiInstance);
                }
            }
        }

        public override void BuildSelectStatement(SelectStatement selectStatement, object searchCondition, RuntimeContext context, string selectClausePrefix) {
            // SELECT
            var dbEntity = context.DbSchema.GetDbEntity(Owner);
            selectStatement.Select(e => {
                var table = e.GetAlias(dbEntity);
                var column = DbPropName;
                var alias = selectClausePrefix + SearchResultPropName;
                return $"{table}.{column} AS [{alias}]";
            });

            // WHERE
            var targetTypes = Variations
                .Where(variation => {
                    var prop = searchCondition.GetType().GetProperty(SearchConditionPropName(variation));
                    var value = prop.GetValue(searchCondition);
                    return value is bool b && (b == true);
                })
                .Select(variation => variation.Key)
                .ToArray();

            // - どのタイプも選択されていない場合は無条件検索とみなす
            // - すべてのタイプが選択されている場合は無条件検索とみなす
            if (targetTypes.Any() && targetTypes.Length < Variations.Count) {
                selectStatement.Where(e => {
                    var table = e.GetAlias(dbEntity);
                    var column = DbPropName;
                    return $"{table}.{column} IN ({string.Join(", ", targetTypes)})";
                });
            }
        }

        public override void MapSearchResultToUI(System.Data.Common.DbDataReader reader, object searchResult, RuntimeContext context, string selectClausePrefix) {
            var prop = searchResult.GetType().GetProperty(SearchResultPropName);
            var value = reader[SearchResultPropName];
            if (value is long key && Variations.TryGetValue((int)key, out var variation)) {
                prop.SetValue(searchResult, variation.Name);
            } else {
                prop.SetValue(searchResult, null);
            }
        }
    }

    partial class VariationSearchConditionTemplate {
        public string PropertyName { get; set; }
        public string AspFor { get; set; }
    }

    partial class VariationInstanceTemplate {
        public string RadioButtonAspFor { get; set; }
        public int Key { get; set; }
        public string Name { get; set; }
        public string ChildAggregateView { get; set; }
    }
}
