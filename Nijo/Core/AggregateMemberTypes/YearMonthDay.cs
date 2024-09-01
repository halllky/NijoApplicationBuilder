using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonthDay : SchalarMemberType {

        public override void GenerateCode(CodeRenderingContext context) {
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(Parts.Utility.RuntimeDateClass.RenderDeclaring());
            });
            var util = context.UseSummarizedFile<Parts.Utility.UtilityClass>();
            util.AddJsonConverter(Parts.Utility.RuntimeDateClass.GetCustomJsonConverter());
        }

        public override string GetCSharpTypeName() => Parts.Utility.RuntimeDateClass.CLASS_NAME;
        public override string GetTypeScriptTypeName() => "string";

        protected override string ComponentName => "Input.Date";
        protected override IEnumerable<string> RenderAttributes() {
            yield return $"className=\"w-28\"";
        }

        public override IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
                SetValueToRow = (value, parsed) => {
                    return $$"""
                        const { result: {{parsed}} } = Util.tryParseAsDateOrEmpty({{value}})
                        """;
                },
            };
        }
    }
}
