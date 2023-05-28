using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core20230514 {
    public interface IGuiForm {
        IEnumerable<string> TextBox(
            bool multiline = false);

        IEnumerable<string> Toggle();

        /// <summary>
        /// コード自動生成時点で選択肢が確定している選択肢（列挙体など）
        /// </summary>
        IEnumerable<string> Selection();
    }
}
