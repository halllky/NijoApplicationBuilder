using System;
using System.IO;

namespace HalApplicationBuilder.AspNetMvc {

    internal class InstancePartialView {
        internal Core.Aggregate Aggregate { get; init; }

        internal string FileName => $"_{Aggregate.Name}__Partial.cshtml";
        internal string AspViewPath => Path.Combine("~", Aggregate.Schema.Config.MvcViewDirectoryRelativePath, FileName);

        internal string TransformText() {
            var model = Aggregate.Schema.GetInstanceModel(Aggregate);
            var template = new InstancePartialViewTemplate {
                ModelTypeFullname = model.RuntimeFullName,
                View = model.Render(new ViewRenderingContext("Model")),
            };
            return template.TransformText();
        }
    }

    partial class InstancePartialViewTemplate {
        internal string ModelTypeFullname { get; set; }
        internal string View { get; set; }
    }
}
