using Newtonsoft.Json;

namespace Model
{
    public class UploadFile
    {
        [JsonProperty("did")]
        public int Id { get; set; }
        [JsonProperty ("title")]

        public string FileName { get; set; }
        [JsonProperty("url")]

        public string Url { get; set; }
        [JsonProperty("ext")]
        public string Extension { get; set; }
    }
}