using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using Nijo.Ver1.Parts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.DataModelModules {
    internal class SaveCommandMessageContainer : MessageContainer {
        public SaveCommandMessageContainer(AggregateBase aggregate) : base(aggregate) {
        }

        /// <summary>
        /// DataModelの場合、ユーザーに対してDataModelの型ではなくQuery/CommandModelの型で通知する必要があるケースがあるため
        /// DataModel型のインターフェースを実装したQueryModelのメッセージコンテナを使用することがある。
        /// </summary>
        internal string InterfaceName => $"I{_aggregate.PhysicalName}Messages";

        protected override IEnumerable<string> GetCsClassImplements() {
            yield return InterfaceName;
        }

        protected override IEnumerable<IMessageContainerMember> GetMembers() {
            // SaveCommandと同じデータ型になるのでSaveCommandの処理を流用する。
            // Updateを使っているのは、Create, Update, Delete のうちUpdateが最も多くの項目を持っているため
            return new SaveCommand(_aggregate)
                .GetUpdateCommandMembers()
                .Select(m => new MessageContainerMemberImpl {
                    PhysicalName = m.PhysicalName,
                    DisplayName = m.DisplayName,
                    ArrayGenericType = m.Member is ChildrenAggreagte children
                        ? new SaveCommandMessageContainer(children).InterfaceName
                        : null,
                });
        }

        internal override string RenderCSharp() {
            // 基底クラス側でレンダリングされるソースに加えてインターフェースもレンダリングする
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} のデータ構造と対応したメッセージの入れ物
                /// </summary>
                public interface {{InterfaceName}} : {{INTERFACE}} {
                {{GetMembers().SelectTextTemplate(m => m.ArrayGenericType == null ? $$"""
                    /// <summary>{{m.DisplayName}}に対して発生したメッセージの入れ物</summary>
                    {{INTERFACE}} {{m.PhysicalName}} { get; }
                """ : $$"""
                    /// <summary>{{m.DisplayName}}に対して発生したメッセージの入れ物</summary>
                    {{INTERFACE_LIST}}<{{m.ArrayGenericType}}> {{m.PhysicalName}} { get; }
                """)}}
                }

                {{base.RenderCSharp()}}
                """;
        }

        private class MessageContainerMemberImpl : IMessageContainerMember {
            public required string PhysicalName { get; set; }
            public required string DisplayName { get; set; }
            public required string? ArrayGenericType { get; set; }
        }
    }
}

namespace Nijo.Ver1.CodeGenerating {
    using Nijo.Ver1.Models.DataModelModules;

    partial class SchemaPathNodeExtensions {

        /// <summary>
        /// <see cref="GetFullPath(ISchemaPathNode)"/> の結果を <see cref="SaveCommandMessageContainer"/> のルールに沿ったパスとして返す
        /// </summary>
        public static IEnumerable<string> AsSaveCommandMessage(this IEnumerable<ISchemaPathNode> path) {
            var entry = path.FirstOrDefault()?.GetEntry();
            var isOutOfEntryTree = false;

            foreach (var node in path) {
                if (node == entry) continue; // パスの一番最初（エントリー）はスキップ
                if (node.PreviousNode is RefToMember) continue; // refの1つ次の要素の名前はrefで列挙済みのためスキップ

                // 外部参照のナビゲーションプロパティを辿るパス
                if (node is RefToMember refTo) {
                    var previous = (AggregateBase?)node.PreviousNode ?? throw new InvalidOperationException("reftoの前は必ず参照元集約か参照先集約になるのでありえない");

                    // 参照元から参照先へ辿るパス
                    if (previous == refTo.Owner) {
                        if (!isOutOfEntryTree) {
                            // エントリーの集約内部から外に出る瞬間の場合
                            var member = new SaveCommand.SaveCommandRefMember(refTo);
                            yield return member.PhysicalName;

                            isOutOfEntryTree = true;

                            // ref-toに対するメッセージはref-toのIDや名称ではなくref-to自身に付すため、ここで終わり
                            break;

                        } else {
                            // 参照先のキーの中でさらに他の集約への参照が発生した場合
                            var key = new KeyClass.KeyClassRefMember(refTo);
                            yield return key.PhysicalName;
                            continue;
                        }
                    }
                    // 参照先から参照元へ辿るパス
                    if (previous == refTo.RefTo) {
                        throw new InvalidOperationException("更新処理引数クラスでは参照先から参照元へ辿ることはできない");
                    }
                    throw new InvalidOperationException("reftoの前は必ず参照元集約か参照先集約になるのでありえない");
                }

                // 親子間のナビゲーションプロパティを辿るパス
                if (node is AggregateBase curr && node.PreviousNode is AggregateBase prev) {

                    // 子から親へ辿るパス
                    if (curr.IsParentOf(prev)) {

                        // エントリーの集約内部では子から親へ辿るパターンは無い
                        if (!isOutOfEntryTree) throw new InvalidOperationException("エントリーの集約内部では子から親へ辿るパターンは無い");

                        var parentMember = new KeyClass.KeyClassParentMember(curr);
                        yield return parentMember.PhysicalName;
                        continue;
                    }
                    // 親から子へ辿るパス
                    if (curr.IsChildOf(prev)) {

                        // 参照先のキーの中では親から子へ辿るパターンは無い
                        if (isOutOfEntryTree) throw new InvalidOperationException("参照先のキーの中では親から子へ辿るパターンは無い");

                        var childMember = curr switch {
                            ChildAggreagte child => new SaveCommand.SaveCommandDescendantMember(child),
                            ChildrenAggreagte children => new SaveCommand.SaveCommandDescendantMember(children),
                            _ => throw new InvalidOperationException("ありえない"),
                        };
                        yield return childMember.PhysicalName;
                        continue;
                    }
                    throw new InvalidOperationException("必ず 親→子, 子→親 のどちらかになるのでありえない");
                }

                // 末端のメンバー
                if (node is not ValueMember vm) throw new InvalidOperationException("この分岐まで来るケースは値の場合しか無いのでありえない");

                var valueMember = new SaveCommand.SaveCommandValueMember(vm);
                yield return valueMember.PhysicalName;
            }
        }

    }
}
