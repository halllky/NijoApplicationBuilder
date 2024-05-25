using Nijo.Core;
using Nijo.Core.AggregateMemberTypes;
using Nijo.Util.DotnetEx;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    internal class TSInitializerFunction {
        internal TSInitializerFunction(GraphNode<Aggregate> instance) {
            _instance = instance;
        }
        private readonly GraphNode<Aggregate> _instance;

        internal string FunctionName => $"create{_instance.Item.PhysicalName}";

        /// <summary>
        /// <see cref="DataClassForDisplay.RenderTsInitializerFunction"/> のロジックと合わせる
        /// </summary>
        internal string Render() {
            var children = _instance
                .GetMembers()
                .OfType<AggregateMember.Children>()
                .Select(member => new {
                    Key = member.MemberName,
                    Value = $"[]",
                });
            var child = _instance
                .GetMembers()
                .OfType<AggregateMember.Child>()
                .Select(member => new {
                    Key = member.MemberName,
                    Value = $"{new TSInitializerFunction(member.ChildAggregate).FunctionName}()",
                });
            var variation = _instance
                .GetMembers()
                .OfType<AggregateMember.VariationItem>()
                .Select(member => new {
                    Key = member.MemberName,
                    Value = $"{new TSInitializerFunction(member.VariationAggregate).FunctionName}()",
                });
            var variationSwitch = _instance
                .GetMembers()
                .OfType<AggregateMember.Variation>()
                .Select(member => new {
                    Key = member.MemberName,
                    Value = $"'{member.GetGroupItems().First().Key}'",
                });
            var uuid = new DataClassForUpdate(_instance)
                .GetOwnMembers()
                .OfType<AggregateMember.ValueMember>()
                .Where(member => member.Options.MemberType is Uuid)
                .Select(member => new {
                    Key = member.MemberName,
                    Value = "UUID.generate()",
                });

            var initializers = uuid
                .Concat(children)
                .Concat(child)
                .Concat(variation)
                .Concat(variationSwitch)
                .ToList();

            return $$"""
                    export const {{FunctionName}} = (): {{new DataClassForUpdate(_instance).TsTypeName}} => ({
                    {{initializers.SelectTextTemplate(item => $$"""
                      {{item.Key}}: {{item.Value}},
                    """)}}
                    })
                    """;
        }
    }
}
