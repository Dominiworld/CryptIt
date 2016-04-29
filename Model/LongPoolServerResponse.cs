using System.Collections.Generic;
using Newtonsoft.Json;

namespace Model
{
   public  class LongPoolServerResponse
    {
       [JsonProperty("ts")]
       public int Ts { get; set; }
       [JsonProperty("updates")]
       public List<List<object>> Updates { get; set; }
        [JsonProperty("failed")]
        public int ErrorCode { get; set; }
    }
}
