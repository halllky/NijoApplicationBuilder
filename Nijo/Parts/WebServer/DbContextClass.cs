using Nijo.Core;
using Nijo.Util.DotnetEx;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using Nijo.Util.CodeGenerating;

namespace Nijo.Parts.WebServer {
    internal class DbContextClass {
        internal DbContextClass(Config config) {
            _config = config;
        }
        private readonly Config _config;

        internal SourceFile RenderDeclaring(CodeRenderingContext ctx, Infrastructure infrastucture) => new SourceFile {
            FileName = $"{_config.DbContextName.ToFileNameSafe()}.cs",
            RenderContent = () => {
                var onModelCreatings = infrastucture._itemsByAggregate
                    .Where(x => x.Value.OnModelCreating.Any())
                    .Select(x => $"OnModelCreating_{x.Key.Item.ClassName}");

                return $$"""
                    using Microsoft.EntityFrameworkCore;

                    namespace {{ctx.Config.DbContextNamespace}} {

                        public partial class {{ctx.Config.DbContextName}} : DbContext {
                            public {{ctx.Config.DbContextName}}(DbContextOptions<{{ctx.Config.DbContextName}}> options) : base(options) { }

                            /// <inheritdoc />
                            protected override void OnModelCreating(ModelBuilder modelBuilder) {
                    {{onModelCreatings.SelectTextTemplate(method => $$"""
                                this.{{method}}(modelBuilder);
                    """)}}
                            }

                            /// <inheritdoc />
                            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
                                optionsBuilder.LogTo(sql => {
                                    if (OutSqlToVisualStudio) {
                                        System.Diagnostics.Debug.WriteLine("---------------------");
                                        System.Diagnostics.Debug.WriteLine(sql);
                                    }
                                }, LogLevel.Information);
                            }
                            /// <summary>デバッグ用</summary>
                            public bool OutSqlToVisualStudio { get; set; } = false;
                        }

                    }
                    """;
            },
        };

        internal string RenderEntity(string modelBuilder, GraphNode<Aggregate> aggregate) {
            return $$"""
                {{modelBuilder}}.Entity<{{_config.EntityNamespace}}.{{aggregate.Item.EFCoreEntityClassName}}>(entity => {

                    entity.HasKey(e => new {
                {{aggregate.GetColumns().Where(x => x.Options.IsKey).SelectTextTemplate(pk => $$"""
                        e.{{pk.Options.MemberName}},
                """)}}
                    });

                {{aggregate.GetColumns().SelectTextTemplate(col => $$"""
                    entity.Property(e => e.{{col.Options.MemberName}})
                        .IsRequired({{(col.Options.IsRequired ? "true" : "false")}});
                """)}}

                    {{WithIndent(RenderNavigationPropertyOnModelCreating(aggregate.As<Aggregate>()), "    ")}}
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
