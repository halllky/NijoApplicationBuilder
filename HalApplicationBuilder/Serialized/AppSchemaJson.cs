using System;
using System.Text.Json.Serialization;

namespace HalApplicationBuilder.Serialized {
    public class AppSchemaJson {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("config")]
        public ConfigJson? Config { get; set; }
        [JsonPropertyName("aggregates")]
        public AggregateJson[]? Aggregates { get; set; }
    }
}

