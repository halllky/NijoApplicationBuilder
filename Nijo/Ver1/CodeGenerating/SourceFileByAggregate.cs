using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Webapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.CodeGenerating {
    /// <summary>
    /// 機能単位などではなく集約単位でソースコードが記載されるファイル
    /// </summary>
    public class SourceFileByAggregate {

        public SourceFileByAggregate(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        private readonly List<string> _appSrvMethods = [];
        private readonly List<string> _csharpClass = [];
        private readonly List<string> _webApiControllerAction = [];
        private readonly List<string> _typeScriptSource = [];

        public void AddAppSrvMethod(string sourceCode) {
            _appSrvMethods.Add(sourceCode);
        }
        public void AddCSharpClass(string sourceCode) {
            _csharpClass.Add(sourceCode);
        }
        public void AddWebapiControllerAction(string sourceCode) {
            _webApiControllerAction.Add(sourceCode);
        }
        public void AddTypeScriptSource(string sourceCode) {
            _typeScriptSource.Add(sourceCode);
        }

        public void ExecuteRendering(CodeRenderingContext ctx) {

            ctx.CoreLibrary(dir => {
                dir.Generate(new SourceFile {
                    FileName = $"{_rootAggregate.PhysicalName.ToFileNameSafe()}.cs",
                    Contents = RenderCoreLibrary(ctx),
                });
            });
            ctx.WebapiProject(dir => {
                dir.Generate(new SourceFile {
                    FileName = $"{_rootAggregate.PhysicalName.ToFileNameSafe()}.cs",
                    Contents = RenderWebapi(ctx),
                });
            });
            ctx.ReactProject(dir => {
                dir.Generate(new SourceFile {
                    FileName = $"{_rootAggregate.PhysicalName.ToFileNameSafe()}.ts",
                    Contents = RenderNodeJs(ctx),
                });
            });
        }

        private string RenderCoreLibrary(CodeRenderingContext ctx) {
            return $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.ComponentModel;
                using System.ComponentModel.DataAnnotations;
                using System.Linq;
                using System.Text.Json.Nodes;
                using System.Text.Json.Serialization;
                using Microsoft.EntityFrameworkCore;
                using Microsoft.EntityFrameworkCore.Infrastructure;

                namespace {{ctx.Config.RootNamespace}};

                partial class {{ApplicationService.ABSTRACT_CLASS}} {
                {{_appSrvMethods.SelectTextTemplate(source => $$"""
                    {{WithIndent(source, "    ")}}

                """)}}
                }

                {{_csharpClass.SelectTextTemplate(source => $$"""
                {{WithIndent(source, "")}}

                """)}}
                """;
        }
        private string RenderWebapi(CodeRenderingContext ctx) {
            var controller = new AspNetController(_rootAggregate);

            return $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.ComponentModel;
                using System.ComponentModel.DataAnnotations;
                using System.Linq;
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.EntityFrameworkCore;
                using Microsoft.EntityFrameworkCore.Infrastructure;

                namespace {{ctx.Config.RootNamespace}};

                /// <summary>
                /// {{_rootAggregate.DisplayName}}に関する Web API 操作を提供する ASP.NET Core のコントローラー
                /// </summary>
                [ApiController]
                [Route("{{controller.Route}}")]
                public partial class {{controller.CsClassName}} : ControllerBase {
                    public {{controller.CsClassName}}({{ApplicationService.CONCRETE_CLASS}} applicationService) {
                        _applicationService = applicationService;
                    }
                    protected readonly {{ApplicationService.CONCRETE_CLASS}} _applicationService;

                    {{WithIndent(_webApiControllerAction, "        ")}}
                }
                """;
        }
        private string RenderNodeJs(CodeRenderingContext ctx) {
            // 1つ隣のref-toはimportする必要がある
            var refTos = _rootAggregate
                .EnumerateThisAndDescendants()
                .SelectMany(agg => agg.GetMembers())
                .OfType<RefToMember>()
                .Select(@ref => new {
                    FileName = $"./{@ref.RefTo.GetRoot().PhysicalName}",
                    RefTo = new Models.QueryModelModules.DisplayDataRefEntry(@ref.RefTo),
                    RefSC = new Models.QueryModelModules.SearchConditionRefEntry(@ref.RefTo),
                });

            return $$"""
                import React from "react"
                import * as ReactRouter from "react-router-dom"
                import { UUID } from "uuidjs"
                import * as Util from "../util2"
                import * as Constraints from "./util/constraints"
                {{refTos.SelectTextTemplate(r => $$"""
                import { {{r.RefTo.TsTypeName}}, {{r.RefTo.TsNewObjectFunction}}, {{r.RefSC.TsFilterTypeName}} } from "{{r.FileName}}"
                """)}}
                {{_typeScriptSource.SelectTextTemplate(source => $$"""

                {{source}}
                """)}}
                """;
        }
    }
}
