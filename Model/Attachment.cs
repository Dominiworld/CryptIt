using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Model
{
    public class Attachment
    {
        /// <summary>
        /// "doc", "photo", "audio", "video", "link"
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("doc")]
        public Document Document { get; set; }
        //todo add other files
    }
  
}
