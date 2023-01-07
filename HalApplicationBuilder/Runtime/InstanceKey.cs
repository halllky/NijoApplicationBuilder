using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HalApplicationBuilder.EntityFramework;

namespace HalApplicationBuilder.Runtime {
    /// <summary>
    /// 複合キーをHTTPでやりとりするためにJSON化して取り扱う仕組み
    /// </summary>
    public class InstanceKey {

        public static InstanceKey Create(object instance, DbEntity dbEntity) {
            if (dbEntity.Parent != null)
                throw new ArgumentException($"{nameof(InstanceKey)}は集約ルートのオブジェクトに対してのみ");
            if (instance.GetType().FullName != dbEntity.RuntimeFullName)
                throw new ArgumentException($"{instance.GetType().Name} は {dbEntity.ClassName} のインスタンスでない");

            var type = instance.GetType();
            var dict = dbEntity.PKColumns
                .Select(col => {
                    var prop = type.GetProperty(col.PropertyName);
                    var value = prop.GetValue(instance);
                    return new { col, value };
                })
                .ToDictionary(x => x.col, x => x.value);

            return new InstanceKey {
                DbEntity = dbEntity,
                ParsedValue = dict,
            };
        }

        public static bool TryParse(string key, DbEntity dbEntity, out InstanceKey instanceKey) {
            var jsonValues = JsonSerializer.Deserialize<JsonElement[]>(key);

            if (dbEntity.PKColumns.Count != jsonValues.Length) { instanceKey = null; return false; }

            var dict = new Dictionary<DbColumn, object>();
            for (int i = 0; i < dbEntity.PKColumns.Count; i++) {
                var value = dbEntity.PKColumns[i].CSharpTypeName switch {
                    "string" => (object)jsonValues[i].GetString(),
                    "int" => jsonValues[i].GetInt32(),
                    _ => throw new InvalidOperationException($"{dbEntity.PKColumns[i].PropertyName} の主キーの型が非対応: {dbEntity.PKColumns[i].CSharpTypeName}"),
                };
                dict.Add(dbEntity.PKColumns[i], value);
            }
            instanceKey = new InstanceKey {
                DbEntity = dbEntity,
                ParsedValue = dict,
            };
            return true;
        }

        private InstanceKey() { }

        public DbEntity DbEntity { get; private init; }
        public IReadOnlyDictionary<DbColumn, object> ParsedValue { get; private init; }

        public string StringValue => JsonSerializer.Serialize(ParsedValue.Select(v => v.Value));
    }
}
