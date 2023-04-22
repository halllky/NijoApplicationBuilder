using System;
namespace HalApplicationBuilder.Runtime {
    public class ReferenceDTO {
        public Guid AggreageteGuid { get; set; }
        public string InstanceKey { get; set; } = string.Empty;
        public string InstanceName { get; set; } = string.Empty;

        internal const string TS_TYPE_NAME = nameof(ReferenceDTO);
        internal const string TS_KEY_PROP_NAME = nameof(InstanceKey);
        internal const string TS_NAME_PROP_NAME = nameof(InstanceName);
    }
}
