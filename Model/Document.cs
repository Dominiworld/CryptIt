using Newtonsoft.Json;

namespace Model
{
    public class Document
    {
        [JsonProperty("did")]
        public int Id { get; set; }
        [JsonProperty ("title")]

        public string FileName { get; set; }
        [JsonProperty("url")]

        public string Url { get; set; }
        [JsonProperty("ext")]
        public string Extension { get; set; }
        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }
        /// <summary>
        /// имя файла до шифровки
        /// </summary>
        public string FullName { get; set; }

    }
}