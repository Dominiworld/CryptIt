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
        private UserService _userService;
        public MessageService()
        {
            _userService = new UserService();
        }

        public async Task<IEnumerable<Message>>GetDialogs()
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = "https://api.vk.com/method/messages.getDialogs?v=5.45&access_token="+token;
            var obj = await GetUrl(url);
            var json =
                JsonConvert.DeserializeObject<IEnumerable<Message>>(obj["response"]["items"].ToString());

            foreach (var messageInfo in json.ToArray())
            {
                messageInfo.User =  await _userService.GetUser(messageInfo.UserId);
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

        public async Task<int> SendMessage(int userId, string message)
        {
            if (message == null)
            {
                return 0;
            }
            var token = AuthorizeService.Instance.AccessToken;

            var url =
                $"https://api.vk.com/method/messages.send?v=5.45&user_id={userId}&message={message}&access_token={token}";
            var id =  await GetUrl(url);
            return JsonConvert.DeserializeObject<int>(id["response"].ToString());
        }

        public async void MarkMessagesAsRead(List<int> messageIds, int peerId)
        {
            string messageIdsString = string.Join(",", messageIds);
            var token = AuthorizeService.Instance.AccessToken;
            var url =
                $"https://api.vk.com/method/messages.markAsRead?v=5.45&access_token={token}&peer_id={peerId}&message_ids={messageIdsString}";
            await GetUrl(url);
        }

       

    }
}
