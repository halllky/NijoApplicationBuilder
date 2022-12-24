using System;
namespace haldoc.Runtime {
    /// <summary>
    /// 集約外部のインスタンス
    /// </summary>
    public class ExternalObject {
        public Guid AggregateGUID { get; set; }
        /// <summary>複合キーの場合はJSON</summary>
        public string Key { get; set; }
        public string InstanceName { get; set; }
    }
}
