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
    internal partial class AggregateRenderer : IAggregateSourceFileUsedByMultiFeature {

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

        public List<Func<WebClient.Controller, string>> ControllerActions { get; } = new();
        public List<Func<ApplicationService, string>> AppServiceMethods { get; } = new();

        void IAggregateSourceFileUsedByMultiFeature.GenerateSourceFile(ICodeRenderingContext context, GraphNode<Aggregate> aggregate) {

            context.EditWebApiDirectory(dir => {
                var appSrv = new ApplicationService(context.Config);
                var controller = new WebClient.Controller(aggregate.Item);
                var search = new Searching.AggregateSearchFeature(aggregate);
                var multiView = search.GetMultiView();
                var find = new FindFeature(aggregate);
                var create = new CreateFeature(aggregate);
                var update = new UpdateFeature(aggregate);
                var delete = new DeleteFeature(aggregate);
                var keywordSearching = aggregate
                    .EnumerateThisAndDescendants()
                    .Select(a => new KeywordSearchingFeature(a));

                dir.Generate(new SourceFile {
                    FileName = $"{aggregate.Item.DisplayName.ToFileNameSafe()}.cs",
                    RenderContent = _ => $$"""
                        namespace {{context.Config.RootNamespace}} {
                            using Microsoft.AspNetCore.Mvc;
                            using {{context.Config.EntityNamespace}};
                        
                            [ApiController]
                            [Route("{{WebClient.Controller.SUBDOMAIN}}/[controller]")]
                            public partial class {{controller.ClassName}} : ControllerBase {
                                public {{controller.ClassName}}(ILogger<{{controller.ClassName}}> logger, {{appSrv.ClassName}} applicationService) {
                                    _logger = logger;
                                    _applicationService = applicationService;
                                }
                                protected readonly ILogger<{{controller.ClassName}}> _logger;
                                protected readonly {{appSrv.ClassName}} _applicationService;

                                {{WithIndent(ControllerActions.SelectTextTemplate(fn => fn.Invoke(controller)), "        ")}}
                            }

                            partial class {{appSrv.ClassName}} {
                                {{WithIndent(AppServiceMethods.SelectTextTemplate(fn => fn.Invoke(appSrv)), "        ")}}
                            }


                            #region データ構造
                            {{If(aggregate.IsCreatable(), () => new AggregateCreateCommand(aggregate).RenderCSharp(context))}}
                            {{If(aggregate.IsStored(), () => aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ins => new AggregateDetail(ins).RenderCSharp(context)))}}

                            {{If(aggregate.IsStored(), () => $$"""
                            namespace {{context.Config.RootNamespace}} {
                                using System.ComponentModel;
                                using System.ComponentModel.DataAnnotations;

                                {{WithIndent(aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ins => new RefTargetKeyName(ins).RenderCSharpDeclaring()), "    ")}}
                            }
                            """)}}

                            {{If(aggregate.IsSearchable(), () => $$"""
                            {{multiView.RenderCSharpTypedef(context)}}
                            """)}}

                            {{If(aggregate.IsStored(), () => $$"""
                            namespace {{context.Config.EntityNamespace}} {
                                using System;
                                using System.Collections;
                                using System.Collections.Generic;
                                using System.Linq;
                                using Microsoft.EntityFrameworkCore;
                                using Microsoft.EntityFrameworkCore.Infrastructure;

                            {{aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ett => $$"""
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

                                partial class {{context.Config.DbContextName}} {
                            {{aggregate.EnumerateThisAndDescendants().SelectTextTemplate(ett => $$"""
                                    public DbSet<{{ett.Item.EFCoreEntityClassName}}> {{ett.Item.DbSetName}} { get; set; }
                            """)}}
                                }
                            }
                            """)}}
                            #endregion データ構造
                        }
                        """,
                });
            });
        }
    }
}
