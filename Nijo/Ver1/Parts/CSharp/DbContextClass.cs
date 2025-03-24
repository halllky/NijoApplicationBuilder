using Nijo.Util.DotnetEx;
using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.CSharp {
    public class DbContextClass : IMultiAggregateSourceFile {

        private readonly List<string> _dbSet = [];
        private readonly List<string> _onModelCreating = [];
        private readonly List<string> _configureConversions = [];

        public DbContextClass AddDbSet(string sourceCode) {
            _dbSet.Add(sourceCode);
            return this;
        }
        public DbContextClass AddOnModelCreating(string sourceCode) {
            _onModelCreating.Add(sourceCode);
            return this;
        }
        public DbContextClass AddConfigureConventions(string sourceCode) {
            _configureConversions.Add(sourceCode);
            return this;
        }

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // DI設定
            ctx.Use<ApplicationConfigure>().AddCoreMethod(
                services => $$"""
                    // DB接続設定
                    {{services}}.AddScoped(ConfigureDbContext);
                    """,
                $$"""
                    /// <summary>
                    /// DB接続設定
                    /// </summary>
                    protected abstract {{ctx.Config.DbContextName}} ConfigureDbContext(IServiceProvider services);
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
            return new SourceFile {
                FileName = $"{ctx.Config.DbContextName.ToFileNameSafe()}.cs",
                Contents = $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    public partial class {{ctx.Config.DbContextName}} {

                    {{_dbSet.SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "    ")}}
                    """)}}

                        // TODO ver.1
                    }
                    """,
            };
        }
    }
}
