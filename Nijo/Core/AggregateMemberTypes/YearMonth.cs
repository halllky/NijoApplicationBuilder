using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class YearMonth : SchalarMemberType {

        public override void GenerateCode(CodeRenderingContext context) {
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(Parts.Utility.RuntimeYearMonthClass.RenderDeclaring());
            });
            var util = context.UseSummarizedFile<Parts.Utility.UtilityClass>();
            util.AddJsonConverter(Parts.Utility.RuntimeYearMonthClass.GetCustomJsonConverter());
        }

        public override string GetCSharpTypeName() => Parts.Utility.RuntimeYearMonthClass.CLASS_NAME;
        public override string GetTypeScriptTypeName() => "number";

        protected override string ComponentName => "Input.YearMonth";
        protected override IEnumerable<string> RenderAttributes() {
            yield return $"className=\"w-20\"";
        }

        public override IGridColumnSetting GetGridColumnEditSetting() {
            return new TextColumnSetting {
                GetValueFromRow = (value, formatted) => {
                    return $$"""
                        const {{formatted}} = {{value}}?.toString()
                        """;
                },
                SetValueToRow = (value, parsed) => {
                    return $$"""
                        const { yyyymm: {{parsed}} } = Util.tryParseAsYearMonthOrEmpty({{value}})
                        """;
                },
            };
        }
    }
}
