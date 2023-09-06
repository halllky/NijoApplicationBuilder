using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.CodeRendering.WebClient {
#pragma warning disable IDE1006 // 命名スタイル
    partial class index : ITemplate {
#pragma warning restore IDE1006 // 命名スタイル

        internal index(CodeRenderingContext ctx, Func<GraphNode<Aggregate>, string> dirNameResolver) {
            _ctx = ctx;
            _dirNameResolver = dirNameResolver;
        }
        private readonly CodeRenderingContext _ctx;
        private readonly Func<GraphNode<Aggregate>, string> _dirNameResolver;

        public string FileName => "index.tsx";

        private IEnumerable<ImportedComponent> GetComponents() {
            foreach (var aggregate in _ctx.Schema.RootAggregates()) {
                var aggregateName = aggregate.Item.DisplayName.ToCSharpSafe();

                yield return new ImportedComponent {
                    ShowMenu = true,
                    Url = new Searching.SearchFeature(aggregate.GetDbEntity(), _ctx).ReactPageUrl,
                    PhysicalName = $"{aggregateName}MultiView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(Searching.SearchFeature.REACT_FILENAME)}",
                };
                var editView = new SingleView(aggregate, _ctx, asEditView: true);
                yield return new ImportedComponent {
                    ShowMenu = false,
                    Url = editView.Route,
                    PhysicalName = $"{aggregateName}EditView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(editView.FileName)}",
                };
                var detailView = new SingleView(aggregate, _ctx, asEditView: false);
                yield return new ImportedComponent {
                    ShowMenu = false,
                    Url = detailView.Route,
                    PhysicalName = $"{aggregateName}DetailView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(detailView.FileName)}",
                };
                var createView = new CreateView(aggregate, _ctx);
                yield return new ImportedComponent {
                    ShowMenu = false,
                    Url = createView.Route,
                    PhysicalName = $"{aggregateName}CreateView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(createView.FileName)}",
                };
            }
        }
        private class ImportedComponent {
            internal required bool ShowMenu { get; init; }
            internal required string Url { get; init; }
            internal required string PhysicalName { get; init; }
            internal required string DisplayName { get; init; }
            internal required string From { get; init; }
        }
    }
}
