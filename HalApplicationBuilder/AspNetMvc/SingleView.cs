using System;
namespace HalApplicationBuilder.AspNetMvc {
    public class SingleView {
        internal Core.Aggregate RootAggregate { get; init; }

        internal string FileName => $"{RootAggregate.Name}_SingleView.cshtml";

        internal string TransformText() {
            var modelClass = RootAggregate.ToInstanceModel(new Core.ViewRenderingContext("Model", nameof(Model<object>.Item)));
            var template = new SingleViewTemplate {
                ModelTypeFullname = $"{GetType().FullName}.{nameof(Model<object>)}<{modelClass.RuntimeFullName}>",
                PageTitle = $"@Model.{nameof(Model<object>.InstanceName)}",
                ModelClass = modelClass,
                UpdateActionName = "Update",
                DeleteActionName = "Delete",
            };
            return template.TransformText();
        }

        public class Model<T> {
            public string InstanceName { get; set; }
            public T Item { get; set; }
        }
    }

    partial class SingleViewTemplate {
        internal string ModelTypeFullname { get; set; }
        internal string PageTitle { get; set; }
        internal string NameofInstanceName => nameof(SingleView.Model<object>.InstanceName);
        internal Core.AutoGenerateMvcModelClass ModelClass { get; set; }
        internal string UpdateActionName { get; set; }
        internal string DeleteActionName { get; set; }
    }
}
