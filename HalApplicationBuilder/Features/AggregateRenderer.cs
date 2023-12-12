using HalApplicationBuilder.Features.InstanceHandling;
using HalApplicationBuilder.Features.KeywordSearching;
using HalApplicationBuilder.Features.Util;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.Features.TemplateTextHelper;

namespace HalApplicationBuilder.Features {
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
                    #pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
                    #pragma warning disable CS8604 // Null 参照引数の可能性があります。
                    #pragma warning disable CS8618 // null 非許容の変数には、コンストラクターの終了時に null 以外の値が入っていなければなりません
                    #pragma warning disable IDE1006 // 命名スタイル

                    {{controller.Render(_ctx)}}

                    {{If(_aggregate.IsCreatable(), () => $$"""
                    #region データ新規作成
                    {{create.RenderController(_ctx)}}
                    {{create.RenderEFCoreMethod(_ctx)}}
                    #endregion データ新規作成
                    """)}}


                    #region 一覧検索
                    {{multiView.RenderAspNetController(_ctx)}}
                    {{search.RenderDbContextMethod(_ctx)}}
                    #endregion 一覧検索


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


                    #region データ構造
                    {{If(_aggregate.IsCreatable(), () => new AggregateCreateCommand(_aggregate).RenderCSharp(_ctx))}}
                    {{If(_aggregate.IsStored(), () => _aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ins => new AggregateDetail(ins).RenderCSharp(_ctx)))}}

                    {{If(_aggregate.IsStored(), () => $$"""
                    namespace {{_ctx.Config.RootNamespace}} {
                        using System.ComponentModel;
                        using System.ComponentModel.DataAnnotations;

                        {{WithIndent(_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ins => new RefTargetKeyName(ins).RenderCSharpDeclaring()), "    ")}}
                    }
                    """)}}

                    {{multiView.RenderCSharpTypedef(_ctx)}}

                    {{If(_aggregate.IsStored(), () => $$"""
                    namespace {{_ctx.Config.EntityNamespace}} {
                        using System;
                        using System.Collections;
                        using System.Collections.Generic;
                        using System.Linq;
                        using Microsoft.EntityFrameworkCore;
                        using Microsoft.EntityFrameworkCore.Infrastructure;

                    {{_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ett => $$"""
                        /// <summary>
                        /// {{ett.Item.DisplayName}}のデータベースに保存されるデータの形を表すクラスです。
                        /// </summary>
                        public partial class {{ett.Item.EFCoreEntityClassName}} {
                    {{ett.GetColumns().SelectTextTemplate(col => $$"""
                            public {{col.Options.MemberType.GetCSharpTypeName()}}? {{col.Options.MemberName}} { get; set; }
                    """)}}

                    {{EnumerateNavigationProperties(ett).SelectTextTemplate(nav => $$"""
                            public virtual {{nav.CSharpTypeName}} {{nav.PropertyName}} { get; set; }
                    """)}}

                            /// <summary>このオブジェクトと比較対象のオブジェクトの主キーが一致するかを返します。</summary>
                            public bool {{IEFCoreEntity.KEYEQUALS}}({{ett.Item.EFCoreEntityClassName}} entity) {
                    {{ett.GetColumns().Where(c => c.Options.IsKey).SelectTextTemplate(col => $$"""
                                if (entity.{{col.Options.MemberName}} != this.{{col.Options.MemberName}}) return false;
                    """)}}
                                return true;
                            }
                        }
                    """)}}

                        partial class {{_ctx.Config.DbContextName}} {
                    {{_aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ett => $$"""
                            public DbSet<{{_ctx.Config.EntityNamespace}}.{{ett.Item.EFCoreEntityClassName}}> {{ett.Item.DbSetName}} { get; set; }
                    """)}}
                        }
                    }
                    """)}}
                    #endregion データ構造
                    """,
            };
        }
    }
}
