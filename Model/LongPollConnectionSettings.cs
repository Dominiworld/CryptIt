using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Model
{
    public class LongPollConnectionSettings
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("server")]
        public string Adress { get; set; }
        [JsonProperty("ts")]
        public int TS { get; set; }

    }
}
