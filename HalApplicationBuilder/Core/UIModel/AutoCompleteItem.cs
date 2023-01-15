using System.Text.Json.Serialization;

namespace HalApplicationBuilder.Core.UIModel {
    public class AutoCompleteSource {
        [JsonPropertyName("value")] // この名前にするとjQueryのautocompleteが認識してくれる
        public string InstanceKey { get; set; }
        [JsonPropertyName("label")] // この名前にするとjQueryのautocompleteが認識してくれる
        public string InstanceName { get; set; }
    }
}
