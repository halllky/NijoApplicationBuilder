using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    public interface IGuiFormRenderer {
        string TextBox(bool multiline = false);
        string Number();
        string DateTime(E_DateType dateType);

        string Toggle();

        /// <summary>
        /// コード自動生成時点で選択肢が確定している選択肢（列挙体など）
        /// </summary>
        string Selection(IEnumerable<KeyValuePair<string, string>> options);

        string HiddenField();

        public enum E_DateType {
            Year,
            YearMonth,
            Date,
            DateTime,
        }
    }
}
