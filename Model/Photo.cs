using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Model
{
    public class Photo:IBaseFile
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("photo_1280")]
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
