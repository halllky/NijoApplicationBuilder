using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// カスタマイズで使うためのReadModelの名前の一覧
    /// </summary>
    internal class DisplayDataTypeList : ISummarizedFile {

        internal void Add(DataClassForDisplay displayData) {
            _displayDataList.Add(displayData);
        }
        private readonly List<DataClassForDisplay> _displayDataList = new();

        public void OnEndGenerating(CodeRenderingContext context) {
            context.ReactProject.Types.Add($$"""
                /** 自動生成されたReadModelの種類の一覧 */
                export type ReadModelType
                {{If(_displayDataList.Count == 0, () => $$"""
                  = never
                """)}}
                {{_displayDataList.SelectTextTemplate((disp, i) => $$"""
                  {{(i == 0 ? "=" : "|")}} '{{disp.TsTypeName}}'
                """)}}
                """);
        }
    }
}
