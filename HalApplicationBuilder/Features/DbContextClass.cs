using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.Features.TemplateTextHelper;

namespace HalApplicationBuilder.Features {
    internal class DbContextClass {
        internal DbContextClass(Config config) {
            _config = config;
        }
        private readonly Config _config;

        internal SourceFile RenderDeclaring() => new SourceFile {
            FileName = $"{_config.DbContextName.ToFileNameSafe()}.cs",
            RenderContent = ctx => {
                var dbEntities = ctx.Schema
                    .AllAggregates()
                    .Where(agg => agg.IsStored())
                    .Select(agg => agg.As<IEFCoreEntity>());

                return $$"""
                    using Microsoft.EntityFrameworkCore;

                    namespace {{ctx.Config.DbContextNamespace}} {

                        public partial class {{ctx.Config.DbContextName}} : DbContext {
                            public {{ctx.Config.DbContextName}}(DbContextOptions<{{ctx.Config.DbContextName}}> options) : base(options) { }

                            /// <inheritdoc />
                            protected override void OnModelCreating(ModelBuilder modelBuilder) {

                                {{WithIndent(dbEntities.Select(RenderEntity), "            ")}}

                                //// バッチ処理
                                // {{ctx.Config.EntityNamespace}}.BackgroundTaskEntity.OnModelCreating(modelBuilder);
                            }
                        }
                    }
                    """;
            },
        };

        private string RenderEntity(GraphNode<IEFCoreEntity> dbEntity) {
            return $$"""
                modelBuilder.Entity<{{_config.EntityNamespace}}.{{dbEntity.Item.ClassName}}>(entity => {

                    entity.HasKey(e => new {
                {{dbEntity.GetColumns().Where(x => x.Options.IsKey).SelectTextTemplate(pk => $$"""
                        e.{{pk.Options.MemberName}},
                """)}}
                    });

                {{dbEntity.GetColumns().SelectTextTemplate(col => $$"""
                    entity.Property(e => e.{{col.Options.MemberName}})
                        .IsRequired({{(col.Options.IsRequired ? "true" : "false")}});
                """)}}

                {{If(dbEntity.Item is Aggregate, () => $$"""
                    {{WithIndent(RenderNavigationPropertyOnModelCreating(dbEntity.As<Aggregate>()), "    ")}}
                """)}}
                });
                """;
        }

        private IEnumerable<string> RenderNavigationPropertyOnModelCreating(GraphNode<Aggregate> aggregate) {
            foreach (var nav in aggregate.GetNavigationProperties()) {

                if (nav.Principal.Owner != aggregate) continue;

                // Has
                if (nav.Principal.OppositeIsMany) {
                    yield return $"entity.HasMany(e => e.{nav.Principal.PropertyName})";
                } else {
                    yield return $"entity.HasOne(e => e.{nav.Principal.PropertyName})";
                }

                // With
                if (nav.Relevant.OppositeIsMany) {
                    yield return $"    .WithMany(e => e.{nav.Relevant.PropertyName})";
                } else {
                    yield return $"    .WithOne(e => e.{nav.Relevant.PropertyName})";
                }

                // FK
                if (!nav.Principal.OppositeIsMany && !nav.Relevant.OppositeIsMany) {
                    // HasOneWithOneのときは型引数が要るらしい
                    yield return $"    .HasForeignKey<{nav.Relevant.Owner.Item.EFCoreEntityClassName}>(e => new {{";
                } else {
                    yield return $"    .HasForeignKey(e => new {{";
                }
                foreach (var fk in nav.Relevant.GetForeignKeys()) {
                    yield return $"        e.{fk.Options.MemberName},";
                }
                yield return $"    }})";

                // OnDelete
                yield return $"    .OnDelete({nameof(DeleteBehavior)}.{nav.OnPrincipalDeleted});";
            }
        }
    }
}
