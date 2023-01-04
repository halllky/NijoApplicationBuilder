using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;

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
            yield return new DbColumn {
                CSharpTypeName = "int?",
                PropertyName = Name,
            };
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

        /// <summary>アンダースコア2連だと ArgumentException: The name of an HTML field cannot be null or empty... になるので</summary>
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
