using HalApplicationBuilder.CodeRendering.WebClient;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.ReactAndWebApi {
    partial class menuItems : ITemplate {
        internal const string FILE_NAME = "menuItems.tsx";
        internal static string IMPORT_NAME => Path.GetFileNameWithoutExtension(FILE_NAME);

        public string FileName => FILE_NAME;

        internal menuItems(CodeRenderingContext ctx, Func<GraphNode<Aggregate>, string> dirNameResolver) {
            _ctx = ctx;
            _dirNameResolver = dirNameResolver;
        }
        private readonly CodeRenderingContext _ctx;
        private readonly Func<GraphNode<Aggregate>, string> _dirNameResolver;

        private IEnumerable<ImportedComponent> GetComponents() {
            foreach (var aggregate in _ctx.Schema.RootAggregates()) {
                var aggregateName = aggregate.Item.DisplayName.ToCSharpSafe();

                var multiView = new MultiView(aggregate, _ctx);
                yield return new ImportedComponent {
                    ShowMenu = true,
                    Url = multiView.Url,
                    PhysicalName = $"{aggregateName}MultiView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(multiView.FileName)}",
                };
                var singleView = new SingleView(aggregate, _ctx);
                yield return new ImportedComponent {
                    ShowMenu = false,
                    Url = singleView.Url,
                    PhysicalName = $"{aggregateName}SingleView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(singleView.FileName)}",
                };
                var createView = new CreateView(aggregate, _ctx);
                yield return new ImportedComponent {
                    ShowMenu = false,
                    Url = createView.Url,
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
