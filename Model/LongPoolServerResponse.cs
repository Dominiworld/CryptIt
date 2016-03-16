using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Model
{
   public  class LongPoolServerResponse
    {
       [JsonProperty("ts")]
       public int Ts { get; set; }
       [JsonProperty("updates")]
       public string[] Updates { get; set; }
    }
}
