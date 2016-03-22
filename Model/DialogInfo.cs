using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Model
{
    public class DialogInfo
    {
        [JsonProperty("message")]
        public Message Message { get; set; }
        [JsonProperty("unread")]
        public int UnreadMessagesAmount { get; set; }
    }
}
