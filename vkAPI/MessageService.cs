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

        public delegate void GotNewMessage(Message message);

        public event GotNewMessage GotNewMessageEvent;

        private UserService _userService = new UserService();
        public async Task<IEnumerable<Message> >GetDialogs()
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = "https://api.vk.com/method/messages.getDialogs?v=5.45&access_token="+token;
            var obj = await GetUrl(url);
            var json =
                JsonConvert.DeserializeObject<IEnumerable<Message>>(obj["response"]["items"].ToString());
            foreach (var messageInfo in json.ToArray())
            {
                messageInfo.User =await _userService.GetUser(messageInfo.UserId);
            }
            return json;
        }

        public async Task<IEnumerable<Message>> GetDialog(int userId)
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = string.Format("https://api.vk.com/method/messages.getHistory?user_id={0}&v=5.45&access_token={1}",
                userId, token);
            var obj = await GetUrl(url);
            var messages = JsonConvert.DeserializeObject<List<Message>>(obj["response"]["items"].ToString());
            var lastPeerReadId = JsonConvert.DeserializeObject<int>(obj["response"]["out_read"].ToString());

            if (messages.Count!=0)
            {
                var otherUser = await _userService.GetUser(messages[0].UserId);
                foreach (var message in messages.ToArray())
                {
                    message.User = message.Out ? AuthorizeService.Instance.CurrentUser : otherUser;
                    if ((lastPeerReadId < message.Id) && message.Out)
                    {
                        message.IsNotRead = true;
                    }
                }
            }          
            return messages;
        }

        public async void SendMessage(int userId, string message)
        {
            var token = AuthorizeService.Instance.AccessToken;

            var url =String.Format("https://api.vk.com/method/messages.send?v=5.45&user_id={0}&message={1}&access_token={2}",
                userId, message,token);
            await GetUrl(url);
        }

        public async void ConnectToLongPollServer(bool useSsl=true, bool needPts=true)
        {
            var token = AuthorizeService.Instance.AccessToken;

            var url = String.Format("https://api.vk.com/method/messages.getLongPollServer?v=5.45&use_ssl={0}&need_pts={1}&access_token={2}",
               useSsl, needPts, token);
            var obj = await GetUrl(url);
            var connectionSettings = JsonConvert.DeserializeObject<LongPollConnectionSettings>(obj["response"].ToString());

            while (connectionSettings.TS != 0)
            {
                url = String.Format("http://{0}?act=a_check&key={1}&ts={2}&wait=25&mode=2",
                connectionSettings.Adress, connectionSettings.Key, connectionSettings.TS);
                obj = await GetUrl(url);
               
                var updates = JsonConvert.DeserializeObject<LongPoolServerResponse>(obj.ToString());
                connectionSettings.TS = updates.Ts;
                
                foreach (var update in updates.Updates)
                {
                    if (int.Parse(update[0].ToString()) == 4)
                    {
                        var message = new Message
                        {
                            Body = update[6].ToString(),
                            UserId = int.Parse(update[3].ToString()),
                            UnixTime = int.Parse(update[4].ToString()),
                            Out = (int.Parse(update[2].ToString()) & 2)!=0, //+2 - OUTBOX   
                            IsNotRead = (int.Parse(update[2].ToString()) & 1)!=0 //+1 - UNREAD
                        };
                        
                        if (GotNewMessageEvent != null) GotNewMessageEvent(message);
                    }
                }
            }
        }

    }
}
