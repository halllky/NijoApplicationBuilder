using Nijo.Core;
using Nijo.Util.DotnetEx;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Nijo.Parts;
using Nijo.Parts.WebClient;
using Nijo.Features;
using Nijo.Features.BatchUpdate;
using Nijo.Features.Storing;

namespace Nijo.Models {
    internal class WriteModel : IModel {

        void IModel.GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            // Create
            var createCommand = new CommandFeature();
            var createFeature = new CreateFeature(rootAggregate);
            createCommand.CommandName = root => $"{root.Item.PhysicalName}新規作成";
            createCommand.ActionName = root => $"{root.Item.PhysicalName}作成";
            createCommand.ControllerAction = createFeature.RenderController();
            createCommand.AppSrvMethod = createFeature.RenderAppSrvMethod();
            createCommand.GenerateCode(context, rootAggregate);

            context.CoreLibrary.UseAggregateFile(rootAggregate, builder => {

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
                    var aggregateDetail = new DataClassForSave(aggregate);
                    var initializerFunc = new TSInitializerFunction(aggregate);
                    builder.DataClassDeclaring.Add(aggregateDetail.RenderCSharp(context));
                    builder.TypeScriptDataTypes.Add(aggregateDetail.RenderTypeScript(context));
                    builder.TypeScriptDataTypes.Add(initializerFunc.Render());
                    // TODO: ディープイコール不要かもしれない
                    //if (aggregate.IsRoot()) builder.TypeScriptDataTypes.Add(aggregateDetail.RenderTsDeepEquals());
                }

                // Load
                var loadFeature = new FindManyFeature(rootAggregate);
                builder.ControllerActions.Add(loadFeature.RenderController());
                builder.AppServiceMethods.Add(loadFeature.RenderAppSrvMethod(context));
                builder.DataClassDeclaring.Add(loadFeature.RenderSearchConditionTypeDeclaring(csharp: true));
                builder.TypeScriptDataTypes.Add(loadFeature.RenderSearchConditionTypeDeclaring(csharp: false));
                builder.TypeScriptDataTypes.Add(loadFeature.RenderTypeScriptConditionInitializerFn());

                // KeywordSearching
                foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                    var keywordSearching = new KeywordSearchingFeature(aggregate);
                    var refTargetKeyName = new DataClassForSaveRefTarget(aggregate);
                    builder.DataClassDeclaring.Add(refTargetKeyName.RenderCSharpDeclaring());
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
                    static IEnumerable<NavigationProperty.PrincipalOrRelevant> EnumerateNavigationProperties(GraphNode<Aggregate> aggregate) {
                        foreach (var nav in aggregate.GetNavigationProperties()) {
                            if (nav.Principal.Owner == aggregate) yield return nav.Principal;
                            if (nav.Relevant.Owner == aggregate) yield return nav.Relevant;
                        }
                    }
                    builder.DataClassDeclaring.Add($$"""
                        /// <summary>
                        /// Entity Framework Core のルールに則った{{aggregate.Item.DisplayName}}のデータ型
                        /// </summary>
                        public partial class {{aggregate.Item.EFCoreEntityClassName}} {
                        {{aggregate.GetMembers().OfType<AggregateMember.ValueMember>().SelectTextTemplate(col => $$"""
                            public {{col.Options.MemberType.GetCSharpTypeName()}}? {{col.MemberName}} { get; set; }
                        """)}}

                        {{EnumerateNavigationProperties(aggregate).SelectTextTemplate(nav => $$"""
                            public virtual {{nav.CSharpTypeName}} {{nav.PropertyName}} { get; set; }
                        """)}}

                            /// <summary>このオブジェクトと比較対象のオブジェクトの主キーが一致するかを返します。</summary>
                            public bool {{Aggregate.KEYEQUALS}}({{aggregate.Item.EFCoreEntityClassName}} entity) {
                        {{aggregate.GetKeys().OfType<AggregateMember.ValueMember>().SelectTextTemplate(col => $$"""
                                if (entity.{{col.MemberName}} != this.{{col.MemberName}}) return false;
                        """)}}
                                return true;
                            }
                        }
                        """);
                }

                // SingleView
                var singleViewDataClass = new DataClassForDisplay(rootAggregate);
                builder.DataClassDeclaring.Add(singleViewDataClass.RenderCSharpDataClassDeclaration());
                builder.TypeScriptDataTypes.Add(singleViewDataClass.RenderTypeScriptDataClassDeclaration());
                builder.TypeScriptDataTypes.Add(singleViewDataClass.RenderConvertFnToLocalRepositoryType());
                foreach (var agg in rootAggregate.EnumerateThisAndDescendants()) {
                    var refTargetDataClass = new DataClassForDisplayRefTarget(agg);
                    builder.DataClassDeclaring.Add(refTargetDataClass.RenderCsDeclaring());
                    builder.TypeScriptDataTypes.Add(refTargetDataClass.RenderTsDeclaring());
                }
            });

            var editableMultiView = new MultiViewEditable(rootAggregate);
            var detailView = new SingleView(rootAggregate, SingleView.E_Type.View);
            var editView = new SingleView(rootAggregate, SingleView.E_Type.Edit);

            context.ReactProject.AddPage(editableMultiView);
            context.ReactProject.AddPage(detailView);
            context.ReactProject.AddPage(editView);

            context.ReactProject.PagesDir(pageDir => {
                pageDir.Directory(rootAggregate.Item.DisplayName.ToFileNameSafe(), aggregateDir => {
                    aggregateDir.Generate(detailView.Render());
                    aggregateDir.Generate(editView.Render());
                });
            });
            context.ReactProject.UtilDir(utilDir => {
                utilDir.Generate(LocalRepository.UseLocalRepositoryCommitHandling(context));
                utilDir.Generate(LocalRepository.RenderUseAggregateLocalRepository());
            });

            // 一括アップデート
            if (!context.Config.DisableBatchUpdate) {
                var batchUpdate = new BatchUpdateFeature();
                batchUpdate.GenerateCode(context);
            }
        }
    }
}
