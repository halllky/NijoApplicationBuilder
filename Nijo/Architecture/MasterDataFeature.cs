using Nijo.Core;
using Nijo.DotnetEx;
using Nijo.Architecture.InstanceHandling;
using Nijo.Architecture.KeywordSearching;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Architecture {
    internal class MasterDataFeature : NijoFeatureBaseByAggregate {

        public override void GenerateCode(ICodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            // Create
            var createCommand = new Command.CommandFeature();
            var createFeature = new CreateFeature(rootAggregate);
            createCommand.CommandName = root => $"{root.Item.ClassName}新規作成";
            createCommand.ActionName = root => $"{root.Item.ClassName}作成";
            createCommand.ControllerAction = createFeature.RenderController();
            createCommand.AppSrvMethod = createFeature.RenderAppSrvMethod();
            createCommand.GenerateCode(context, rootAggregate);

            // Search
            var searchFeature = new Searching.AggregateSearchFeature();
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
                    builder.AppServiceMethods.Add(findFeature.RenderAppSrvMethod());
                    builder.AppServiceMethods.Add(updateFeature.RenderAppSrvMethod());
                    builder.AppServiceMethods.Add(deleteFeature.RenderAppSrvMethod());

                    foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                        var aggregateDetail = new AggregateDetail(aggregate);
                        var initializerFunc = new TSInitializerFunction(aggregate);
                        builder.DataClassDeclaring.Add(aggregateDetail.RenderCSharp(context));
                        builder.TypeScriptDataTypes.Add(aggregateDetail.RenderTypeScript(context));
                        builder.TypeScriptDataTypes.Add(initializerFunc.Render());
                    }

                    // KeywordSearching
                    foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                        var keywordSearching = new KeywordSearchingFeature(aggregate);
                        var refTargetKeyName = new RefTargetKeyName(aggregate);
                        builder.DataClassDeclaring.Add(refTargetKeyName.RenderCSharpDeclaring());
                        builder.TypeScriptDataTypes.Add(refTargetKeyName.RenderTypeScriptDeclaring());
                        builder.ControllerActions.Add(keywordSearching.RenderController());
                        builder.AppServiceMethods.Add(keywordSearching.RenderAppSrvMethod());
                    }

                    // DbContext
                    builder.HasDbSet = true;
                    var dbContextClass = new DbContextClass(context.Config);

                    foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                        // OnModelCreating
                        builder.OnModelCreating.Add(modelBuilder => dbContextClass.RenderEntity(modelBuilder, aggregate));

                        // DbEntity
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
