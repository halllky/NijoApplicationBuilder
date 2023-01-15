using System;
using System.Collections.Generic;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.Core.UIModel {

    public class InstanceName : ValueObject {
        public static InstanceName Create(object dbInstance, DbEntity dbEntity) {
            var type = dbInstance.GetType();
            var name = "";
            foreach (var col in dbEntity.InstanceNameColumns) {
                var prop = type.GetProperty(col.PropertyName);
                name += prop.GetValue(dbInstance);
            }
            return new InstanceName { Value = name };
        }

        private InstanceName() { }

        public string Value { get; private init; }

        protected override IEnumerable<object> ValueObjectIdentifiers() {
            yield return Value;
        }
    }
}
