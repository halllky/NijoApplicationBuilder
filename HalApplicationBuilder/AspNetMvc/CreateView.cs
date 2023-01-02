using System;
namespace HalApplicationBuilder.AspNetMvc {
    public class CreateView {
        internal Core.Aggregate RootAggregate { get; init; }

        internal string FileName => $"{RootAggregate.Name}__CreateView.cshtml";

        internal string TransformText() {
            var model = RootAggregate.Schema.GetInstanceModel(RootAggregate);
            var context = new ViewRenderingContext("Model", nameof(Model<object>.Item));
            var template = new CreateViewTemplate {
                ModelTypeFullname = $"{GetType().FullName}.{nameof(Model<object>)}<{model.RuntimeFullName}>",
                PageTitle = $"{RootAggregate.Name} - 新規作成",
                ExecuteActionName = "Create",
                PartialViewName = new InstancePartialView { Aggregate = RootAggregate }.FileName,
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
        internal string ExecuteActionName { get; set; }
        internal string PartialViewName { get; set; }
        internal string PartialViewBoundObjectName { get; set; }
    }
}
