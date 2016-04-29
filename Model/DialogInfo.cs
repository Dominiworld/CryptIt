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
