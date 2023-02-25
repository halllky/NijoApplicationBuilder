internal class SqliteParamGenerator : HalApplicationBuilder.Core.DBModel.SelectStatement.IParamGenerator {
    public System.Data.Common.DbParameter CreateParameter(string paramName, object value) {
        return new Microsoft.Data.Sqlite.SqliteParameter(paramName, value);
    }
}
