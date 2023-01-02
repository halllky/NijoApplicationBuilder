using System;
using System.IO;

namespace HalApplicationBuilder.AspNetMvc {

    internal class AggregatePartialView {
        internal Core.Aggregate Aggregate { get; init; }

        internal string FileName => $"_{Aggregate.Name}__Partial.cshtml";
        internal string AspViewPath => Path.Combine("~", Aggregate.Schema.Config.MvcViewDirectoryRelativePath, FileName);

        internal string TransformText() {
            var modelClass = Aggregate.ToInstanceModel(new Core.ViewRenderingContext("Model"));
            var template = new AggregatePartialViewTemplate {
                ModelTypeFullname = modelClass.RuntimeFullName,
                View = modelClass.View,
            };
            return template.TransformText();
        }
    }

    partial class AggregatePartialViewTemplate {
        internal string ModelTypeFullname { get; set; }
        internal string View { get; set; }
    }
}
