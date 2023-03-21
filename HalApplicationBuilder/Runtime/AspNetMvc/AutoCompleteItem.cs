using System.Text.Json.Serialization;

namespace HalApplicationBuilder.Runtime.AspNetMvc {
    public class AutoCompleteSource {
        [JsonPropertyName("value")] // この名前にするとjQueryのautocompleteが認識してくれる
        public required string InstanceKey { get; set; }
        [JsonPropertyName("label")] // この名前にするとjQueryのautocompleteが認識してくれる
        public required string InstanceName { get; set; }
    }
}
