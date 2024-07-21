using Nijo.Features.Storing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Numeric : SchalarMemberType {
        public override string GetCSharpTypeName() => "decimal";
        public override string GetTypeScriptTypeName() => "number";

        public override ReactInputComponent GetReactComponent() {
            return new ReactInputComponent { Name = "Input.Num" };
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
                        const { num: {{parsed}} } = Util.tryParseAsNumberOrEmpty({{value}})
                        """;
                },
            };
        }
    }
}
