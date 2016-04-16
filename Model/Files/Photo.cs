using System.Linq;
using Newtonsoft.Json;

namespace Model.Files
{
    public class Photo:IBaseFile
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("photo_604")]
        public string Url { get; set; }
        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }
        [JsonProperty("user_id")]
        public int UserId { get; set; }

        public string FileName
        {
            get { return Url.Split('/').Last(); }
            set { }
        }
    }
}
