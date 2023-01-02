using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.AspNetMvc {

    public class SearchConditionClass : MvcModel {
        public override string ClassName
            => $"{Source.Name}__SearchCondition";
        private protected override IEnumerable<MvcModelProperty> CreateProperties(IAggregateMember member)
            => PropertyFactory.CreateSearchConditionModels(member);
        internal override string Render(ViewRenderingContext context) {
            var template = new AggregateVerticalViewTemplate {
                Members = Properties.ToDictionary(
                    x => x.PropertyName,
                    x => x.Render.Invoke(context)),
            };
            return template.TransformText();
        }
    }
    public class SearchResultClass : MvcModel {
        public override string ClassName
            => $"{Source.Name}__SearchResult";
        private protected override IEnumerable<MvcModelProperty> CreateProperties(IAggregateMember member)
            => PropertyFactory.CreateSearchResultModels(member);
        internal override string Render(ViewRenderingContext context) {
            var propViews = Properties
                .Select(member => member.Render.Invoke(context));
            return string.Join(Environment.NewLine, propViews);
        }
    }
    public class InstanceModelClass : MvcModel {
        public override string ClassName
            => Source.Name;
        private protected override IEnumerable<MvcModelProperty> CreateProperties(IAggregateMember member)
            => PropertyFactory.CreateInstanceModels(member);
        internal override string Render(ViewRenderingContext context) {
            var template = new AggregateVerticalViewTemplate {
                Members = Properties.ToDictionary(
                    x => x.PropertyName,
                    x => x.Render.Invoke(context)),
            };
            return template.TransformText();
        }
    }


    public abstract class MvcModel {

        public Aggregate Source { get; init; }
        public MvcModelProperty Parent { get; init; }
        public MvcModel GetRoot() {
            var cls = this;
            while (cls.Parent != null) {
                cls = cls.Parent.Owner;
            }
            return cls;
        }

        public abstract string ClassName { get; }
        public string RuntimeFullName => Source.Schema.Config.MvcModelNamespace + "." + ClassName;

        private protected IMvcModelPropertyFactory PropertyFactory { get; init; }
        private IReadOnlyList<MvcModelProperty> _properties;
        public IReadOnlyList<MvcModelProperty> Properties {
            get {
                if (_properties == null) {
                    var props = Source.Members
                        .SelectMany(CreateProperties, (m, ui) => new { m, ui })
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
        private protected abstract IEnumerable<MvcModelProperty> CreateProperties(IAggregateMember member);

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

    internal interface IMvcModelPropertyFactory {
        IEnumerable<MvcModelProperty> CreateSearchConditionModels(IAggregateMember member);
        IEnumerable<MvcModelProperty> CreateSearchResultModels(IAggregateMember member);
        IEnumerable<MvcModelProperty> CreateInstanceModels(IAggregateMember member);
    }
}
