using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebServer {
    public sealed class AggregateFile {
        internal AggregateFile(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        // DbContext
        public bool HasDbSet { get; set; }
        public List<Func<string, string>> OnModelCreating { get; } = new();

        // AggregateRenderer
        public List<string> ControllerActions { get; } = new();
        public List<string> AppServiceMethods { get; } = new();
        public List<string> DataClassDeclaring { get; } = new();

        // react
        public List<string> TypeScriptDataTypes { get; } = new List<string>();

        internal SourceFile Render(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var controller = new Parts.WebClient.Controller(_aggregate.Item);

            return new SourceFile {
                FileName = $"{_aggregate.Item.DisplayName.ToFileNameSafe()}.cs",
                RenderContent = () => $$"""
                    namespace {{context.Config.RootNamespace}} {
                        using System;
                        using System.Collections;
                        using System.Collections.Generic;
                        using System.ComponentModel;
                        using System.ComponentModel.DataAnnotations;
                        using System.Linq;
                        using Microsoft.AspNetCore.Mvc;
                        using Microsoft.EntityFrameworkCore;
                        using Microsoft.EntityFrameworkCore.Infrastructure;
                        using {{context.Config.EntityNamespace}};

                        [ApiController]
                        [Route("{{Parts.WebClient.Controller.SUBDOMAIN}}/[controller]")]
                        public partial class {{controller.ClassName}} : ControllerBase {
                            public {{controller.ClassName}}(ILogger<{{controller.ClassName}}> logger, {{appSrv.ClassName}} applicationService) {
                                _logger = logger;
                                _applicationService = applicationService;
                            }
                            protected readonly ILogger<{{controller.ClassName}}> _logger;
                            protected readonly {{appSrv.ClassName}} _applicationService;

                            {{WithIndent(ControllerActions, "        ")}}
                        }


                        partial class {{appSrv.ClassName}} {
                            {{WithIndent(AppServiceMethods, "        ")}}
                        }


                    #region データ構造クラス
                        {{WithIndent(DataClassDeclaring, "    ")}}
                    #endregion データ構造クラス
                    }

                    namespace {{context.Config.DbContextNamespace}} {
                        using {{context.Config.RootNamespace}};
                        using Microsoft.EntityFrameworkCore;

                        partial class {{context.Config.DbContextName}} {
                    {{If(HasDbSet, () => _aggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => $$"""
                            public virtual DbSet<{{agg.Item.EFCoreEntityClassName}}> {{agg.Item.DbSetName}} { get; set; }
                    """))}}

                    {{If(OnModelCreating.Any(), () => $$"""
                            private void OnModelCreating_{{_aggregate.Item.ClassName}}(ModelBuilder modelBuilder) {
                                {{WithIndent(OnModelCreating.SelectTextTemplate(fn => fn.Invoke("modelBuilder")), "            ")}}
                            }
                    """)}}
                        }
                    }
                    """,
            };
        }
    }
}
