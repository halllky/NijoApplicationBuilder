using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace haldoc.SqlQuery {
    public class QueryBuilderContext {
        public QueryBuilderContext() {
        }

        private readonly StringBuilder _sql = new();
        private readonly List<SqlParameter> _parameters = new();

        public string NewParameter(object value, SqlDbType? type = null) {
            var name = $"@p_{_parameters.Count}";
            var param = new SqlParameter(name, value);
            if (type != null) param.SqlDbType = type.Value;
            _parameters.Add(param);
            return name;
        }
        public void Append(string text) {
            _sql.Append(text);
        }
        public SqlCommand Build(SqlConnection conn) {
            var command = conn.CreateCommand();
            command.CommandText = _sql.ToString();
            command.Parameters.AddRange(_parameters.ToArray());
            return command;
        }
    }
}
