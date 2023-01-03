using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HalApplicationBuilder.AspNetMvc {
    public class Controller {

        internal string TransformText(Core.ApplicationSchema schema, Core.Config config) {
            var controllers = schema
                .RootAggregates()
                .Select(aggregate => new ControllerClassMetadata {
                    ClassName = $"{aggregate.Name}Controller",
                    BaseClassFullName
                        = $"{typeof(ControllerBase<,,>).Namespace}.{nameof(ControllerBase<object, object, object>)}"
                        + $"<{schema.GetSearchConditionModel(aggregate).RuntimeFullName},"
                        + $" {schema.GetSearchResultModel(aggregate).RuntimeFullName},"
                        + $" {schema.GetInstanceModel(aggregate).RuntimeFullName}>",
                    MultiViewName = "~/" + Path.Combine(config.MvcViewDirectoryRelativePath, new MultiView(aggregate).FileName),
                    CreateViewName = "~/" + Path.Combine(config.MvcViewDirectoryRelativePath, new CreateView(aggregate).FileName),
                    SingleViewName = "~/" + Path.Combine(config.MvcViewDirectoryRelativePath, new SingleView(aggregate).FileName),
                });
            var template = new ControllerTemplate {
                Namespace = config.MvcControllerNamespace,
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
