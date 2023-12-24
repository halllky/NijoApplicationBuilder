using Nijo.Features.InstanceHandling;
using Nijo.Features.KeywordSearching;
using Nijo.Features.Util;
using Nijo.Core;
using Nijo.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Features.TemplateTextHelper;

namespace Nijo.Features {
    internal partial class AggregateRenderer {

        internal AggregateRenderer(GraphNode<Aggregate> aggregate) {
            if (!aggregate.IsRoot())
                throw new ArgumentException($"{nameof(AggregateRenderer)} requires root aggregate.", nameof(aggregate));

            _aggregate = aggregate;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        private IEnumerable<NavigationProperty.Item> EnumerateNavigationProperties(GraphNode<Aggregate> aggregate) {
            foreach (var nav in aggregate.GetNavigationProperties()) {
                if (nav.Principal.Owner == aggregate) yield return nav.Principal;
                if (nav.Relevant.Owner == aggregate) yield return nav.Relevant;
            }
        }

        internal SourceFile Render() {
            var controller = new WebClient.Controller(_aggregate.Item);
            var search = new Searching.AggregateSearchFeature(_aggregate);
            var multiView = search.GetMultiView();
            var find = new FindFeature(_aggregate);
            var create = new CreateFeature(_aggregate);
            var update = new UpdateFeature(_aggregate);
            var delete = new DeleteFeature(_aggregate);
            var keywordSearching = _aggregate
                .EnumerateThisAndDescendants()
                .Select(a => new KeywordSearchingFeature(a));

            return new SourceFile {
                FileName = $"{_aggregate.Item.DisplayName.ToFileNameSafe()}.cs",
                RenderContent = _ctx => $$"""
                    {{If(_aggregate.IsCreatable(), () => $$"""
                    #region データ新規作成
                    {{create.RenderController(_ctx)}}
                    {{create.RenderEFCoreMethod(_ctx)}}
                    #endregion データ新規作成
                    """)}}


                    {{If(_aggregate.IsSearchable(), () => $$"""
                    #region 一覧検索
                    {{multiView.RenderAspNetController(_ctx)}}
                    {{search.RenderDbContextMethod(_ctx)}}
                    #endregion 一覧検索
                    """)}}


                    {{If(_aggregate.IsStored(), () => $$"""
                    #region キーワード検索
                    {{keywordSearching.SelectTextTemplate(feature => $$"""
                    {{feature.RenderController(_ctx)}}
                    {{feature.RenderDbContextMethod(_ctx)}}
                    """)}}
                    #endregion キーワード検索
                    """)}}


                    {{If(_aggregate.IsStored(), () => $$"""
                    #region 詳細検索
                    {{find.RenderController(_ctx)}}
                    {{find.RenderEFCoreMethod(_ctx)}}
                    #endregion 詳細検索
                    """)}}


                    {{If(_aggregate.IsEditable(), () => $$"""
                    #region 更新
                    {{update.RenderController(_ctx)}}
                    {{update.RenderEFCoreMethod(_ctx)}}
                    #endregion 更新
                    """)}}


                    {{If(_aggregate.IsDeletable(), () => $$"""
                    #region 削除
                    {{delete.RenderController(_ctx)}}
                    {{delete.RenderEFCoreMethod(_ctx)}}
                    #endregion 削除
                    """)}}
                    """,
            };
        }

        public AggregateRenderer() { }
    }
}
