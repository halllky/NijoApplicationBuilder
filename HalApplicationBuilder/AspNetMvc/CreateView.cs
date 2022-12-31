using System;
namespace HalApplicationBuilder.AspNetMvc {
    public class CreateView {
        internal Core.Aggregate RootAggregate { get; init; }

        internal string FileName => $"{RootAggregate.Name}_CreateView.cshtml";

        internal string TransformText() {
            var modelClass = RootAggregate.ToInstanceModel(new Core.ViewRenderingContext("Model", nameof(Model<object>.Item)));
            var template = new CreateViewTemplate {
                ModelTypeFullname = $"{GetType().FullName}.{nameof(Model<object>)}<{modelClass.RuntimeFullName}>",
                PageTitle = $"{RootAggregate.Name} - 新規作成",
                ModelClass = modelClass,
                ExecuteActionName = "Create",
            };
            return template.TransformText();
        }

        public class Model<T> {
            public T Item { get; set; }
        }
    }

    partial class CreateViewTemplate {
        internal string ModelTypeFullname { get; set; }
        internal string PageTitle { get; set; }
        internal Core.AutoGenerateMvcModelClass ModelClass { get; set; }
        internal string ExecuteActionName { get; set; }
    }
}
