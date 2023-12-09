using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HalApplicationBuilder.Features.TemplateTextHelper;

namespace HalApplicationBuilder.Features.EFCore {
    partial class DbContext : TemplateBase {
        internal DbContext(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public override string FileName => $"{_ctx.Config.DbContextName.ToFileNameSafe()}.cs";

        protected override string Template() {
            var dbEntities = _ctx.Schema
                .AllAggregates()
                .Select(agg => agg.As<IEFCoreEntity>());

            return $$"""
                using Microsoft.EntityFrameworkCore;

                namespace {{_ctx.Config.DbContextNamespace}} {

                    public partial class {{_ctx.Config.DbContextName}} : DbContext {
                        public {{_ctx.Config.DbContextName}}(DbContextOptions<{{_ctx.Config.DbContextName}}> options) : base(options) { }

                        /// <inheritdoc />
                        protected override void OnModelCreating(ModelBuilder modelBuilder) {

                            {{WithIndent(dbEntities.Select(RenderEntity), "            ")}}

                            {{_ctx.Config.EntityNamespace}}.BackgroundTaskEntity.OnModelCreating(modelBuilder);
                        }
                    }
                }
                """;
        }

        private string RenderEntity(GraphNode<IEFCoreEntity> dbEntity) {
            return $$"""
                modelBuilder.Entity<{{_ctx.Config.EntityNamespace}}.{{dbEntity.Item.ClassName}}>(entity => {
            
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
