using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.Core.Members {
    public class Variation : AggregateMemberBase {
        internal Variation(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => false;

        private Dictionary<int, Aggregate> _variations;
        public IReadOnlyDictionary<int, Aggregate> Variations {
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

        internal string DbPropName => Name;
        internal string NavigationPropName(DbEntity variationDbEntity) => $"{DbPropName}__{variationDbEntity.ClassName}";

        /// <summary>アンダースコア2連だと ArgumentException: The name of an HTML field cannot be null or empty... になる</summary>
        private string SearchConditionPropName(KeyValuePair<int, Aggregate> variation) => $"{Name}_{variation.Value.Name}";
        internal string SearchResultPropName => Name;
        internal string InstanceModelTypeSwitchPropName => Name;
        internal string InstanceModelTypeDetailPropName(KeyValuePair<int, Aggregate> variation) => $"{Name}_{variation.Value.Name}";

        public override IEnumerable<string> GetInvalidErrors() {
            if (IsPrimaryKey) yield return $"{Name} は子要素のため主キーに設定できません。";
        }

        private protected override void Accept(IMemberVisitor visitor) {
            visitor.Visit(this);
        }
    }
}
