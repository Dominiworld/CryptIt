﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace vkAPI
{
    public class UserService:BaseService
    {     
        public User GetUser(int id) //id, first_name, last_name, photoUrl
        {      
            using (var client = new System.Net.WebClient())
            {
                var url =
                    "https://api.vk.com/method/users.get?user_id=" + id + "&v=5.45&fields=photo_50";
                var obj = GetUrl(url);
                return JsonConvert.DeserializeObject<User>(obj["response"][0].ToString());
            }
        }

        public IEnumerable<User> GetFriends(int userId)//only ids
        {
            var url = "https://api.vk.com/method/friends.get?user_id=" + userId + "&fields=nickname,photo_50,online&v=5.45";
            var obj = GetUrl(url);
            return JsonConvert.DeserializeObject<IEnumerable<User>>(obj["response"]["items"].ToString());
        }
        
    }
}