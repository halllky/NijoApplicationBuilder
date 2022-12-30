using System;
using System.Collections.Generic;
using haldoc.Core;

namespace haldoc.AspNet {
    public class CreateView {

        public MvcContext Context { get; init; }

        public string Render(Aggregate aggregate) {
            var body = aggregate.RenderSingleView(nameof(haldoc.Runtime.SingleViewModel.Instance), 1);

            var model = Context.FindModel(aggregate);
            var template = new CreateViewTemplate {
                ModelType = $"haldoc.Runtime.SingleViewModel<{model.RuntimeClassFullName}>",
                PageTitle = $"{aggregate.Name} - 新規作成",
                Body = body,
            };
            return template.TransformText();
        }
    }

    partial class CreateViewTemplate {
        public string ModelType { get; set; }
        public string PageTitle { get; set; }
        public string Body { get; set; }
    }
}
