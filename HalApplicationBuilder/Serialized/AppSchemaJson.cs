using System;
using System.Text.Json.Serialization;

namespace HalApplicationBuilder.Serialized {
    public class AppSchemaJson {
        [JsonPropertyName("aggregates")]
        public AggregateJson[]? Aggregates { get; set; }
    }
}

