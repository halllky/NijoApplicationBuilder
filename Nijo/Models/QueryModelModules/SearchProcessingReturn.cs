using Nijo.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.QueryModelModules {
    /// <summary>
    /// 一覧検索処理の戻り値の型
    /// </summary>
    internal class SearchProcessingReturn {
        internal const string TYPE_CS = "LoadReturnType";
        internal const string TYPE_TS = "LoadReturnType";
        internal const string CURRENT_PAGE_ITEMS_CS = "CurrentPageItems";
        internal const string CURRENT_PAGE_ITEMS_TS = "currentPageItems";
        internal const string TOTAL_COUNT_CS = "TotalCount";
        internal const string TOTAL_COUNT_TS = "totalCount";

        /// <summary>
        /// Load処理の戻り値の型の定義をレンダリングします。（C#）
        /// </summary>
        internal static SourceFile RenderCSharp(CodeRenderingContext ctx) {
            return new SourceFile {
                FileName = "LoadReturnType.cs",
                Contents = $$"""
                    using System.Text.Json.Serialization;

                    namespace {{ctx.Config.RootNamespace}};

                    /// <summary>
                    /// 一覧検索処理の戻り値の型
                    /// </summary>
                    public class {{TYPE_CS}}<TDisplayData> {
                        /// <summary>
                        /// 検索結果のうち現在のページに表示されるもの
                        /// </summary>
                        [JsonPropertyName("{{CURRENT_PAGE_ITEMS_TS}}")]
                        public required IReadOnlyList<TDisplayData> {{CURRENT_PAGE_ITEMS_CS}} { get; init; }
                        /// <summary>
                        /// 検索結果のトータル件数（検索結果のうち現在のページ以外も含む）
                        /// </summary>
                        [JsonPropertyName("{{TOTAL_COUNT_TS}}")]
                        public required int {{TOTAL_COUNT_CS}} { get; init; }
                    }
                    """,
            };
        }
        /// <summary>
        /// Load処理の戻り値の型の定義をレンダリングします。（TypeScript）
        /// </summary>
        internal static SourceFile RenderTypeScript() {
            return new SourceFile {
                FileName = "load-return-type.ts",
                Contents = $$"""
                    /** 一覧検索処理の戻り値の型 */
                    export type {{TYPE_TS}}<TDisplayData> = {
                      /** 検索結果のうち現在のページに表示されるもの */
                      {{CURRENT_PAGE_ITEMS_TS}}: TDisplayData[]
                      /** 検索結果のトータル件数（検索結果のうち現在のページ以外も含む） */
                      {{TOTAL_COUNT_TS}}: number
                    }
                    """,
            };
        }
    }
}
