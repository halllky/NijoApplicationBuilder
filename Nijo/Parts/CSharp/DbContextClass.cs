using Nijo.CodeGenerating;
using Nijo.Models.DataModelModules;
using Nijo.Util.DotnetEx;
using Nijo.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Nijo.Parts.CSharp {
    public class DbContextClass : IMultiAggregateSourceFile {

        private const string ON_DBCONTEXT_MODEL_CREATING = "OnModelCreating";
        private const string ON_DBCONTEXT_CONFIGURE_CONVENSIONS = "ConfigureConventions";
        private const string ON_DBCONTEXT_CONFIGURING = "OnConfiguringDbContext";

        private readonly List<EFCoreEntity> _efCoreEntities = [];
        private readonly List<string> _configureConversions = [];
        private readonly Lock _lock = new();

        internal DbContextClass AddEntities(IEnumerable<EFCoreEntity> eFCoreEntities) {
            lock (_lock) {
                _efCoreEntities.AddRange(eFCoreEntities);
                return this;
            }
        }
        public DbContextClass AddConfigureConventions(string sourceCode) {
            lock (_lock) {
                _configureConversions.Add(sourceCode);
                return this;
            }
        }

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            ctx.Use<ApplicationConfigure>()

                // 接続先設定
                .AddCoreMethod($$"""
                    /// <summary>
                    /// DBコンテキストの OnConfiguring メソッドから呼ばれる。
                    /// DB接続先設定などを行なう。
                    /// </summary>
                    public abstract void {{ON_DBCONTEXT_CONFIGURING}}(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder optionsBuilder, NLog.Logger logger);
                    """)

                // DI設定
                .AddCoreConfigureServices(services => $$"""
                    // DBコンテキスト(Entity Framework Core)
                    {{services}}.AddDbContext<{{ctx.Config.DbContextName}}>(ConfigureDbContext);
                    """)
                .AddCoreMethod($$"""
                    /// <summary>
                    /// DBコンテキスト(Entity Framework Core)
                    /// </summary>
                    protected abstract void ConfigureDbContext(IServiceProvider services, Microsoft.EntityFrameworkCore.DbContextOptionsBuilder options);
                    """)

                // 生成後のプロジェクトでOnModelCreating等をカスタマイズできるようにしておく
                .AddCoreMethod($$"""
                    /// <summary>
                    /// Entity Framework Core の定義にカスタマイズを加えます。
                    /// 既定のモデル定義処理の一番最後に呼ばれます。
                    /// データベース全体に対する設定を行なうことを想定しています。
                    /// （例えば、全テーブルの列挙体のDB保存される型を数値でなく文字列にするなど）
                    /// </summary>
                    /// <param name="modelBuilder">モデルビルダー。Entity Framework Core 公式の解説を参照のこと。</param>
                    public virtual void {{ON_DBCONTEXT_MODEL_CREATING}}(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
                        // 何か処理がある場合はこのメソッドをオーバーライドして記述してください。
                    }
                    """)
                .AddCoreMethod($$"""
                    /// <summary>
                    /// Entity Framework Core の <see cref="DbContext.ConfigureConventions"/> メソッドから呼ばれます。
                    /// 主にC#の値とDBのカラムの値の変換処理を定義します。
                    /// </summary>
                    /// <param name="configurationBuilder">Entity Framework Core 公式の解説を参照のこと。</param>
                    public virtual void {{ON_DBCONTEXT_CONFIGURE_CONVENSIONS}}(Microsoft.EntityFrameworkCore.ModelConfigurationBuilder configurationBuilder) {
                        // 何か処理がある場合はこのメソッドをオーバーライドして記述してください。
                    }
                    """);
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            ctx.CoreLibrary(dir => {
                dir.Directory("Util", efcoreDir => {
                    efcoreDir.Generate(Render(ctx));
                });
            });
        }

        private SourceFile Render(CodeRenderingContext ctx) {
            var efCoreEntitiesOrderByDataFlow = _efCoreEntities
                .OrderBy(e => e.Aggregate.GetRoot().GetIndexOfDataFlow())
                .ThenBy(e => e.Aggregate.GetOrderInTree())
                .ToArray();

            return new SourceFile {
                FileName = $"{ctx.Config.DbContextName.ToFileNameSafe()}.cs",
                Contents = $$"""
                    using Microsoft.EntityFrameworkCore;

                    namespace {{ctx.Config.RootNamespace}};

                    public partial class {{ctx.Config.DbContextName}} : DbContext {

                    #pragma warning disable CS8618 // DbSetはEFCore側で自動的に設定されるため問題なし
                        /// <summary>
                        /// DBコンテキスト。Entity Framework Core の中核的な仕組み。
                        /// </summary>
                        /// <param name="options">EFCoreのオプション</param>
                        /// <param name="nijoConfig">ソースコード自動生成に関するオプション</param>
                        /// <param name="logger">SQLのログ出力用</param>
                        public {{ctx.Config.DbContextName}}(DbContextOptions<{{ctx.Config.DbContextName}}> options, {{ApplicationConfigure.ABSTRACT_CLASS_CORE}} nijoConfig, NLog.Logger logger) : base(options) {
                            _nijoConfig = nijoConfig;
                            _logger = logger;
                        }
                    #pragma warning restore CS8618 // DbSetはEFCore側で自動的に設定されるため問題なし

                        private readonly {{ApplicationConfigure.ABSTRACT_CLASS_CORE}} _nijoConfig;
                        private readonly NLog.Logger _logger;

                    {{efCoreEntitiesOrderByDataFlow.SelectTextTemplate(entity => $$"""
                        public virtual DbSet<{{entity.CsClassName}}> {{entity.DbSetName}} { get; set; }
                    """)}}

                        /// <inheritdoc />
                        protected override void OnModelCreating(ModelBuilder modelBuilder) {
                            try {
                                // 集約ごとのモデル定義
                    {{efCoreEntitiesOrderByDataFlow.SelectTextTemplate(entity => $$"""
                                _nijoConfig.{{entity.OnModelCreatingAutoGenerated}}(this, modelBuilder);
                    """)}}

                                // モデル定義のカスタマイズ
                                _nijoConfig.{{ON_DBCONTEXT_MODEL_CREATING}}(modelBuilder);

                            } catch (Exception ex) {
                                _logger.Error(ex);
                                throw;
                            }
                        }

                        /// <inheritdoc />
                        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
                            _nijoConfig.{{ON_DBCONTEXT_CONFIGURE_CONVENSIONS}}(configurationBuilder);
                        }

                        /// <inheritdoc />
                        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
                            _nijoConfig.{{ON_DBCONTEXT_CONFIGURING}}(optionsBuilder, _logger);
                        }

                    }
                    """,
            };
        }
    }
}
