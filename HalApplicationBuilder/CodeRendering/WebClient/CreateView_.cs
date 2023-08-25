using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class CreateView : ITemplate {
        internal CreateView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx) {
            _ctx = ctx;
            _aggregate = aggregate;
            _instance = aggregate.GetInstanceClass().AsEntry();
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<AggregateInstance> _instance;

        public string FileName => "new.tsx";
        internal string Url => $"/{_aggregate.Item.UniqueId}/new";
        internal string Route => $"/{_aggregate.Item.UniqueId}/new";

        private string GetMultiViewUrl() => new MultiView(_aggregate, _ctx).Url;
        private string GetSingleViewUrl() => new SingleView(_aggregate, _ctx).Url;
        private string GetCreateCommandApi() => new AggFile.Controller(_aggregate).CreateCommandApi;

        private void RenderImportFromComponents() {
            var descs = new DescencantForms(_instance, _ctx);
            var components = descs.EnumerateComponentNames().Join(", ");
            WriteLine($"import {{ {components} }} from './{Path.GetFileNameWithoutExtension(descs.FileName)}'");
        }

        private void RenderForm() {
            var body = new AggregateInstanceFormBody(_instance, _ctx);
            ((ITemplate)this).Render(body);
        }
    }
}
