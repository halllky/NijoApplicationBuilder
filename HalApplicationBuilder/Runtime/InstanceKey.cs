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
            var values = dbEntity.PKColumns.Count == 1
                ? new[] { JsonSerializer.Deserialize<object>(key) }
                : JsonSerializer.Deserialize<object[]>(key);

            if (dbEntity.PKColumns.Count != values.Length) { instanceKey = null; return false; }

            instanceKey = new InstanceKey {
                DbEntity = dbEntity,
                ParsedValue = values
                    .Select((value, index) => new { col = dbEntity.PKColumns[index], value })
                    .ToDictionary(x => x.col, x => x.value),
            };
            return true;
        }

        private InstanceKey() { }

        public DbEntity DbEntity { get; private init; }
        public IReadOnlyDictionary<DbColumn, object> ParsedValue { get; private init; }

        public string StringValue => JsonSerializer.Serialize(ParsedValue.Select(v => v.Value));
    }
}
