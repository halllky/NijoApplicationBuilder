using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 一括更新処理のコンテキスト引数。
    /// エラーメッセージや確認メッセージなどを書きやすくするためのもの。
    /// </summary>
    internal class BatchUpdateContext {
        internal SourceFile Render() => new SourceFile {
            FileName = "BatchUpdateContext.cs",
            RenderContent = ctx => {
                return $$"""
                    TODO #35
                    """;
            },
        };
    }
}
