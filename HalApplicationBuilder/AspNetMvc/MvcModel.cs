using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.AspNetMvc {

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
            var views = new Dictionary<string, string>();
            foreach (var member in Source.Members) {
                if (member is not IMemberRenderer renderer) continue;
                views.Add(member.Name, renderer.RenderSearchConditionView(context));
            }
            var template = new PartialViewOfInstanceTemplate {
                Members = views,
            };
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
                if (member is not IMemberRenderer renderer) continue;
                propViews.Add(renderer.RenderSearchResultView(context));
            }
            //var propViews = Properties
            //    .Select(member => member.Render.Invoke(context));
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
            var views = new Dictionary<string, string>();
            foreach (var member in Source.Members) {
                if (member is not IMemberRenderer renderer) continue;
                views.Add(member.Name, renderer.RenderInstanceView(context));
            }
            var template = new PartialViewOfInstanceTemplate {
                Members = views,
            };
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
    internal interface IMemberRenderer {
        string RenderSearchConditionView(ViewRenderingContext context);
        string RenderSearchResultView(ViewRenderingContext context);
        string RenderInstanceView(ViewRenderingContext context);
    }
}
