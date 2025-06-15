using Nijo.Models.ReadModel2Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Uuid : StringMemberType {
        public override string GetUiDisplayName() => "UUID";
        public override string GetHelpText() {
            return $"UUID。" +
                $"新規作成画面の表示時や、一括編集画面の行追加時など、" +
                $"クライアント側でのオブジェクト作成のタイミングで発番されます。";
        }

        protected override E_SearchBehavior GetSearchBehavior(AggregateMember.ValueMember vm) => E_SearchBehavior.Strict;
    }
}
