using System;
using HalApplicationBuilder.Core.UIModel;
using HalApplicationBuilder.Core.Runtime;

namespace HalApplicationBuilder.AspNetMvc {
    public class SingleView {
        internal SingleView(Core.Aggregate aggregate) {
            if (aggregate.Parent != null) throw new ArgumentException($"集約ルートのみ");
            RootAggregate = aggregate;
        }

        internal Core.Aggregate RootAggregate { get; }

        internal string FileName => $"{RootAggregate.Name}__SingleView.cshtml";

        internal string TransformText(IViewModelProvider viewModelProvider, Core.Config config) {
            var model = viewModelProvider.GetInstanceModel(RootAggregate);
            var context = new ViewRenderingContext(viewModelProvider, "Model", nameof(Model<UIInstanceBase>.Item));
            var template = new SingleViewTemplate {
                ModelTypeFullname = $"{GetType().FullName}.{nameof(Model<UIInstanceBase>)}<{model.RuntimeFullName}>",
                PageTitle = $"@Model.{nameof(Model<UIInstanceBase>.InstanceName)}",
                UpdateActionName = "Update",
                DeleteActionName = "Delete",
                PartialViewName = new InstancePartialView(RootAggregate, config).FileName,
                PartialViewBoundObjectName = context.AspForPath,
            };
            return template.TransformText();
        }

        public class Model<T> where T : UIInstanceBase {
            public string InstanceName { get; set; }
            public T Item { get; set; }
        }
    }

    partial class SingleViewTemplate {
        internal static string FormId => JsTemplate.FORM_ID;
        internal static string FormFooterId => JsTemplate.FORM_FOOTER_ID;

        internal string ModelTypeFullname { get; set; }
        internal string PageTitle { get; set; }
        internal string NameofInstanceName => nameof(SingleView.Model<UIInstanceBase>.InstanceName);
        internal string UpdateActionName { get; set; }
        internal string DeleteActionName { get; set; }
        internal string PartialViewName { get; set; }
        internal string PartialViewBoundObjectName { get; set; }
    }
}
