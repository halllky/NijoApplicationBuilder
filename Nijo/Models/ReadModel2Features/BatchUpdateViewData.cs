using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// <see cref="DataClassForView"/> を一括更新する処理。
    /// サーバー側で画面表示用データを <see cref="WriteModel2Features.DataClassForSave"/> に変換してForSaveの一括更新処理を呼ぶ。
    /// </summary>
    internal class BatchUpdateViewData {

        internal string ReactHookName => throw new NotImplementedException("TODO");

        internal string RenderReactHook(CodeRenderingContext context) {
            throw new NotImplementedException("TODO");
        }

        internal SourceFile RenderController() {
            throw new NotImplementedException("TODO");
        }

        internal string RenderAppSrvMethod(CodeRenderingContext context) {
            throw new NotImplementedException("TODO");
        }
    }
}
