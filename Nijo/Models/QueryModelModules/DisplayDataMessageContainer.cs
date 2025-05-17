using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models.DataModelModules;
using Nijo.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.QueryModelModules;

/// <summary>
/// <see cref="DisplayData"/> の形と一致するメッセージの入れ物
/// </summary>
internal class DisplayDataMessageContainer : MessageContainer {
    public DisplayDataMessageContainer(AggregateBase aggregate) : base(aggregate) {
    }

    internal override string CsClassName => $"{_aggregate.PhysicalName}DisplayDataMessages";
    internal override string TsTypeName => $"{_aggregate.PhysicalName}DisplayDataMessages";

    protected override IEnumerable<string> GetCsClassImplements() {
        // この集約がデータモデルの場合、登録更新削除処理で使われるメッセージの入れ物のインタフェースを実装する
        if (_aggregate.GetRoot().Model is DataModel) {
            var saveCommandMessage = new SaveCommandMessageContainer(_aggregate);
            yield return saveCommandMessage.InterfaceName;
        }
    }

    protected override IEnumerable<IMessageContainerMember> GetMembers() {
        var displayData = new DisplayData(_aggregate);
        foreach (var member in displayData.Values.GetMembers()) {
            yield return new ContainerMemberImpl {
                PhysicalName = member.GetPropertyName(E_CsTs.CSharp),
                DisplayName = member.DisplayName,
                NestedObject = null,
                CsType = null,
            };
        }
        foreach (var member in displayData.GetChildMembers()) {
            yield return new ContainerMemberImpl {
                PhysicalName = member.PhysicalName,
                DisplayName = member.DisplayName,
                NestedObject = new DisplayDataMessageContainer(member.Aggregate),
                CsType = null,
            };
        }
    }

    /// <summary>
    /// この集約がデータモデルの場合、Child, Children のプロパティは明示的にSaveCommandのメンバーにキャストする必要がある
    /// </summary>
    protected override string RenderCSharpAdditionalSource() {
        // この集約がデータモデルでないのであれば関係なし
        if (_aggregate.GetRoot().Model is not DataModel) return SKIP_MARKER;

        var saveCommandMessage = new SaveCommandMessageContainer(_aggregate);
        var childMembers = GetMembers()
            .Where(member => member.NestedObject != null)
            .Select(member => {
                var memberAggregate = ((ContainerMemberImpl)member).NestedObject!._aggregate;
                var childMsgContainer = new SaveCommandMessageContainer(memberAggregate);

                return new {
                    member.PhysicalName,
                    InterfaceName = memberAggregate is ChildrenAggregate
                        ? $"{INTERFACE_LIST}<{childMsgContainer.InterfaceName}>"
                        : childMsgContainer.InterfaceName,
                };
            });

        return $$"""

            {{childMembers.SelectTextTemplate(member => $$"""
            {{member.InterfaceName}} {{saveCommandMessage.InterfaceName}}.{{member.PhysicalName}} => this.{{member.PhysicalName}};
            """)}}
            """;
    }

    private class ContainerMemberImpl : IMessageContainerMember {
        public required string PhysicalName { get; init; }
        public required string DisplayName { get; init; }
        public required DisplayDataMessageContainer? NestedObject { get; init; }
        public required string? CsType { get; init; }

        MessageContainer? IMessageContainerMember.NestedObject => NestedObject;
    }

    internal static string RenderCSharpRecursively(RootAggregate rootAggregate) {
        var tree = rootAggregate
            .EnumerateThisAndDescendants()
            .Select(agg => new DisplayDataMessageContainer(agg))
            .ToArray();

        return $$"""
                #region 画面表示用クラスのデータ構造と対応するメッセージの入れ物クラス
                {{tree.SelectTextTemplate(container => $$"""
                {{container.RenderCSharp()}}
                """)}}
                #endregion 画面表示用クラスのデータ構造と対応するメッセージの入れ物クラス
                """;
    }
}
