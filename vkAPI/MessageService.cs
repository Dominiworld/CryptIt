using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;

namespace vkAPI
{
    public class MessageService:BaseService
    {
        public IEnumerable<MessageInfo> GetDialogs()
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = "https://api.vk.com/method/messages.getDialogs?v=5.45&access_token="+token;
            var obj = GetUrl(url);

            var json =
                JsonConvert.DeserializeObject<IEnumerable<MessageInfo>>(obj["response"]["items"].ToString());
            return json;
        }

        public IEnumerable<Message> GetMessages(int isOutComming)
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = string.Format("https://api.vk.com/method/messages.get?out={0}&v=5.45&access_token={1}",
                isOutComming, token);
            var obj = GetUrl(url);
            return JsonConvert.DeserializeObject<IEnumerable<Message>>(obj["response"]["items"].ToString());
        }

    }
}
