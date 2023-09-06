using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class SingleView : ITemplate {
        internal SingleView(GraphNode<Aggregate> aggregate, CodeRenderingContext ctx, bool asEditView) {
            _ctx = ctx;
            _aggregate = aggregate;
            _instance = aggregate.GetInstanceClass().AsEntry();
            _asEditView = asEditView;
        }

        private readonly CodeRenderingContext _ctx;
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly GraphNode<AggregateInstance> _instance;
        private readonly bool _asEditView;

        public string FileName => _asEditView ? "edit.tsx" : "detail.tsx";
        internal string Url => _asEditView
            ? $"/{_aggregate.Item.UniqueId}/edit"
            : $"/{_aggregate.Item.UniqueId}/detail";
        internal string Route => _asEditView
            ? $"/{_aggregate.Item.UniqueId}/edit/:instanceKey"
            : $"/{_aggregate.Item.UniqueId}/detail/:instanceKey";

        private string GetMultiViewUrl() => new Searching.SearchFeature(_aggregate.GetDbEntity(), _ctx).ReactPageUrl;
        private string GetEditViewUrl() => new SingleView(_aggregate, _ctx, asEditView: true).Url;
        private string GetReadonlySingleViewUrl() => new SingleView(_aggregate, _ctx, asEditView: false).Url;
        private string GetFindCommandApi() => new Controller(_aggregate.Item, _ctx).FindCommandApi;
        private string GetUpdateCommandApi() => new Controller(_aggregate.Item, _ctx).UpdateCommandApi;
    }
}
