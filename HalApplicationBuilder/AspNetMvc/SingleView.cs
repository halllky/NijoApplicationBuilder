using System;
namespace HalApplicationBuilder.AspNetMvc {
    public class SingleView {
        internal Core.Aggregate RootAggregate { get; init; }

        internal string FileName => $"{RootAggregate.Name}__SingleView.cshtml";

        internal string TransformText() {
            var context = new Core.ViewRenderingContext("Model", nameof(Model<object>.Item));
            var modelClass = RootAggregate.ToInstanceModel(context);
            var template = new SingleViewTemplate {
                ModelTypeFullname = $"{GetType().FullName}.{nameof(Model<object>)}<{modelClass.RuntimeFullName}>",
                PageTitle = $"@Model.{nameof(Model<object>.InstanceName)}",
                ModelClass = modelClass,
                UpdateActionName = "Update",
                DeleteActionName = "Delete",
                PartialViewName = new AggregatePartialView { Aggregate = RootAggregate }.FileName,
                PartialViewBoundObjectName = context.AspForPath,
            };
            return template.TransformText();
        }

        public class Model<T> {
            public string InstanceName { get; set; }
            public T Item { get; set; }
        }
    }

    partial class SingleViewTemplate {
        internal static string FormId => JsTemplate.FORM_ID;
        internal static string FormFooterId => JsTemplate.FORM_FOOTER_ID;

        internal string ModelTypeFullname { get; set; }
        internal string PageTitle { get; set; }
        internal string NameofInstanceName => nameof(SingleView.Model<object>.InstanceName);
        internal Core.AutoGenerateMvcModelClass ModelClass { get; set; }
        internal string UpdateActionName { get; set; }
        internal string DeleteActionName { get; set; }
        internal string PartialViewName { get; set; }
        internal string PartialViewBoundObjectName { get; set; }
    }
}
