using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
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

        private string RenderForm(string indent) {
            var body = new AggregateInstanceFormBody(_instance, _ctx);
            body.PushIndent(indent);
            return body.TransformText();
        }

        private string CollectCombobox() {
            return _aggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetRefMembers())
                .Select(refProp => $", {new ComboBox(refProp.Terminal, _ctx).ComponentName}")
                .Distinct()
                .Join(string.Empty);
        }
    }
}
