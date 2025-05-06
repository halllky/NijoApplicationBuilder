using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models;
using Nijo.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.CommandModelModules {
    /// <summary>
    /// <see cref="MessageContainer"/> の拡張。
    /// CommandModelの場合はメッセージコンテナの構造定義に使われる集約とクラス名に使われる集約が別
    /// </summary>
    internal class ParameterTypeMessageContainer : MessageContainer {
        internal ParameterTypeMessageContainer(AggregateBase aggregate) : base(aggregate) {
            if (aggregate is RootAggregate) {
                throw new InvalidOperationException($"{nameof(CommandModelExtensions.GetCommandModelParameterChild)}の結果を渡してください。");
            }
        }

        internal override string CsClassName => $"{_aggregate.PhysicalName}Messages";
        internal override string TsTypeName => $"{_aggregate.PhysicalName}Messages";

        protected override IEnumerable<IMessageContainerMember> GetMembers() {
            var parameter = new ParameterOrReturnValue.CommandDescendantMember(_aggregate);

            foreach (var member in parameter.GetMembers()) {
                if (member is ParameterOrReturnValue.CommandDescendantMember descendant) {
                    yield return new ContainerMemberImpl {
                        PhysicalName = member.PropertyName,
                        DisplayName = member.DisplayName,
                        NestedObject = new ParameterTypeMessageContainer(descendant.Aggregate),
                        CsType = null,
                    };
                } else if (member is ParameterOrReturnValue.CommandValueMember) {
                    yield return new ContainerMemberImpl {
                        PhysicalName = member.PropertyName,
                        DisplayName = member.DisplayName,
                        NestedObject = null,
                        CsType = null,
                    };
                } else if (member is ParameterOrReturnValue.CommandRefToMember refToMember) {
                    yield return new ContainerMemberImpl {
                        PhysicalName = member.PropertyName,
                        DisplayName = member.DisplayName,
                        NestedObject = refToMember.GetMessageContainer(),
                        CsType = null,
                    };
                } else {
                    throw new NotImplementedException();
                }
            }
        }

        private class ContainerMemberImpl : IMessageContainerMember {
            public required string PhysicalName { get; init; }
            public required string DisplayName { get; init; }
            public required MessageContainer? NestedObject { get; init; }
            public required string? CsType { get; init; }
        }

        internal static string RenderCSharpRecursively(RootAggregate rootAggregate) {
            var tree = rootAggregate
                .GetCommandModelParameterChild()
                .EnumerateThisAndDescendants()
                .Select(agg => new ParameterTypeMessageContainer(agg))
                .ToArray();

            return $$"""
                #region パラメータのデータ構造と対応するメッセージの入れ物クラス
                {{tree.SelectTextTemplate(container => $$"""
                {{container.RenderCSharp()}}
                """)}}
                #endregion パラメータのデータ構造と対応するメッセージの入れ物クラス
                """;
        }
    }
}
