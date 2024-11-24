using Nijo.Parts.BothOfClientAndServer;
using Nijo.Parts.WebClient.DataTable;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Core.StringMemberType;

namespace Nijo.Core.AggregateMemberTypes {
    /// <summary>
    /// 文字列型のコード。
    /// </summary>
    internal class CodeOfString : StringMemberType {

        public override string GetUiDisplayName() => "コード（文字列）";
        public override string GetHelpText() => $$"""
            文字列型のコード。
            通常の単語型と異なり、検索時の挙動を部分一致以外にすることができ、英数字のみの指定ができる。
            """;

        protected override E_SearchBehavior GetSearchBehavior(AggregateMember.ValueMember vm) {
            return vm.Options.SearchBehavior ?? E_SearchBehavior.PartialMatch;
        }
    }
}
