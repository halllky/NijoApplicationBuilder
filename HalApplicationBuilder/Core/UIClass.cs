using System;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.Core {

    public class SearchConditionClass : UIClass {
        public override string ClassName
            => $"{Source.Name}__SearchCondition";
        public override UIProperty Parent
            => Source.Parent?.SearchConditionModels.FirstOrDefault();
        protected override IReadOnlyList<UIProperty> BuildProperties(AggregateMemberBase member)
            => member.SearchConditionModels;
        internal override string Render(ViewRenderingContext context) {
            var propViews = Source.Members.Select(x => KeyValuePair.Create(x.Name, x.RenderSearchConditionView(context)));
            var template = new AggregateVerticalViewTemplate {
                Members = propViews,
            };
            return template.TransformText();
        }
    }
    public class SearchResultClass : UIClass {
        public override string ClassName
            => $"{Source.Name}__SearchResult";
        public override UIProperty Parent
            => Source.Parent?.SearchResultModels.FirstOrDefault();
        protected override IReadOnlyList<UIProperty> BuildProperties(AggregateMemberBase member)
            => member.SearchResultModels;
        internal override string Render(ViewRenderingContext context) {
            var propViews = Source.Members
                .Select(member => member.RenderSearchResultView(context))
                .ToList();
            return string.Join(Environment.NewLine, propViews);
        }
    }
    public class InstanceModelClass : UIClass {
        public override string ClassName
            => Source.Name;
        public override UIProperty Parent
            => Source.Parent?.InstanceModels.FirstOrDefault();
        protected override IReadOnlyList<UIProperty> BuildProperties(AggregateMemberBase member)
            => member.InstanceModels;
        internal override string Render(ViewRenderingContext context) {
            var propViews = Source.Members.Select(x => KeyValuePair.Create(x.Name, x.RenderInstanceView(context)));
            var template = new AggregateVerticalViewTemplate {
                Members = propViews,
            };
            return template.TransformText();
        }
    }

    public abstract class UIClass {

        public Aggregate Source { get; init; }
        public abstract UIProperty Parent { get; }
        public UIClass GetRoot() {
            var cls = this;
            while (cls.Parent != null) {
                cls = cls.Parent.Owner;
            }
            return cls;
        }

        public abstract string ClassName { get; }
        public string RuntimeFullName => Source.Schema.Config.MvcModelNamespace + "." + ClassName;

        private IReadOnlyList<UIProperty> _properties;
        public IReadOnlyList<UIProperty> Properties {
            get {
                if (_properties == null) {
                    var props = Source.Members
                        .SelectMany(BuildProperties, (m, ui) => new { m, ui })
                        .ToList();
                    foreach (var x in props) {
                        x.ui.Source = x.m;
                        x.ui.Owner = this;
                    }
                    _properties = props.Select(x => x.ui).ToArray();
                }
                return _properties;
            }
        }
        protected abstract IReadOnlyList<UIProperty> BuildProperties(AggregateMemberBase member);

        internal abstract string Render(ViewRenderingContext context);

        public override string ToString() {
            var path = new List<string>();
            var parent = Parent;
            while (parent != null) {
                path.Insert(0, parent.PropertyName);
                parent = parent.Owner.Parent;
            }
            path.Insert(0, GetRoot().ClassName);
            return $"{nameof(UIClass)}[{string.Join(".", path)}]";
        }
    }

    public class UIProperty {
        internal UIProperty() { }

        public AggregateMemberBase Source { get; set; }
        public UIClass Owner { get; set; }

        public string CSharpTypeName { get; init; }
        public string PropertyName { get; init; }
        public string Initializer { get; init; }

        public override string ToString() {
            var path = new List<string>();
            path.Insert(0, PropertyName);

            var parent = Owner.Parent;
            while (parent != null) {
                path.Insert(0, parent.PropertyName);
                parent = parent.Owner.Parent;
            }
            path.Insert(0, Owner.GetRoot().ClassName);
            return $"{nameof(UIProperty)}[{string.Join(".", path)}]";
        }
    }
}
