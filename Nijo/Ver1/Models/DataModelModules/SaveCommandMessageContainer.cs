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

        internal override string RenderCSharp() {
            // 基底クラス側でレンダリングされるソースに加えてインターフェースもレンダリングする
            return $$"""
                /// <summary>
                /// {{_aggregate.DisplayName}} のデータ構造と対応したメッセージの入れ物
                /// </summary>
                public interface {{InterfaceName}} {
                    // TODO ver.1
                }

                {{base.RenderCSharp()}}
                """;
        }
    }
}
