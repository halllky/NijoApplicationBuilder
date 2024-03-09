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
        string GetGridCellValueFormatter();
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

    /// <summary>
    /// 現実のものや出来事を分類する値。ID、名前、列挙体など。
    /// </summary>
    public abstract class CategorizeType : IAggregateMemberType {
        public abstract SearchBehavior SearchBehavior { get; }
        public abstract string GetCSharpTypeName();
        public abstract string GetTypeScriptTypeName();
        public virtual string GetGridCellValueFormatter() => string.Empty;
        public abstract ReactInputComponent GetReactComponent(GetReactComponentArgs e);
    }


    /// <summary>
    /// 連続した量をもつ値。数値、日付時刻など。
    /// </summary>
    public abstract class SchalarType : IAggregateMemberType {
        public SearchBehavior SearchBehavior => SearchBehavior.Range;
        public abstract string GetCSharpTypeName();
        public abstract string GetTypeScriptTypeName();
        public virtual string GetGridCellValueFormatter() => string.Empty;
        public abstract ReactInputComponent GetReactComponent(GetReactComponentArgs e);
        //object? Min { get; }
        //object? Max { get; }
    }
    /// <inheritdoc/>
    public abstract class SchalarType<T> : SchalarType {
        //new T? Min { get; }
        //new T? Max { get; }
    }
}
