using System;
namespace HalApplicationBuilder.AspNetMvc {
    public class CreateView {
        internal Core.Aggregate RootAggregate { get; init; }

        internal string FileName => $"{RootAggregate.Name}__CreateView.cshtml";

        internal string TransformText() {
            var context = new Core.ViewRenderingContext("Model", nameof(Model<object>.Item));
            var modelClass = RootAggregate.ToInstanceModel();
            var template = new CreateViewTemplate {
                ModelTypeFullname = $"{GetType().FullName}.{nameof(Model<object>)}<{modelClass.RuntimeFullName}>",
                PageTitle = $"{RootAggregate.Name} - 新規作成",
                ModelClass = modelClass,
                ExecuteActionName = "Create",
                PartialViewName = new AggregatePartialView { Aggregate = RootAggregate }.FileName,
                PartialViewBoundObjectName = context.AspForPath,
            };
            return template.TransformText();
        }

        public class Model<T> {
            public T Item { get; set; }
        }
    }

    partial class CreateViewTemplate {
        internal static string FormId => JsTemplate.FORM_ID;
        internal static string FormFooterId => JsTemplate.FORM_FOOTER_ID;

        internal string ModelTypeFullname { get; set; }
        internal string PageTitle { get; set; }
        internal Core.UIClass ModelClass { get; set; }
        internal string ExecuteActionName { get; set; }
        internal string PartialViewName { get; set; }
        internal string PartialViewBoundObjectName { get; set; }
    }
}
