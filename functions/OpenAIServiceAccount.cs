using Newtonsoft.Json;

namespace Function
{
    class OpenAIServiceAccount
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("deployName")]
        public string DeployName { get; set; }
    }
}
