using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Model
{
    public class MessageInfo
    {
        [JsonProperty("message")]
        public Message Message { get; set; }
        [JsonProperty("in_read")]
        public int LastReadMessageId { get; set; }
        [JsonProperty("out_read")]
        public int LastReadPeerMessageId { get; set; }

    }
}
