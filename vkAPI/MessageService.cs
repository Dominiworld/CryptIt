using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;

namespace vkAPI
{
    public class MessageService:BaseService
    {
        private UserService _userService = new UserService();
        public async Task<IEnumerable<MessageInfo> >GetDialogs()
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = "https://api.vk.com/method/messages.getDialogs?v=5.45&access_token="+token;
            var obj = await GetUrl(url);
            var json =
                JsonConvert.DeserializeObject<IEnumerable<MessageInfo>>(obj["response"]["items"].ToString());
            foreach (var messageInfo in json.ToArray())
            {
                messageInfo.Message.User =await _userService.GetUser(messageInfo.Message.UserId);
            }
            return json;
        }

        public async Task<IEnumerable<Message>> GetDialog(int userId)
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = string.Format("https://api.vk.com/method/messages.getHistory?user_id={0}&v=5.45&access_token={1}",
                userId, token);
            var obj = await GetUrl(url);
            var messages = JsonConvert.DeserializeObject<IEnumerable<Message>>(obj["response"]["items"].ToString());

            var currentUser = await _userService.GetUser(AuthorizeService.Instance.CurrentUserId);
            foreach (var message in messages.ToArray())
            {
                message.User = message.Out ? currentUser : await _userService.GetUser(message.UserId);
            }
            return messages.ToList();
        }

        public async void SendMessage(int userId, string message)
        {
            var token = AuthorizeService.Instance.AccessToken;

            var url =String.Format("https://api.vk.com/method/messages.send?v=5.45&user_id={0}&message={1}&access_token={2}",
                userId, message,token);
            await GetUrl(url);
        }

        //public async Task<Message> ConnectToLongPollServer(bool useSsl, bool needPts)
        //{
        //    var url = String.Format("https://api.vk.com/method/messages.send?v=5.45&use_ssl={0}&need_pts={1}",
        //       useSsl, needPts);
        //    var obj = await GetUrl(url);
        //    var connectionSettings = JsonConvert.DeserializeObject<LongPollConnectionSettings>(obj.ToString());

        //    while (connectionSettings.TS!=0)
        //    {
        //        url = String.Format("http://{0}?act=a_check&key={1}&ts={2}&wait=25&mode=2", 
        //        connectionSettings.Adress, connectionSettings.Key, connectionSettings.TS);
        //        obj = await GetUrl(url);
        //        var updates = JsonConvert.DeserializeObject<LongPoolServerResponse>(obj.ToString());
        //        connectionSettings.TS = updates.Ts;
        //        foreach (var update in updates.Updates)
        //        {
        //            var components = update.Split(',');
        //            if (int.Parse(components[0])==4)
        //            {
        //                var message = new Message
        //                {
        //                    Body = components[6],
        //                    UserId = int.Parse(components[3]),
        //                    UnixTime = int.Parse(components[4])
        //                };
        //                return message;
        //            }
        //        }
        //    }

        //    return null;
        //}

    }
}
