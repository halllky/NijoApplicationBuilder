using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.EntityFramework;

namespace HalApplicationBuilder.Runtime {
    /// <summary>
    /// 複合キーをHTTPでやりとりするためにJSON化して取り扱う仕組み
    /// </summary>
    public class InstanceKey : ValueObject {

        public static InstanceKey Create(object dbInstance, DbEntity dbEntity) {
            var type = dbInstance.GetType();
            var dict = dbEntity.PKColumns
                .Select(col => {
                    var prop = type.GetProperty(col.PropertyName);
                    var value = prop.GetValue(dbInstance);
                    return new { col, value };
                })
                .ToDictionary(x => x.col, x => x.value);

            return new InstanceKey(dbEntity, dict);
        }

        public static bool TryParse(string key, DbEntity dbEntity, out InstanceKey instanceKey) {
            if (string.IsNullOrWhiteSpace(key)) { instanceKey = null; return false; }

            var jsonValues = JsonSerializer.Deserialize<JsonElement[]>(key);

            if (dbEntity.PKColumns.Count != jsonValues.Length) { instanceKey = null; return false; }

            var dict = new Dictionary<DbColumn, object>();
            for (int i = 0; i < dbEntity.PKColumns.Count; i++) {
                var value = dbEntity.PKColumns[i].CSharpTypeName switch {
                    // この3つの型に対応していればほぼ大丈夫だろう
                    "string" => (object)jsonValues[i].GetString(),
                    "int" => jsonValues[i].GetInt32(),
                    "int?" => jsonValues[i].TryGetInt32(out var x) ? x : (int?)null,
                    _ => throw new InvalidOperationException($"{dbEntity.PKColumns[i].PropertyName} の主キーの型が非対応: {dbEntity.PKColumns[i].CSharpTypeName}"),
                };
                dict.Add(dbEntity.PKColumns[i], value);
            }
            instanceKey = new InstanceKey(dbEntity, dict);
            return true;
        }

        private InstanceKey(DbEntity dbEntity, IReadOnlyDictionary<DbColumn, object> dict) {
            DbEntity = dbEntity;
            ValuesDictionary = dict;
            StringValue = JsonSerializer.Serialize(ValuesDictionary.Select(v => v.Value));
        }
        public DbEntity DbEntity { get; }
        public string StringValue { get; }
        public IReadOnlyDictionary<DbColumn, object> ValuesDictionary { get; }
        public object[] Values => ValuesDictionary.Values.ToArray();

        protected override IEnumerable<object> ValueObjectIdentifiers() {
            yield return DbEntity;
            yield return StringValue;
        }
        public override string ToString() {
            return $"[{DbEntity}] {StringValue}";
        }
    }
}
