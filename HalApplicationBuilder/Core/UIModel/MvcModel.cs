using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core.Members;
using HalApplicationBuilder.DotnetEx;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.Core.UIModel {

    public class SearchConditionClass : MvcModel {
        public override string ClassName
            => $"{Source.Name}__SearchCondition";

        private protected override IEnumerable<MvcModelProperty> CreateProperties(IMvcModelPropertySource mvcModelProperty) {
            foreach (var prop in mvcModelProperty.CreateSearchConditionModels()) {
                //prop.Render = mvcModelProperty.RenderSearchConditionView;
                yield return prop;
            }
        }

        internal override string Render(ViewRenderingContext context) {
            var members = new Dictionary<string, object>();
            foreach (var member in Source.Members) {
                if (member is SchalarValue schalarValue) {
                    var type = schalarValue.GetPropertyTypeExceptNullable();
                    var nested = context.Nest(schalarValue.SearchConditonPropName);
                    if (schalarValue.IsRangeSearchCondition()) {
                        // 範囲検索
                        var data = new InstanceTemplateSchalarValueSearchConditionData {
                            Type = InstanceTemplateSchalarValueSearchConditionData.E_Type.Range,
                            AspFor = new[] {
                                $"{nested.AspForPath}.{nameof(FromTo.From)}",
                                $"{nested.AspForPath}.{nameof(FromTo.To)}",
                            },
                        };
                        members.Add(member.Name, data);

                    } else if (type.IsEnum) {
                        // enumドロップダウン
                        var data = new InstanceTemplateSchalarValueSearchConditionData {
                            Type = InstanceTemplateSchalarValueSearchConditionData.E_Type.Select,
                            AspFor = new[] { nested.AspForPath },
                            EnumTypeName = schalarValue.GetSearchConditionCSharpTypeName(),
                            Options = schalarValue.IsNullable()
                                ? new[] { KeyValuePair.Create("", "") }
                                : Array.Empty<KeyValuePair<string, string>>(),
                        };
                        members.Add(member.Name, data);

                    } else {
                        // ただのinput
                        var data = new InstanceTemplateSchalarValueSearchConditionData {
                            Type = InstanceTemplateSchalarValueSearchConditionData.E_Type.Input,
                            AspFor = new[] { nested.AspForPath },
                        };
                        members.Add(member.Name, data);
                    }

                } else if (member is Child child) {
                    var nested = context.Nest(child.SearchConditionPropName);
                    var data = new InstanceTemplateChildData {
                        ChildView = context.ViewModelProvider.GetSearchConditionModel(child.ChildAggregate).Render(nested),
                    };
                    members.Add(member.Name, data);

                } else if (member is Variation variation) {
                    var data = variation.CreateSearchConditionModels().Select(child => {
                        var nested = context.Nest(child.PropertyName);
                        return new InstanceTemplateVariationSearchConditionData {
                            PropertyName = child.PropertyName,
                            AspFor = nested.AspForPath,
                        };
                    });
                    members.Add(member.Name, data);

                } else if (member is Children children) {
                    // 何もしない

                } else if (member is Reference reference) {
                    var nestedKey = context.Nest(reference.SearchConditonPropName).Nest(nameof(ReferenceDTO.InstanceKey));
                    var nestedText = context.Nest(reference.SearchConditonPropName).Nest(nameof(ReferenceDTO.InstanceName));
                    var data = new InstanceTemplateReferencenData {
                        AspForKey = nestedKey.AspForPath,
                        AspForText = nestedText.AspForPath,
                    };
                    members.Add(member.Name, data);
                }
            }
            var template = new PartialViewOfInstanceTemplate { Members = members };
            return template.TransformText();
        }
    }

    public class SearchResultClass : MvcModel {
        public override string ClassName
            => $"{Source.Name}__SearchResult";

        private protected override IEnumerable<MvcModelProperty> CreateProperties(IMvcModelPropertySource mvcModelProperty) {
            foreach (var prop in mvcModelProperty.CreateSearchResultModels()) {
                //prop.Render = mvcModelProperty.RenderSearchResultView;
                yield return prop;
            }
        }

        internal override string Render(ViewRenderingContext context) {
            var propViews = new List<string>();
            foreach (var member in Source.Members) {
                if (member is SchalarValue schalarValue) {
                    var nested = context.Nest(schalarValue.SearchResultPropName, isCollection: false);
                    propViews.Add($"<span>@{nested.Path}</span>");

                } else if (member is Child child) {
                    // 何もしない

                } else if (member is Variation variation) {
                    var nested = context.Nest(variation.SearchResultPropName);
                    propViews.Add($"<span>@{nested.Path}</span>");

                } else if (member is Children children) {
                    // 何もしない

                } else if (member is Reference reference) {
                    var nestedKey = context.Nest(reference.SearchResultPropName).Nest(nameof(ReferenceDTO.InstanceKey));
                    var nestedText = context.Nest(reference.SearchResultPropName).Nest(nameof(ReferenceDTO.InstanceName));
                    propViews.Add($"<span>@{nestedText.Path}<input type=\"hidden\" asp-for=\"{nestedKey.AspForPath}\"></span>");
                }
            }
            return string.Join(Environment.NewLine, propViews);
        }
    }

    public class InstanceModelClass : MvcModel {
        public override string ClassName
            => Source.Name;

        private protected override IEnumerable<MvcModelProperty> CreateProperties(IMvcModelPropertySource mvcModelProperty) {
            foreach (var prop in mvcModelProperty.CreateInstanceModels()) {
                //prop.Render = mvcModelProperty.RenderInstanceView;
                yield return prop;
            }
        }

        internal override string Render(ViewRenderingContext context) {
            var members = new Dictionary<string, object>();
            foreach (var member in Source.Members) {
                if (member is SchalarValue schalarValue) {
                    var nested = context.Nest(schalarValue.InstanceModelPropName);
                    var data = new InstanceTemplateSchalarValueData {
                        AspForPath = nested.AspForPath,
                    };
                    members.Add(member.Name, data);

                } else if (member is Children children) {
                    var nested = context.Nest(children.InstanceModelPropName, isCollection: true);
                    var data = new InstanceTemplateChildrenData {
                        i = context.LoopVar,
                        Count = $"{nested.CollectionPath}.{nameof(ICollection<object>.Count)}",
                        PartialViewName = new InstancePartialView(children.ChildAggregate, Config).FileName,
                        PartialViewBoundObjectName = nested.AspForPath,
                        AspForAddChild = new AggregatePath(children.ChildAggregate).Value,
                        AddButtonBoundObjectName = nested.AspForCollectionPath,
                    };
                    members.Add(member.Name, data);

                } else if (member is Reference reference) {
                    var nestedKey = context.Nest(reference.InstanceModelPropName).Nest(nameof(ReferenceDTO.InstanceKey));
                    var nestedText = context.Nest(reference.InstanceModelPropName).Nest(nameof(ReferenceDTO.InstanceName));
                    var data = new InstanceTemplateReferencenData {
                        AspForKey = nestedKey.AspForPath,
                        AspForText = nestedText.AspForPath,
                    };
                    members.Add(member.Name, data);

                } else if (member is Child child) {
                    var nested = context.Nest(child.InstanceModelPropName);
                    var data = new InstanceTemplateChildData {
                        ChildView = context.ViewModelProvider.GetInstanceModel(child.ChildAggregate).Render(nested),
                    };
                    members.Add(member.Name, data);

                } else if (member is Variation variation) {
                    var nested1 = context.Nest(variation.InstanceModelTypeSwitchPropName); // 区分値(ラジオボタン用)
                    var data = variation.Variations.Select(child => {
                        var nested2 = context.Nest(variation.InstanceModelTypeDetailPropName(child));
                        return new InstanceTemplateVariationData {
                            Key = child.Key,
                            Name = child.Value.Name,
                            RadioButtonAspFor = nested1.AspForPath,
                            ChildAggregateView = context.ViewModelProvider.GetInstanceModel(child.Value).Render(nested2),
                        };
                    });
                    members.Add(member.Name, data);
                }
            }
            var template = new PartialViewOfInstanceTemplate { Members = members };
            return template.TransformText();
        }
    }


    public abstract class MvcModel {

        public Aggregate Source { get; init; }
        public MvcModelProperty Parent { get; init; }
        public Core.Config Config { get; init; }

        public MvcModel GetRoot() {
            var model = this;
            while (model.Parent != null) {
                model = model.Parent.Owner;
            }
            return model;
        }
        public IEnumerable<MvcModel> GetAncestors() {
            var model = Parent.Owner;
            while (model != null) {
                yield return model;
                model = model.Parent.Owner;
            }
        }

        public abstract string ClassName { get; }
        public string RuntimeFullName => Config.MvcModelNamespace + "." + ClassName;

        private List<MvcModelProperty> _properties;
        public IReadOnlyList<MvcModelProperty> Properties {
            get {
                if (_properties == null) {
                    _properties = new List<MvcModelProperty>();
                    foreach (var member in Source.Members) {
                        if (member is not IMvcModelPropertySource mvcModelProperty)
                            throw new InvalidOperationException($"{nameof(IMvcModelPropertySource)}を継承していない{nameof(IAggregateMember)}がある");

                        var props = CreateProperties(mvcModelProperty).ToArray();
                        foreach (var prop in props) {
                            prop.Source = member;
                            prop.Owner = this;
                        }

                        _properties.AddRange(props);
                    }
                }
                return _properties;
            }
        }
        private protected abstract IEnumerable<MvcModelProperty> CreateProperties(IMvcModelPropertySource mvcModelProperty);

        internal abstract string Render(ViewRenderingContext context);

        public override string ToString() {
            var path = new List<string>();
            var parent = Parent;
            while (parent != null) {
                path.Insert(0, parent.PropertyName);
                parent = parent.Owner.Parent;
            }
            path.Insert(0, GetRoot().ClassName);
            return $"{nameof(MvcModel)}[{string.Join(".", path)}]";
        }
    }


    public class MvcModelProperty {

        public IAggregateMember Source { get; set; }
        public MvcModel Owner { get; set; }

        public string CSharpTypeName { get; init; }
        public string PropertyName { get; init; }
        public string Initializer { get; init; }

        [Obsolete]
        internal Func<ViewRenderingContext, string> Render { get; set; }

        public override string ToString() {
            var path = new List<string>();
            path.Insert(0, PropertyName);

            var parent = Owner.Parent;
            while (parent != null) {
                path.Insert(0, parent.PropertyName);
                parent = parent.Owner.Parent;
            }
            path.Insert(0, Owner.GetRoot().ClassName);
            return $"{nameof(MvcModelProperty)}[{string.Join(".", path)}]";
        }
    }

    internal interface IMvcModelPropertySource {
        IEnumerable<MvcModelProperty> CreateSearchConditionModels();
        IEnumerable<MvcModelProperty> CreateSearchResultModels();
        IEnumerable<MvcModelProperty> CreateInstanceModels();
    }
}
