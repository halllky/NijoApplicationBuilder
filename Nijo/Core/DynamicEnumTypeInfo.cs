using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    /// <summary>
    /// 動的列挙体（区分マスタ）の種類に関する情報
    /// </summary>
    internal class DynamicEnumTypeInfo {
        /// <summary>
        /// 物理名
        /// </summary>
        public required string PhysicalName { get; init; }
        /// <summary>
        /// 画面表示名称
        /// </summary>
        public required string DisplayName { get; init; }
        /// <summary>
        /// 種類キー
        /// </summary>
        public required string TypeKey { get; init; }
    }
}
