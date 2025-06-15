using Nijo.Models.ReadModel2Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Word : StringMemberType {
        public override string GetUiDisplayName() => "単語";
        public override string GetHelpText() => $"単語。改行を含まず、値の前後に空白が入らない文字列属性。";

        protected override E_SearchBehavior GetSearchBehavior(AggregateMember.ValueMember vm) {
            return vm.Options.SearchBehavior ?? E_SearchBehavior.PartialMatch;
        }
    }
}
