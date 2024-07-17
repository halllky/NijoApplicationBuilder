using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// <see cref="DataClassForDisplay"/> を一括更新する処理。
    /// サーバー側で画面表示用データを <see cref="WriteModel2Features.DataClassForSave"/> に変換してForSaveの一括更新処理を呼ぶ。
    /// </summary>
    internal class BatchUpdateViewData {

        internal string ReactHookName => "useBatchUpdateViewData";

        internal string RenderReactHook(CodeRenderingContext context) {
            return $$"""
                TODO #35
                """;
        }

        internal SourceFile RenderController() => new SourceFile {
            FileName = "BatchUpdate.cs",
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
