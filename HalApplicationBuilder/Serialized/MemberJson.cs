using System;
using HalApplicationBuilder.Core;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HalApplicationBuilder.Serialized {

    public class MemberJson {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }
        [JsonPropertyName("primary")]
        public bool? IsPrimary { get; set; }
        [JsonPropertyName("isInstanceName")]
        public bool? IsInstanceName { get; set;}
        [JsonPropertyName("nullable")]
        public bool? IsNullable { get; set; }
        [JsonPropertyName("child")]
        public AggregateJson? Child { get; set; }
        [JsonPropertyName("children")]
        public AggregateJson? Children { get; set; }
        [JsonPropertyName("variations")]
        public Dictionary<int, AggregateJson>? Variations { get; set; }
        [JsonPropertyName("refTarget")]
        public string? RefTarget { get; set; }
    }
}

