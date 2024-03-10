using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    public interface IAggregateMemberType {
        SearchBehavior SearchBehavior { get; }

        string GetCSharpTypeName();
        string GetTypeScriptTypeName();
        ReactInputComponent GetReactComponent(GetReactComponentArgs e);
    }
    /// <summary>
    /// 検索処理の挙動
    /// </summary>
    public enum SearchBehavior {
        /// <summary>
        /// 発行されるSQL文: WHERE DBの値 = 検索条件
        /// </summary>
        Strict,
        /// <summary>
        /// 発行されるSQL文: WHERE DBの値 LIKE '%検索条件%'
        /// </summary>
        Ambiguous,
        /// <summary>
        /// 発行されるSQL文: WHERE DBの値 >= 検索条件.FROM
        ///                AND   DBの値 <= 検索条件.TO
        /// </summary>
        Range,
    }
    public sealed class ReactInputComponent {
        public required string Name { get; init; }
        public Dictionary<string, string> Props { get; init; } = [];
        public Func<string, string, string>? GridCellFormatStatement { get; init; }

        /// <summary>
        /// <see cref="Props"/> をReactのコンポーネントのレンダリングの呼び出し時用の記述にして返す
        /// </summary>
        internal IEnumerable<string> GetPropsStatement() {
            foreach (var p in Props) {
                if (p.Value == string.Empty)
                    yield return $" {p.Key}";
                else if (p.Value.StartsWith("\"") && p.Value.EndsWith("\""))
                    yield return $" {p.Key}={p.Value}";
                else
                    yield return $" {p.Key}={{{p.Value}}}";
            }
        }
    }

    public sealed class GetReactComponentArgs {
        public required E_Type Type { get; init; }

        public enum E_Type {
            InDetailView,
            InDataGrid,
        }
    }
}
