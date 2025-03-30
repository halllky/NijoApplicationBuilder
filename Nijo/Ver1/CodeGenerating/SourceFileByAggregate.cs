using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.CSharp;
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
        private readonly List<string> _typeScriptTypeDef = [];
        private readonly List<string> _typeScriptFunctions = [];

        public void AddAppSrvMethod(string sourceCode) {
            _appSrvMethods.Add(sourceCode);
        }
        public void AddCSharpClass(string sourceCode) {
            _csharpClass.Add(sourceCode);
        }
        public void AddWebapiControllerAction(string sourceCode) {
            _webApiControllerAction.Add(sourceCode);
        }
        public void AddTypeScriptTypeDef(string sourceCode) {
            _typeScriptTypeDef.Add(sourceCode);
        }
        public void AddTypeScriptFunction(string sourceCode) {
            _typeScriptFunctions.Add(sourceCode);
        }

        public void ExecuteRendering(CodeRenderingContext ctx) {

            ctx.CoreLibrary(dir => {
                if (_csharpClass.Count > 0 || _appSrvMethods.Count > 0) {
                    dir.Generate(new SourceFile {
                        FileName = $"{_rootAggregate.PhysicalName.ToFileNameSafe()}.cs",
                        Contents = RenderCoreLibrary(ctx),
                    });
                }
            });
            ctx.WebapiProject(dir => {
                if (_webApiControllerAction.Count > 0) {
                    dir.Generate(new SourceFile {
                        FileName = $"{_rootAggregate.PhysicalName.ToFileNameSafe()}.cs",
                        Contents = RenderWebapi(ctx),
                    });
                }
            });
            ctx.ReactProject(dir => {
                if (_typeScriptTypeDef.Count > 0 || _typeScriptFunctions.Count > 0) {
                    dir.Generate(new SourceFile {
                        FileName = $"{_rootAggregate.PhysicalName.ToFileNameSafe()}.ts",
                        Contents = RenderNodeJs(ctx),
                    });
                }
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
                internal partial class {{controller.CsClassName}} : ControllerBase {
                    internal {{controller.CsClassName}}({{ApplicationService.ABSTRACT_CLASS}} applicationService, {{ApplicationConfigure.ABSTRACT_CLASS_WEBAPI}} webConfigure) {
                        _applicationService = applicationService;
                        _webConfigure = webConfigure;
                    }
                    /// <summary>アプリケーションサービス</summary>
                    private readonly {{ApplicationService.ABSTRACT_CLASS}} _applicationService;
                    /// <summary>WebApiプロジェクトの設定処理</summary>
                    private readonly {{ApplicationConfigure.ABSTRACT_CLASS_WEBAPI}} _webConfigure;
                {{_webApiControllerAction.SelectTextTemplate(source => $$"""

                    {{WithIndent(source, "    ")}}
                """)}}
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
                    RefSC = new Models.QueryModelModules.SearchCondition.Filter(@ref.RefTo),
                });

            return $$"""
                import React from "react"
                import * as ReactRouter from "react-router-dom"
                import { UUID } from "uuidjs"
                import * as Util from "../util2"
                import * as Constraints from "./util/constraints"
                {{refTos.SelectTextTemplate(r => $$"""
                import { {{r.RefTo.TsTypeName}}, {{r.RefTo.TsNewObjectFunction}}, {{r.RefSC.TsTypeName}} } from "{{r.FileName}}"
                """)}}

                //#region 型定義
                {{_typeScriptTypeDef.SelectTextTemplate(source => $$"""
                {{source}}

                """)}}
                //#endregion 型定義


                //#region 関数
                {{_typeScriptFunctions.SelectTextTemplate(source => $$"""
                {{source}}

                """)}}
                //#endregion 関数
                """;
        }
    }
}
