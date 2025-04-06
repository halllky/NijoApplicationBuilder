using Nijo.Ver1.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.CSharp {
    /// <summary>
    /// 範囲検索の検索条件
    /// </summary>
    public class FromTo {
        public const string CS_CLASS_NAME = "FromTo";

        internal static SourceFile Render(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "FromTo.cs",
                Contents = $$"""
                    using System.Text.Json.Serialization;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// 範囲検索の検索条件
                    /// </summary>
                    public class {{CS_CLASS_NAME}}<T> {
                        [JsonPropertyName("from")]
                        public T? From { get; set; }
                        [JsonPropertyName("to")]
                        public T? To { get; set; }
                    }
                    """,
            };
        }
    }
}
