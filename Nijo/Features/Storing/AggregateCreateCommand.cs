using Nijo.Core;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;

namespace Nijo.Features.Storing {
    internal class AggregateCreateCommand : DataClassForSave {
        internal AggregateCreateCommand(GraphNode<Aggregate> aggregate) : base(aggregate) {
            if (!aggregate.IsRoot()) throw new ArgumentException();
        }

        internal override string CsClassName => $"{_aggregate.Item.DisplayName.ToCSharpSafe()}CreateCommand";
        internal bool RendersDbEntity { get; set; } = true;

        internal override string RenderCSharp(CodeRenderingContext ctx) {
            return $$"""
                /// <summary>
                /// {{_aggregate.Item.DisplayName}}のデータ作成コマンドです。
                /// </summary>
                public partial class {{CsClassName}} {
                {{GetOwnMembers().SelectTextTemplate(prop => $$"""
                    public {{prop.CSharpTypeName}}? {{prop.MemberName}} { get; set; }
                """)}}

                {{If(RendersDbEntity, () => $$"""
                    {{WithIndent(ToDbEntity(ctx), "    ")}}
                """)}}
                }
                """;
        }
    }
}
