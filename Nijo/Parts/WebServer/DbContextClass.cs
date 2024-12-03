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
    internal class DbContextClass : ISummarizedFile {

        /// <summary>DbSet追加</summary>
        internal void AddDbSet(string className, string propName) {
            _dbSet.Add((className, propName));
        }
        private readonly List<(string ClassName, string PropName)> _dbSet = new();

        /// <summary>OnModelCreatingメソッドに生のソースを追加する。関数の引数はmodelBuilderの変数名。</summary>
        internal void AddOnModelCreating(Func<string, string> render) {
            _onModelCreating.Add(render);
        }
        private readonly List<Func<string, string>> _onModelCreating = new();

        /// <summary>
        /// プロパティの型のコンバータを登録する。
        /// 二重ループの処理をレンダリングことによるパフォーマンスの都合上、
        /// <see cref="AddOnModelCreating"/> とは別のメソッドになっている。
        /// </summary>
        /// <param name="className">変換対象の型の名前</param>
        /// <param name="configureClassMethodName">コンフィグクラスのメソッドの名前</param>
        internal void AddOnModelCreatingPropConverter(string className, string configureClassMethodName) {
            _onModelCreatingPropValueConverter.Add(className, configureClassMethodName);
        }
        private readonly Dictionary<string, string> _onModelCreatingPropValueConverter = new();

        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {

            context.UseSummarizedFile<Configure>().AddMethod($$"""
                /// <summary>
                /// Entity Framework Core の定義にカスタマイズを加えます。
                /// 既定のモデル定義処理の一番最後に呼ばれます。
                /// データベース全体に対する設定を行うことを想定しています。（例えば、全テーブルの列挙体のDB保存される型を数値ではなく文字列にする、など）
                /// </summary>
                /// <param name="modelBuilder">モデルビルダー。Entity Framework Core 公式の解説を参照のこと。</param>
                public virtual void {{ON_DBCONTEXT_MODEL_CREATING}}(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
                }
                """);

            context.CoreLibrary.AutoGeneratedDir(genDir => {
                genDir.Directory("EntityFramework", efDir => {
                    efDir.Generate(RenderDeclaring());
                    efDir.Generate(RenderFactoryForMigration());
                });
            });
        }

        private SourceFile RenderDeclaring() => new SourceFile {
            FileName = $"EFCoreDbContext.cs",
            RenderContent = ctx => {
                var app = new ApplicationService();

                return $$"""
                    using Microsoft.EntityFrameworkCore;
                    using Microsoft.Extensions.Logging;

                    namespace {{ctx.Config.DbContextNamespace}} {

                        /// <summary>
                        /// DBコンテキスト。データベース全体と対応する抽象。
                        /// 詳しくは Entity Framework Core で調べてください。
                        /// </summary>
                        public partial class {{ctx.Config.DbContextName}} : DbContext {
                    #pragma warning disable CS8618 // DbSetはEFCore側で自動的に設定されるため問題なし
                            public {{ctx.Config.DbContextName}}(DbContextOptions<{{ctx.Config.DbContextName}}> options) : base(options) { }
                    #pragma warning restore CS8618 // DbSetはEFCore側で自動的に設定されるため問題なし

                    {{_dbSet.SelectTextTemplate(dbSet => $$"""
                            public virtual DbSet<{{dbSet.ClassName}}> {{dbSet.PropName}} { get; set; }
                    """)}}

                            /// <inheritdoc />
                            protected override void OnModelCreating(ModelBuilder modelBuilder) {
                                var customizedConfigure = new {{app.ConcreteClassName}}.{{Configure.CONCRETE_CLASS_NAME}}();

                                // 集約ごとのモデル定義
                    {{_onModelCreating.SelectTextTemplate(fn => $$"""
                                {{WithIndent(fn("modelBuilder"), "            ")}}
                    """)}}
                    {{If(_onModelCreatingPropValueConverter.Count > 0, () => $$"""

                                // 自前で定義したプロパティ型の、DBとC#の間の変換処理を定義する
                                foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
                                    foreach (var property in entityType.GetProperties()) {
                    {{_onModelCreatingPropValueConverter.SelectTextTemplate((kv, i) => $$"""
                                        {{(i == 0 ? "if" : "} else if")}} (property.ClrType == typeof({{kv.Key}})) {
                                            property.SetValueConverter(customizedConfigure.{{kv.Value}}());
                    """)}}
                                        }
                                    }
                                }
                    """)}}

                                // モデル定義に変更を加えたい場合は {{Configure.CONCRETE_CLASS_NAME}} クラスでこのメソッドをオーバーライドしてください。
                                customizedConfigure.{{ON_DBCONTEXT_MODEL_CREATING}}(modelBuilder);
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

        private SourceFile RenderFactoryForMigration() => new SourceFile {
            FileName = $"EFCoreDbContextFactoryForMigration.cs",
            RenderContent = ctx => {
                var app = new ApplicationService();

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
                                new {{app.ConcreteClassName}}.{{Configure.CONCRETE_CLASS_NAME}}().{{Configure.CONFIGURE_SERVICES}}(serviceCollection);
                                var services = serviceCollection.BuildServiceProvider();
                                return services.GetRequiredService<{{ctx.Config.DbContextName}}>();
                            }
                        }
                    }
                    """;
            },
        };

        private const string ON_DBCONTEXT_MODEL_CREATING = "OnModelCreating";
    }
}
