using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HalApplicationBuilder.Runtime.AspNetMvc {
    public class Controller {
        internal Core.Config Config { get; init; }
        internal Core.Builder AggregateBuilder { get; init; }

        internal string TransformText() {
            var _ = new Core.ViewRenderingContext();
            var controllers = AggregateBuilder
                .EnumerateRootAggregates()
                .Select(aggregate => new ControllerClassMetadata {
                    ClassName = $"{aggregate.Name}Controller",
                    BaseClassFullName
                        = $"{typeof(ControllerBase<,,>).Namespace}.{nameof(ControllerBase<object, object, object>)}"
                        + $"<{aggregate.ToSearchConditionModel(_).RuntimeFullName},"
                        + $" {aggregate.ToSearchResultModel(_).RuntimeFullName},"
                        + $" {aggregate.ToInstanceModel(_).RuntimeFullName}>",
                    MultiViewName = "~/" + Path.Combine(Config.MvcViewDirectoryRelativePath, new MultiView { RootAggregate = aggregate }.FileName),
                    CreateViewName = "~/" + Path.Combine(Config.MvcViewDirectoryRelativePath, new CreateView { RootAggregate = aggregate }.FileName),
                    SingleViewName = "~/" + Path.Combine(Config.MvcViewDirectoryRelativePath, new SingleView { RootAggregate = aggregate }.FileName),
                });
            var template = new ControllerTemplate {
                Namespace = Config.MvcControllerNamespace,
                Controllers = controllers,
            };
            return template.TransformText();
        }
    }

    internal class ControllerClassMetadata {
        public string ClassName { get; set; }
        public string BaseClassFullName { get; set; }
        public string MultiViewName { get; set; }
        public string CreateViewName { get; set; }
        public string SingleViewName { get; set; }
    }

    partial class ControllerTemplate {
        internal string Namespace { get; set; }
        internal IEnumerable<ControllerClassMetadata> Controllers { get; set; }
    }
}
