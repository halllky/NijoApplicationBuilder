using System;
namespace haldoc.Schema {
    /// <summary>
    /// これが無いとAggregateRootがついていないクラスの型のプロパティが
    /// ComplexTypeなのか集約外部への参照なのか判別できない
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Nested<T> where T : class {
        public Nested() {
        }
    }
}
