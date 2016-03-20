using System.Collections.Generic;
using Newtonsoft.Json;

namespace Model
{
    public class User
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }
        [JsonProperty("last_name")]
        public string LastName { get; set; }
        [JsonProperty("photo_50")]
        public string PhotoUrl { get; set; }
        [JsonProperty("online")]
        public int Online { get; set; }

        public string Status
        {
            get
            {
                if (Online==0)
                {
                    return "";
                }
                return "Online";
            }

            set { throw new System.NotImplementedException(); }
        }

        public IEnumerable<User> Friends { get; set; }

        public string FullName => FirstName + " " + LastName;
    }
}
