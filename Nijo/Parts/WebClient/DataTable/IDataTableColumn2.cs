using Nijo.Core;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient.DataTable {
    /// <summary>
    /// Reactテンプレート側で宣言されているコンポーネント DataTable の列定義。
    /// <see cref="Models.ReadModel2"/> 用。
    /// </summary>
    internal interface IDataTableColumn2 {
        // 基本
        /// <summary>列ヘッダ文字</summary>
        string Header { get; }
        /// <summary>列グルーピング</summary>
        string? HeaderGroupName { get; }

        // 外観
        int? DefaultWidth { get; }
        bool EnableResizing { get; }
    }
}
