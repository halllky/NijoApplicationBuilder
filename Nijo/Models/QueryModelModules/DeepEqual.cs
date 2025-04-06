using Nijo.ImmutableSchema;
using Nijo.Models.QueryModelModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.QueryModelModules {
    /// <summary>
    /// 画面表示用データのディープイコール関数
    /// </summary>
    internal class DeepEqual {

        internal DeepEqual(RootAggregate rootAggregate) {
            _rootAggregate = rootAggregate;
        }
        private readonly RootAggregate _rootAggregate;

        internal string TsFunctionName => $"deepEquals{_rootAggregate.PhysicalName}";

        internal string RenderTypeScript() {
            var displayData = new DisplayData(_rootAggregate);

            return $$"""
                /** {{_rootAggregate.DisplayName}}の画面表示用データをディープイコールで比較し、一致していればtrueを返します。 */
                export const {{TsFunctionName}} = (left: {{displayData.TsTypeName}}, right: {{displayData.TsTypeName}}): boolean => {
                  throw new Error() // TODO ver.1
                }
                """;
        }
    }
}
