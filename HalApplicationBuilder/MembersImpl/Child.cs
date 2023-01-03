using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;
using HalApplicationBuilder.Runtime;

namespace HalApplicationBuilder.MembersImpl {
    internal class Child : AggregateMemberBase {
        public override bool IsCollection => false;

        private Dictionary<int, Aggregate> _children;
        private IReadOnlyDictionary<int, Aggregate> GetChildren() {
            if (_children == null) {
                var childType = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                var variations = UnderlyingPropertyInfo.GetCustomAttributes<VariationAttribute>();

                if (!childType.IsAbstract && !variations.Any()) {
                    // complex object
                    _children = new Dictionary<int, Aggregate> {
                        { -1, new Aggregate(childType, this, MemberFactory) },
                    };

                } else if (childType.IsAbstract && variations.Any()) {
                    // バリエーション型
                    var cannotAssignable = variations.Where(x => !childType.IsAssignableFrom(x.Type)).ToArray();
                    if (cannotAssignable.Any()) {
                        var typeNames = string.Join(", ", cannotAssignable.Select(x => x.Type.Name));
                        throw new InvalidOperationException($"{childType.Name} の派生型でない: {typeNames}");
                    }
                    _children = variations.ToDictionary(
                        v => v.Key,
                        v => new Aggregate(v.Type, this, MemberFactory));

                } else {
                    throw new InvalidOperationException($": 抽象型ならバリエーション必須、抽象型でないならバリエーション指定不可");
                }
            }
            return _children;
        }

        /// <summary>ComplexTypeかバリエーション型か</summary>
        private bool IsComplexType() {
            var children = GetChildren();
            return children.Count == 1 && children.Single().Key == -1;
        }
        /// <summary>バリエーション型なら例外</summary>
        private Aggregate ChildAggregate => IsComplexType()
            ? GetChildren().Single().Value
            : throw new InvalidOperationException($"バリエーション型なので{nameof(ChildAggregate)}プロパティ使用不可。{nameof(IsComplexType)}メソッドを使って分岐をかけてください。");

        public override IEnumerable<Aggregate> GetChildAggregates() {
            return GetChildren().Values;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            if (IsComplexType()) {
                yield break;
            } else {
                yield return new DbColumn {
                    CSharpTypeName = "int?",
                    PropertyName = Name,
                };
            }
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels(IAggregateMember member) {
            if (IsComplexType()) {
                yield return new MvcModelProperty {
                    CSharpTypeName = Schema.GetSearchConditionModel(ChildAggregate).RuntimeFullName,
                    PropertyName = Name,
                    Initializer = "new()",
                };
            } else {
                foreach (var variation in GetChildren()) {
                    yield return new MvcModelProperty {
                        PropertyName = $"{Name}_{variation.Value.Name}",
                        CSharpTypeName = "bool",
                        Initializer = "true",
                    };
                }
            }
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels(IAggregateMember member) {
            if (IsComplexType()) {
                foreach (var childProp in Schema.GetSearchResultModel(ChildAggregate).Properties) {
                    yield return new MvcModelProperty {
                        PropertyName = childProp.PropertyName,
                        CSharpTypeName = childProp.CSharpTypeName,
                        Initializer = childProp.Initializer,
                    };
                }
            } else {
                yield return new MvcModelProperty {
                    CSharpTypeName = "string",
                    PropertyName = Name,
                };
            }
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels(IAggregateMember member) {
            if (IsComplexType()) {
                yield return new MvcModelProperty {
                    CSharpTypeName = Schema.GetInstanceModel(ChildAggregate).RuntimeFullName,
                    PropertyName = Name,
                    Initializer = "new()",
                };
            } else {
                // 区分値
                yield return new MvcModelProperty {
                    CSharpTypeName = "int?",
                    PropertyName = Name,
                };
                // 各区分の詳細値
                foreach (var child in GetChildren()) {
                    yield return new MvcModelProperty {
                        CSharpTypeName = Schema.GetInstanceModel(child.Value).RuntimeFullName,
                        PropertyName = $"{Name}__{child.Value.Name}",
                        Initializer = "new()",
                    };
                }
            }
        }

        internal override string RenderSearchConditionView(ViewRenderingContext context) {
            if (IsComplexType()) {
                var nested = context.Nest(SearchConditionModels.Single().PropertyName);
                return Schema.GetSearchConditionModel(ChildAggregate).Render(nested);
            } else {
                var childrenViews = SearchConditionModels
                    .Select(child => {
                        var nested = context.Nest(child.PropertyName);
                        var template = new ChildVariationSearchConditionTemplate {
                            PropertyName = child.PropertyName,
                            AspFor = nested.AspForPath,
                        };
                        return template.TransformText();
                    });
                return string.Join(Environment.NewLine, childrenViews);
            }
        }

        internal override string RenderSearchResultView(ViewRenderingContext context) {
            if (IsComplexType()) {
                return string.Empty;
            } else {
                var nested = context.Nest(SearchResultModels.Single().PropertyName);
                return $"<span>@{nested.Path}</span>";
            }
        }

        internal override string RenderInstanceView(ViewRenderingContext context) {
            if (IsComplexType()) {
                var model = InstanceModels.Single();
                var nestedContext = context.Nest(model.PropertyName);
                return Schema.GetInstanceModel(ChildAggregate).Render(nestedContext);
            } else {
                var nested1 = context.Nest(Name); // 区分値(ラジオボタン用)
                var instanceModels = InstanceModels.ToArray();
                var childrenViews = GetChildren()
                    .Select(child => {
                        var nested2 = context.Nest($"{Name}__{child.Value.Name}"); // TODO: ToInstanceModelとロジック重複
                        var template = new ChildVariationInstanceTemplate {
                            Key = child.Key,
                            Name = child.Value.Name,
                            RadioButtonAspFor = nested1.AspForPath,
                            ChildAggregateView = Schema.GetInstanceModel(child.Value).Render(nested2),
                        };
                        return template.TransformText();
                    });
                return string.Join(Environment.NewLine, childrenViews);
            }
        }
    }

    partial class ChildVariationSearchConditionTemplate {
        public string PropertyName { get; set; }
        public string AspFor { get; set; }
    }

    partial class ChildVariationInstanceTemplate {
        public string RadioButtonAspFor { get; set; }
        public int Key { get; set; }
        public string Name { get; set; }
        public string ChildAggregateView { get; set; }
    }
}
