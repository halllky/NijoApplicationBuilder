using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;

namespace HalApplicationBuilder.EntityFramework {

    /// <summary>
    /// SELECT文
    /// </summary>
    public class SelectStatement {

        private readonly List<DbEntity> _tables = new();
        private readonly List<System.Data.Common.DbParameter> _params = new();

        private readonly List<string> _selectClauses = new();
        private readonly List<string> _whereClauses = new();
        private readonly List<(DbColumn column, bool asc)> _orderByClauses = new();

        internal string GetAlias(DbEntity dbEntity) {
            if (dbEntity == null) throw new ArgumentNullException(nameof(dbEntity));
            if (_tables.Contains(dbEntity) == false) throw new InvalidOperationException($"{dbEntity} がFROM句にもJOINにも存在しない");

            return $"t{_tables.IndexOf(dbEntity)}";
        }

        public SelectStatement Select(Func<Arg, string> column) {
            if (column == null) throw new ArgumentNullException(nameof(column));

            var arg = new Arg(this);
            var selectClause = column(arg);
            _selectClauses.Add(selectClause);
            return this;
        }
        public SelectStatement From(DbEntity dbEntity) {
            if (dbEntity == null) throw new ArgumentNullException(nameof(dbEntity));
            if (_tables.Any()) throw new InvalidOperationException($"FROM句には既に {_tables[0]} が設定されているので {dbEntity} を設定できません。");

            _tables.Add(dbEntity);
            return this;
        }
        public SelectStatement LeftJoin(DbEntity dbEntity) {
            if (dbEntity == null) throw new ArgumentNullException(nameof(dbEntity));
            if (_tables.Any() == false) throw new InvalidOperationException($"FROM句未設定");
            if (_tables[0] == dbEntity) throw new InvalidOperationException($"{dbEntity} はFROM句に設定されているのでJOIN不可");
            if (_tables.Contains(dbEntity)) throw new InvalidOperationException($"{dbEntity} を複数回JOINするのは非対応");
            if (_tables[0].GetDescendants().Contains(dbEntity) == false) throw new InvalidOperationException($"{_tables[0]} の子テーブル以外のJOINは非対応");

            _tables.Add(dbEntity);
            return this;
        }

        public SelectStatement Where(Func<Arg, string> predicate) {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            var arg = new Arg(this);
            var whereClause = predicate(arg);
            _whereClauses.Add(whereClause);
            return this;
        }

        public SelectStatement OrderBy(DbColumn dbColumn) {
            _orderByClauses.Add((column: dbColumn, asc: true));
            return this;
        }
        public SelectStatement OrderByDescending(DbColumn dbColumn) {
            _orderByClauses.Add((column: dbColumn, asc: false));
            return this;
        }


        public string ToSqlString() {
            if (_selectClauses.Any() == false) throw new InvalidOperationException($"SELECT句未設定");
            if (_tables.Any() == false) throw new InvalidOperationException($"FROM句未設定");

            var builder = new StringBuilder();

            // SELECT
            builder.AppendLine($"SELECT {string.Join(", ", _selectClauses)}");

            // FROM
            var mainTable = _tables[0];
            var mainTableAlias = GetAlias(_tables[0]);
            builder.AppendLine($"FROM {mainTable.ClassName} AS {mainTableAlias}");

            // LEFT JOIN
            foreach (var table in _tables.Skip(1)) {
                var joinedTable = GetAlias(table);
                builder.AppendLine($"LEFT JOIN {table.ClassName} AS {joinedTable}");

                // Q.子テーブルの主キーを見ていないのはなぜ？ => A.このクラスは親子テーブルのJOINにしか対応していないため
                // Q.親子間でカラム名が異なる場合にエラーにならないか？ => A.別のカラム名をつける機能は非対応のため未考慮
                var onClause = mainTable.PKColumns.Select(column => $"({joinedTable}.{column.PropertyName} = {mainTableAlias}.{column.PropertyName})");
                builder.AppendLine($"ON ({string.Join(" AND ", onClause)})");
            }

            // WHERE
            if (_whereClauses.Any()) {
                var where = _whereClauses.Select(w => $"({w})");
                builder.AppendLine($"WHERE {string.Join(" AND ", where)}");
            }

            // ORDER BY
            if (_orderByClauses.Any()) {
                var order = _orderByClauses.Select(o => $"{GetAlias(o.column.Owner)}.{o.column.PropertyName} {(o.asc ? "ASC" : "DESC")}");
                builder.AppendLine($"ORDER BY {string.Join(", ", order)}");
            }

            return builder.ToString();
        }

        public System.Data.Common.DbCommand ToSqlCommand(System.Data.Common.DbConnection sqlConnection) {
            var command = sqlConnection.CreateCommand();
            command.CommandText = ToSqlString();
            command.Parameters.AddRange(_params.ToArray());
            return command;
        }


        /// <summary>
        /// Microsoft.Data.SqlClient.SqlParameter を Microsoft.Data.Sqlite.SqliteParameter に変換できないので
        /// DIでパラメータ生成処理を渡すようにするためのインターフェース
        /// </summary>
        public interface IParamGenerator {
            System.Data.Common.DbParameter CreateParameter(string paramName, object value);
        }
        public SelectStatement(IParamGenerator paramGenerator) {
            _paramGenerator = paramGenerator;
        }
        private readonly IParamGenerator _paramGenerator;

        public class Arg {
            internal Arg(SelectStatement statement) => _statement = statement;
            private readonly SelectStatement _statement;
            public string GetAlias(DbEntity dbEntity) {
                return _statement.GetAlias(dbEntity);
            }
            public string NewParam(object value) {
                var name = $"@p_{_statement._params.Count}";
                _statement._params.Add(_statement._paramGenerator.CreateParameter(name, value));
                return name;
            }
        }
    }
}
