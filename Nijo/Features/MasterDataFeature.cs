using Nijo.Core;
using Nijo.DotnetEx;
using Nijo.Features.InstanceHandling;
using Nijo.Features.KeywordSearching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    internal class MasterDataFeature : NijoFeatureBaseByAggregate {

        public override void GenerateCode(ICodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            // Create
            var createCommand = new Command.CommandFeature();
            var createFeature = new CreateFeature(rootAggregate);
            createCommand.CommandName = root => $"{root.Item.ClassName}新規作成";
            createCommand.ActionName = root => $"{root.Item.ClassName}作成";
            createCommand.ControllerAction = createFeature.RenderController();
            createCommand.AppSrvMethod = createFeature.RenderAppSrvMethod(context);
            createCommand.GenerateCode(context, rootAggregate);

            // Search
            var searchFeature = new Searching.AggregateSearchFeature2();
            searchFeature.GenerateCode(context, rootAggregate);

            context.Render<Infrastucture>(infra => {
                infra.Aggregate(rootAggregate, builder => {

                    // AggregateDetail (for Find, Update, Delete)
                    var findFeature = new FindFeature(rootAggregate);
                    var updateFeature = new UpdateFeature(rootAggregate);
                    var deleteFeature = new DeleteFeature(rootAggregate);
                    builder.ControllerActions.Add(findFeature.RenderController());
                    builder.ControllerActions.Add(updateFeature.RenderController());
                    builder.ControllerActions.Add(deleteFeature.RenderController());
                    builder.AppServiceMethods.Add(findFeature.RenderAppSrvMethod(context));
                    builder.AppServiceMethods.Add(updateFeature.RenderAppSrvMethod(context));
                    builder.AppServiceMethods.Add(deleteFeature.RenderAppSrvMethod(context));

                    foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                        builder.DataClassDeclaring.Add(new AggregateDetail(aggregate).RenderCSharp(context));
                    }

                    // KeywordSearching
                    foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                        var keywordSearching = new KeywordSearchingFeature(aggregate);
                        var refTargetKeyName = new RefTargetKeyName(aggregate);
                        builder.DataClassDeclaring.Add(refTargetKeyName.RenderCSharpDeclaring());
                        builder.ControllerActions.Add(keywordSearching.RenderController(context));
                        builder.AppServiceMethods.Add(keywordSearching.RenderAppSrvMethod(context));
                    }

                    // DbContext, DbEntity
                    builder.HasDbSet = true;
                    builder.OnModelCreating.Add(modelBuilder => new DbContextClass(context.Config).RenderEntity(modelBuilder, rootAggregate));

                    foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                        static IEnumerable<NavigationProperty.Item> EnumerateNavigationProperties(GraphNode<Aggregate> aggregate) {
                            foreach (var nav in aggregate.GetNavigationProperties()) {
                                if (nav.Principal.Owner == aggregate) yield return nav.Principal;
                                if (nav.Relevant.Owner == aggregate) yield return nav.Relevant;
                            }
                        }
                        builder.DataClassDeclaring.Add($$"""
                            /// <summary>
                            /// {{aggregate.Item.DisplayName}}のデータベースに保存されるデータの形を表すクラスです。
                            /// </summary>
                            public partial class {{aggregate.Item.EFCoreEntityClassName}} {
                            {{aggregate.GetColumns().SelectTextTemplate(col => $$"""
                                public {{col.Options.MemberType.GetCSharpTypeName()}}? {{col.Options.MemberName}} { get; set; }
                            """)}}

                            {{EnumerateNavigationProperties(aggregate).SelectTextTemplate(nav => $$"""
                                public virtual {{nav.CSharpTypeName}} {{nav.PropertyName}} { get; set; }
                            """)}}

                                /// <summary>このオブジェクトと比較対象のオブジェクトの主キーが一致するかを返します。</summary>
                                public bool {{IEFCoreEntity.KEYEQUALS}}({{aggregate.Item.EFCoreEntityClassName}} entity) {
                            {{aggregate.GetColumns().Where(c => c.Options.IsKey).SelectTextTemplate(col => $$"""
                                    if (entity.{{col.Options.MemberName}} != this.{{col.Options.MemberName}}) return false;
                            """)}}
                                    return true;
                                }
                            }
                            """);
                    }
                });
            });

            var detailView = new SingleView(rootAggregate, SingleView.E_Type.View);
            var editView = new SingleView(rootAggregate, SingleView.E_Type.Edit);

            context.Render<Infrastucture>(infra => {
                infra.ReactPages.Add(detailView);
                infra.ReactPages.Add(editView);
            });

            context.EditReactDirectory(reactDir => {
                reactDir.Directory("pages", pageDir => {
                    pageDir.Directory(rootAggregate.Item.DisplayName.ToFileNameSafe(), aggregateDir => {
                        aggregateDir.Generate(detailView.Render());
                        aggregateDir.Generate(editView.Render());
                    });
                });
            });
        }
    }
}
