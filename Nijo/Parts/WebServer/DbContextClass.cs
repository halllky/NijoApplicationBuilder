using Nijo.Core;
using Nijo.Util.DotnetEx;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Parts.WebServer {
    internal class DbContextClass {
        internal DbContextClass(Config config) {
            _config = config;
        }
        private readonly Config _config;

        internal SourceFile RenderDeclaring() => new SourceFile {
            FileName = $"{_config.DbContextName.ToFileNameSafe()}.cs",
            RenderContent = ctx => {
                var onModelCreating = ctx.CoreLibrary._itemsByAggregate
                    .Where(x => x.Value.OnModelCreating.Any())
                    .Select(x => $"OnModelCreating_{x.Key.Item.PhysicalName}");

                return $$"""
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.Extensions.Logging;

                    namespace {{ctx.Config.DbContextNamespace}} {

                        public partial class {{ctx.Config.DbContextName}} : DbContext {
                            public {{ctx.Config.DbContextName}}(DbContextOptions<{{ctx.Config.DbContextName}}> options) : base(options) { }

                    {{ctx.CoreLibrary.DbSetPropNameAndClassName.SelectTextTemplate(kv => $$"""
                            public virtual DbSet<{{kv.Value}}> {{kv.Key}} { get; set; }
                    """)}}

                            /// <inheritdoc />
                            protected override void OnModelCreating(ModelBuilder modelBuilder) {
                    {{onModelCreating.SelectTextTemplate(method => $$"""
                                this.{{method}}(modelBuilder);
                    """)}}
                    {{ctx.CoreLibrary.DbContextOnModelCreating.SelectTextTemplate(fn => $$"""
                                {{WithIndent(fn("modelBuilder"), "            ")}}
                    """)}}
                                {{WithIndent(Parts.Utility.RuntimeDateClass.RenderEFCoreConversion("modelBuilder"), "            ")}}
                                {{WithIndent(Parts.Utility.RuntimeYearMonthClass.RenderEFCoreConversion("modelBuilder"), "            ")}}
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
                {{aggregate.GetKeys().OfType<AggregateMember.ValueMember>().SelectTextTemplate(pk => $$"""
                        e.{{pk.MemberName}},
                """)}}
                    });

                {{aggregate.GetMembers().OfType<AggregateMember.ValueMember>().SelectTextTemplate(col => $$"""
                    entity.Property(e => e.{{col.MemberName}})
                        .IsRequired({{(col.IsRequired ? "true" : "false")}});
                """)}}

                    {{WithIndent(RenderNavigationPropertyOnModelCreating(aggregate.As<Aggregate>()), "    ")}}
                });
                """;
        }

        private IEnumerable<string> RenderNavigationPropertyOnModelCreating(GraphNode<Aggregate> aggregate) {
            var efCoreEntity = new Models.WriteModel2Features.EFCoreEntity(aggregate);

            foreach (var nav in efCoreEntity.GetNavigationProperties()) {

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
                    yield return $"        e.{fk.MemberName},";
                }
                yield return $"    }})";

                // OnDelete
                yield return $"    .OnDelete({nameof(DeleteBehavior)}.{nav.OnPrincipalDeleted});";
            }
        }

        internal SourceFile RenderFactoryForMigration() => new SourceFile {
            FileName = $"{_config.DbContextName.ToFileNameSafe()}FactoryForMigration.cs",
            RenderContent = ctx => {
                return $$"""
                    using Microsoft.EntityFrameworkCore.Design;
                    using Microsoft.Extensions.DependencyInjection;
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Text;
                    using System.Threading.Tasks;

                    namespace {{ctx.Config.DbContextNamespace}} {
                        /// <summary>
                        /// DB定義更新スクリプト作成に関するコマンド `dotnet ef migrations add` の際に呼ばれるファクトリークラス
                        /// </summary>
                        internal class {{ctx.Config.DbContextName}}FactoryForMigration : IDesignTimeDbContextFactory<{{ctx.Config.DbContextName}}> {
                            public {{ctx.Config.DbContextName}} CreateDbContext(string[] args) {
                                var serviceCollection = new ServiceCollection();
                                {{Configure.CLASSNAME_CORE}}.{{Configure.CONFIGURE_SERVICES}}(serviceCollection);
                                var services = serviceCollection.BuildServiceProvider();
                                return services.GetRequiredService<{{ctx.Config.DbContextName}}>();
                            }
                        }
                    }
                    """;
            },
        };
    }
}
