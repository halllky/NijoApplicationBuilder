using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core.AggregateMemberTypes {
    internal class Sentence : StringMemberType {
        public override string GetUiDisplayName() => "文章";
        public override string GetHelpText() => $"文章。改行を含めることができる文字列。";

        protected override bool MultiLine => true;
    }
}
