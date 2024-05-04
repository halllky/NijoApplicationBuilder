using Nijo.Parts.WebServer;
using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Features.Storing;

namespace Nijo.Models {
    internal class ReadModel : IModel {

        private const string CONTROLLER_ACTION_NAME = "reload";

        void IModel.GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {

            context.UseAggregateFile(rootAggregate, builder => {

                // 洗い替え処理
                // ※以下の理由からほぼなにも自動生成しない。オーバーライドして処理を自前実装する前提とする
                // - 一度の更新でどのエンティティまでIncludeすればよいかの自動判別が難しい
                // - ReadModelの主キーが必ずしもWriteModelの集約1種類へのRefのみで構成されるとは限らない
                // - どういったタイミングで削除されるかの判断が難しい
                var appSrv = new ApplicationService();
                builder.ControllerActions.Add($$"""
                    [HttpPost("{{CONTROLLER_ACTION_NAME}}")]
                    public virtual IActionResult ReloadAll() {
                        _applicationService.ReloadAll{{rootAggregate.Item.ClassName}}();
                        return Ok();
                    }
                    """);
                builder.AppServiceMethods.Add($$"""
                    /// <summary>
                    /// {{rootAggregate.Item.DisplayName}}のデータの洗い替え処理。
                    /// </summary>
                    public virtual void ReloadAll{{rootAggregate.Item.ClassName}}() {
                        using (var tran = {{appSrv.DbContext}}.Database.BeginTransaction()) {
                            try {
                                {{appSrv.DbContext}}.{{rootAggregate.Item.DbSetName}}
                                    .ExecuteDelete();
                                {{appSrv.DbContext}}.{{rootAggregate.Item.DbSetName}}
                                    .AddRange(Recalculate{{rootAggregate.Item.ClassName}}());
                                {{appSrv.DbContext}}
                                    .SaveChanges();
                                tran.Commit();
                            } catch {
                                tran.Rollback();
                                throw;
                            }
                        }
                    }
                    /// <summary>
                    /// {{rootAggregate.Item.DisplayName}}のデータの計算処理。
                    /// </summary>
                    public virtual IEnumerable<{{rootAggregate.Item.EFCoreEntityClassName}}> Recalculate{{rootAggregate.Item.ClassName}}() {
                        /// <see cref="{{appSrv.ConcreteClass}}"/>クラスでこのメソッドをオーバーライドして、
                        /// すべての{{rootAggregate.Item.DisplayName}}を算出する処理を実装してください。

                        return Enumerable.Empty<{{rootAggregate.Item.EFCoreEntityClassName}}>();
                    }
                    """);

                // Load & MultiView
                var loadFeature = new FindManyFeature(rootAggregate);
                builder.ControllerActions.Add(loadFeature.RenderController());
                builder.AppServiceMethods.Add(loadFeature.RenderAppSrvMethod());
                builder.DataClassDeclaring.Add(loadFeature.RenderSearchConditionTypeDeclaring(csharp: true));
                builder.TypeScriptDataTypes.Add(loadFeature.RenderSearchConditionTypeDeclaring(csharp: false));

                var controller = new Parts.WebClient.Controller(rootAggregate.Item);
                var editableMultiView = new MultiViewEditable(rootAggregate, new MultiViewEditable.Options {
                    ReadOnly = true,
                    Hooks = $$"""
                        const handleRecalculateClick = useCallback(async () => {
                          const res = await post(`/{{controller.SubDomain}}/{{CONTROLLER_ACTION_NAME}}`)
                          if (res.ok) {
                            await reloadRemoteItems()
                            dispatchMsg(msg => msg.info('洗い替え処理が完了しました。'))
                          } else {
                            dispatchMsg(msg => msg.error('洗い替え処理でエラーが発生しました。'))
                          }
                        }, [post, reloadRemoteItems])
                        """,
                    PageTitleSide = $$"""
                        <Input.Button onClick={handleRecalculateClick}>全件洗い替え(デバッグ用)</Input.Button>
                        """,
                });
                context.AddPage(editableMultiView);

                // Find & SingleView
                var findFeature = new FindFeature(rootAggregate);
                builder.ControllerActions.Add(findFeature.RenderController());
                builder.AppServiceMethods.Add(findFeature.RenderAppSrvMethod());

                var singleView = new SingleView(rootAggregate, SingleView.E_Type.View);
                context.AddPage(singleView);

                // AggregateDetailクラス定義を作成する
                foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                    var aggregateDetail = new Features.Storing.TransactionScopeDataClass(aggregate);
                    builder.DataClassDeclaring.Add(aggregateDetail.RenderCSharp(context));
                    builder.TypeScriptDataTypes.Add(aggregateDetail.RenderTypeScript(context));
                }

                // EFCoreエンティティ定義を作成する
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
                        /// {{aggregate.Item.DisplayName}}のデータベースに保存されるデータの形を表すクラスです。
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
            });

        }

    }
}
