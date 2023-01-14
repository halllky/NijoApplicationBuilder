using System;
using System.Collections.Generic;
using System.IO;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.AspNetMvc {

    internal class InstancePartialView {
        internal InstancePartialView(Core.Aggregate aggregate, Core.Config config) {
            Aggregate = aggregate;
            _config = config;
        }

        internal Core.Aggregate Aggregate { get; }
        private readonly Core.Config _config;

        internal string FileName => $"_{Aggregate.Name}__Partial.cshtml";
        internal string AspViewPath => Path.Combine("~", _config.MvcViewDirectoryRelativePath, FileName);

        internal string TransformText(IViewModelProvider viewModelProvider) {
            var model = viewModelProvider.GetInstanceModel(Aggregate);
            var template = new InstancePartialViewTemplate {
                ModelTypeFullname = model.RuntimeFullName,
                View = model.Render(new ViewRenderingContext(viewModelProvider, "Model")),
                HiddenFields = new[] {
                    ($"{nameof(UIInstanceBase.halapp_fields)}.{nameof(HalappViewState.IsRoot)}", ""),
                    ($"{nameof(UIInstanceBase.halapp_fields)}.{nameof(HalappViewState.Removed)}", JsTemplate.REMOVE_HIDDEN_FIELD),
                },
                ShowRemoveButton = $"Model.{nameof(UIInstanceBase.halapp_fields)}.{nameof(HalappViewState.IsRoot)} == false",
            };
            return template.TransformText();
        }
    }

    partial class InstancePartialViewTemplate {
        internal string ModelTypeFullname { get; set; }
        internal string View { get; set; }
        internal string ShowRemoveButton { get; set; }
        internal IEnumerable<(string AspFor, string Class)> HiddenFields { get; set; }
        internal static string ContainerClassName => JsTemplate.AGG_PARTIAL_CONTAINER;
        internal static string RemoveButtonClassName => JsTemplate.REMOVE_BTN;
    }
}
