using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalApplicationBuilder.Features.InstanceHandling;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.Features.WebClient {
#pragma warning disable IDE1006 // 命名スタイル
    partial class index : TemplateBase {
#pragma warning restore IDE1006 // 命名スタイル

        internal index(CodeRenderingContext ctx, Func<GraphNode<Aggregate>, string> dirNameResolver) {
            _ctx = ctx;
            _dirNameResolver = dirNameResolver;
        }
        private readonly CodeRenderingContext _ctx;
        private readonly Func<GraphNode<Aggregate>, string> _dirNameResolver;

        public override string FileName => "index.tsx";

        protected override string Template() {
            var components = GetComponents().ToArray();

            return $$"""
                import './halapp.css';
                import 'ag-grid-community/styles/ag-grid.css';
                import 'ag-grid-community/styles/ag-theme-alpine.css';

                {{components.SelectTextTemplate(component => $$"""
                import {{component.PhysicalName}} from '{{component.From}}'
                """)}}

                export const THIS_APPLICATION_NAME = '{{_ctx.Schema.ApplicationName}}' as const

                export const routes: { url: string, el: JSX.Element }[] = [
                {{components.SelectTextTemplate(component => $$"""
                  { url: '{{component.Url}}', el: <{{component.PhysicalName}} /> },
                """)}}
                ]
                export const menuItems: { url: string, text: string }[] = [
                {{components.Where(c => c.ShowMenu).SelectTextTemplate(component => $$"""
                  { url: '{{component.Url}}', text: '{{component.DisplayName}}' },
                """)}}
                ]
                """;
        }

        private IEnumerable<ImportedComponent> GetComponents() {
            foreach (var aggregate in _ctx.Schema.RootAggregates()) {
                var aggregateName = aggregate.Item.DisplayName.ToCSharpSafe();

                yield return new ImportedComponent {
                    ShowMenu = true,
                    Url = new Searching.SearchFeature(aggregate.As<IEFCoreEntity>(), _ctx).ReactPageUrl,
                    PhysicalName = $"{aggregateName}MultiView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(Searching.SearchFeature.REACT_FILENAME)}",
                };

                var createView = new SingleView(aggregate, _ctx, SingleView.E_Type.Create);
                var detailView = new SingleView(aggregate, _ctx, SingleView.E_Type.View);
                var editView = new SingleView(aggregate, _ctx, SingleView.E_Type.Edit);
                yield return new ImportedComponent {
                    ShowMenu = false,
                    Url = createView.Route,
                    PhysicalName = $"{aggregateName}CreateView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(createView.FileName)}",
                };
                yield return new ImportedComponent {
                    ShowMenu = false,
                    Url = detailView.Route,
                    PhysicalName = $"{aggregateName}DetailView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(detailView.FileName)}",
                };
                yield return new ImportedComponent {
                    ShowMenu = false,
                    Url = editView.Route,
                    PhysicalName = $"{aggregateName}EditView",
                    DisplayName = aggregate.Item.DisplayName,
                    From = $"./{_dirNameResolver(aggregate)}/{Path.GetFileNameWithoutExtension(editView.FileName)}",
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
