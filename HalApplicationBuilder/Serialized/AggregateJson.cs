using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HalApplicationBuilder.Serialized {
    public class AggregateJson {
        [JsonPropertyName("guid")]
        public Guid? Guid { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("members")]
        public MemberJson[]? Members { get; set; }
    }
}

