using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class Variation : AggregateMember {
        internal Variation(Config config, PropertyInfo propertyInfo, Aggregate owner) : base(config, propertyInfo, owner) { }

        private IReadOnlyDictionary<int, Aggregate> GetVariations() {
            var childType = _underlyingProp.PropertyType.GetGenericArguments()[0];
            var attrs = _underlyingProp.GetCustomAttributes<VariationAttribute>();

            // バリエーションが存在するかチェック
            if (!attrs.Any())
                throw new InvalidOperationException($"{childType.Name} の派生型が定義されていない");

            // 型の妥当性チェック
            var cannotAssignable = attrs.Where(x => !childType.IsAssignableFrom(x.Type)).ToArray();
            if (cannotAssignable.Any()) {
                var typeNames = string.Join(", ", cannotAssignable.Select(x => x.Type.Name));
                throw new InvalidOperationException($"{childType.Name} の派生型でない: {typeNames}");
            }

            return attrs.ToDictionary(attr => attr.Key, attr => Aggregate.AsChild(_config, attr.Type, this));
        }

        private string DbPropName => _underlyingProp.Name;
        private string NavigationPropName(RenderedEFCoreEntity variationDbEntity) => $"{DbPropName}__{variationDbEntity.ClassName}";

        /// <summary>アンダースコア2連だと ArgumentException: The name of an HTML field cannot be null or empty... になる</summary>
        private string SearchConditionPropName(KeyValuePair<int, Aggregate> variation) => $"{_underlyingProp.Name}_{variation.Value.Name}";
        private string SearchResultPropName => _underlyingProp.Name;
        private string InstanceModelTypeSwitchPropName => _underlyingProp.Name;
        private string InstanceModelTypeDetailPropName(KeyValuePair<int, Aggregate> variation) => $"{_underlyingProp.Name}_{variation.Value.Name}";


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
                context.Template.WriteLine($"    {variation.Value.Name}");
                context.Template.WriteLine($"</label>");
            }
        }

        internal override IEnumerable<string> GetInstanceKeysFromInstanceModel(object uiInstance)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<string> GetInstanceKeysFromSearchResult(object searchResult)
        {
            throw new NotImplementedException();
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance)
        {
            throw new NotImplementedException();
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<RenderedProerty> ToDbEntityMember() {
            // 区分値
            yield return new RenderedProerty {
                Virtual = false,
                CSharpTypeName = "int?",
                PropertyName = DbPropName,
            };
            // ナビゲーションプロパティ
            foreach (var variation in GetChildAggregates()) {
                var variationDbEntity = variation.ToDbEntity();
                yield return new NavigationProerty {
                    Virtual = true,
                    CSharpTypeName = variationDbEntity.CSharpTypeName,
                    PropertyName = NavigationPropName(variationDbEntity),
                    IsManyToOne = false,
                    IsPrincipal = true,
                    OpponentName = Aggregate.PARENT_NAVIGATION_PROPERTY_NAME,
                };
            }
        }

        internal override IEnumerable<RenderedProerty> ToInstanceModelMember() {
            // 区分値
            yield return new RenderedProerty {
                CSharpTypeName = "int?",
                PropertyName = InstanceModelTypeSwitchPropName,
            };
            // 各区分の詳細値
            foreach (var variation in GetVariations()) {
                yield return new RenderedProerty {
                    CSharpTypeName = variation.Value.ToUiInstanceClass().CSharpTypeName,
                    PropertyName = InstanceModelTypeDetailPropName(variation),
                    Initializer = "new()",
                };
            }
        }

        internal override IEnumerable<RenderedProerty> ToSearchConditionMember() {
            foreach (var variation in GetVariations()) {
                yield return new RenderedProerty {
                    PropertyName = SearchConditionPropName(variation),
                    CSharpTypeName = "bool",
                    Initializer = "true",
                };
            }
        }

        internal override IEnumerable<RenderedProerty> ToSearchResultMember() {
            yield return new RenderedProerty {
                CSharpTypeName = "int?",
                PropertyName = SearchResultPropName,
            };
        }
    }
}
