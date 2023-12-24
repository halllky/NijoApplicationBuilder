using Nijo.Core;
using Nijo.DotnetEx;
using Nijo.Features.InstanceHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    internal class MasterDataFeature : NijoFeatureBaseByAggregate {

        public override void GenerateCode(ICodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var createCommand = new Command.CommandFeature();
            createCommand.CommandName = root => $"{root.Item.ClassName}新規作成";
            createCommand.ActionName = root => $"{root.Item.ClassName}作成";
            createCommand.GenerateCode(context, rootAggregate);

            var searchFeature = new Searching.AggregateSearchFeature2();
            searchFeature.GenerateCode(context, rootAggregate);

            context.Render<Infrastucture>(infra => {
                infra.Aggregate(rootAggregate, builder => {
                    builder.HasDbSet = true;
                    builder.OnModelCreating.Add(modelBuilder => new DbContextClass(context.Config).RenderEntity(modelBuilder, rootAggregate));

                    // DbEntity
                    static IEnumerable<NavigationProperty.Item> EnumerateNavigationProperties(GraphNode<Aggregate> aggregate) {
                        foreach (var nav in aggregate.GetNavigationProperties()) {
                            if (nav.Principal.Owner == aggregate) yield return nav.Principal;
                            if (nav.Relevant.Owner == aggregate) yield return nav.Relevant;
                        }
                    }
                    foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
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

            context.EditReactDirectory(reactDir => {
                reactDir.Directory("pages", pageDir => {
                    pageDir.Directory(rootAggregate.Item.DisplayName.ToFileNameSafe(), aggregateDir => {
                        aggregateDir.Generate(new SingleView(rootAggregate, SingleView.E_Type.View).Render());
                        aggregateDir.Generate(new SingleView(rootAggregate, SingleView.E_Type.Edit).Render());
                    });
                });
            });
        }
    }
}
