using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// デバッグ用ダミーデータ作成関数
    /// </summary>
    internal class DummyDataGenerator {
        internal SourceFile Render() => new SourceFile {
            FileName = "generateDummyData.ts",
            RenderContent = ctx => {
                return $$"""
                    TODO #35
                    """;
            },
        };
    }
}
