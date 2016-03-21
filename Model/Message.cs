using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Model
{
    public class Message
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("user_id")]
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime Date
        {
            get
            {
                DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc);
                return dtDateTime.AddSeconds(UnixTime).ToLocalTime();
            }
        }

        [JsonProperty("date")]
        public int UnixTime { get; set; }//количство секунд с 1.01.1970
        [JsonProperty("out")]
        public bool Out { get; set;  } //0-полученное, 1-отправленное
        [JsonProperty("body")]
        public string Body { get; set; } //принятое сообщение
        public bool IsNotRead { get; set; }
        

    }
}
