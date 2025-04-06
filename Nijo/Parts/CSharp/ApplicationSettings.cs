using Nijo.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.CSharp {
    /// <summary>
    /// アプリケーション実行設定ファイルの項目のうち
    /// 自動生成されるコードの中で必要になるもの
    /// </summary>
    public class ApplicationSettings : IMultiAggregateSourceFile {

        public const string INTERFACE_NAME = "IApplicationSettings";

        private readonly List<string> _sourceCode = [];
        public ApplicationSettings AddProperty(string sourceCode) {
            _sourceCode.Add(sourceCode);
            return this;
        }

        void IMultiAggregateSourceFile.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
            // DI設定
            ctx.Use<ApplicationConfigure>()
                .AddCoreConfigureServices(services => $$"""
                    // 実行時設定ファイル
                    {{services}}.AddScoped(ConfigureApplicationSettings);
                    """)
                .AddCoreMethod($$"""
                    /// <summary>
                    /// 実行時設定をどこから参照するかの定義。
                    /// </summary>
                    protected abstract {{INTERFACE_NAME}} ConfigureApplicationSettings(IServiceProvider services);
                    """);
        }

        void IMultiAggregateSourceFile.Render(CodeRenderingContext ctx) {
            ctx.CoreLibrary(dir => {
                dir.Directory("Util", utilDir => {
                    utilDir.Generate(RenderInterface(ctx));
                });
            });
        }

        private SourceFile RenderInterface(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "IApplicationSettings.cs",
                Contents = $$"""
                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// アプリケーション実行設定ファイルの項目のうち
                    /// 自動生成されるコードの中で必要になるもの
                    /// </summary>
                    public interface {{INTERFACE_NAME}} {
                    {{_sourceCode.SelectTextTemplate(source => $$"""
                        {{WithIndent(source, "    ")}}
                    """)}}
                    }
                    """,
            };
        }
    }
}
