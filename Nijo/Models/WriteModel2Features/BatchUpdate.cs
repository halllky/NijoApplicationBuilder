using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 一括更新処理
    /// </summary>
    internal class BatchUpdate {
        internal string HookName => "useBatchUpdate";

        internal string RenderHook(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }

        internal SourceFile RenderController() => new SourceFile {
            FileName = "BatchUpdateController.cs",
            RenderContent = ctx => {
                return $$"""
                    TODO #35
                    """;
            },
        };

        internal string RenderAppSrvMethod(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }
    }
}
