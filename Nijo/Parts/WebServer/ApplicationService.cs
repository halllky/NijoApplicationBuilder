using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    public class ApplicationService {
        public string ClassName => $"AutoGeneratedApplicationService";
        internal string FileName => $"{ClassName}.cs";

        public string ServiceProvider = "ServiceProvider";
        public string DbContext = "DbContext";
        public string CurrentTime = "CurrentTime";

        internal SourceFile Render(CodeRenderingContext ctx, IEnumerable<string> methods) => new SourceFile {
            FileName = FileName,
            RenderContent = context => $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    using {{ctx.Config.DbContextNamespace}};

                    public partial class {{ClassName}} {
                        public {{ClassName}}(IServiceProvider serviceProvider) {
                            {{ServiceProvider}} = serviceProvider;
                        }

                        public IServiceProvider {{ServiceProvider}} { get; }

                        private {{ctx.Config.DbContextName}}? _dbContext;
                        public virtual {{ctx.Config.DbContextName}} {{DbContext}} => _dbContext ??= {{ServiceProvider}}.GetRequiredService<{{ctx.Config.DbContextName}}>();

                        private DateTime? _currentTime;
                        public virtual DateTime {{CurrentTime}} => _currentTime ??= DateTime.Now;

                        {{WithIndent(methods, "        ")}}
                    }
                }
                """,
        };


        public string ConcreteClass => $"OverridedApplicationService";
        internal string ConcreteClassFileName => $"{ConcreteClass}.cs";

        internal SourceFile RenderConcreteClass() => new() {
            FileName = ConcreteClassFileName,
            RenderContent = ctx => RenderConcreteClass(ctx.Config),
        };
        internal string RenderConcreteClass(Config config) => $$"""
            using Microsoft.EntityFrameworkCore;

            namespace {{config.RootNamespace}} {
                /// <summary>
                /// 自動生成された検索機能や登録機能を上書きする場合はこのクラス内でそのメソッドやプロパティをoverrideしてください。
                /// </summary>
                public partial class {{ConcreteClass}} : {{ClassName}} {
                    public {{ConcreteClass}}(IServiceProvider serviceProvider) : base(serviceProvider) { }

            {{config.OverridedApplicationServiceCodeForUnitTest.SelectTextTemplate(code => $$"""
                    {{WithIndent(code, "        ")}}

            """)}}
                }
            }
            """;
    }
}
