using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace vkAPI
{
    public class MessageService:BaseService
    {
        private UserService _userService;
        public MessageService()
        {
            _userService = new UserService();
        }

        public async Task<IEnumerable<DialogInfo>> GetDialogs(bool onlyUnread)
        {
            var dialogsInfo = new List<DialogInfo>();
            var token = AuthorizeService.Instance.AccessToken;
            var unread = onlyUnread ? 1 : 0;
            var url = $"https://api.vk.com/method/messages.getDialogs?v=5.45&access_token={token}&unread={unread}";
            var obj = JsonConvert.DeserializeObject((await GetUrl(url)).ToString()) as JObject;
            var items = obj?["response"]?["items"].ToList();

            if (items == null ||items.Count == 0)
                return dialogsInfo;  
              
            dialogsInfo.AddRange(
                    items.Select(item => JsonConvert.DeserializeObject<DialogInfo>(item.ToString())));

           
            return dialogsInfo;
        }

        public async Task<IEnumerable<Message>> GetDialog(int userId,int offset = 0)
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = $"https://api.vk.com/method/messages.getHistory?user_id={userId}&v=5.45&access_token={token}&offset={offset}";
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

        public async Task<int> SendMessage(int userId, Message message)
        {
            if (message == null)
            {
                return 0;
            }
            var token = AuthorizeService.Instance.AccessToken;

            var url =
                $"https://api.vk.com/method/messages.send?v=5.45&user_id={userId}&message={message.Body}&access_token={token}";
            if (message.Attachments!=null)
            {
                var attachments = message.Attachments.Select(a => a.Type + a.Document.OwnerId + "_" + a.Document.Id);
                var attachmentString = string.Join(",", attachments);
                url+= $"&attachment={attachmentString}"; 
            }          

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
