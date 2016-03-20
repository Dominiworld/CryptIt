using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;

namespace vkAPI
{
    public class LongPollServerService:BaseService
    {
        public delegate void GotNewMessage(Message message);

        public delegate void MessageStateChangedToRead(int lastReadId, int peerId);

        public delegate void UserBecameOnlineOrOffline(int userId, bool online); //online = 1, если стал онлайн
        //=0, если стал оффлайн

        public event GotNewMessage GotNewMessageEvent;

        public event MessageStateChangedToRead MessageStateChangedToReadEvent;
        public event UserBecameOnlineOrOffline UserBecameOnlineOrOfflineEvent;
        public async void ConnectToLongPollServer(bool useSsl = true, bool needPts = true)
        {
            var token = AuthorizeService.Instance.AccessToken;

            var url =
                $"https://api.vk.com/method/messages.getLongPollServer?v=5.45&use_ssl={useSsl}&need_pts={needPts}&access_token={token}";
            var obj = await GetUrl(url);
            var connectionSettings = JsonConvert.DeserializeObject<LongPollConnectionSettings>(obj["response"].ToString());

            while (connectionSettings.TS != 0)
            {
                url =
                    $"http://{connectionSettings.Adress}?act=a_check&key={connectionSettings.Key}&ts={connectionSettings.TS}&wait=25&mode=2";
                obj = await GetUrl(url);

                var updates = JsonConvert.DeserializeObject<LongPoolServerResponse>(obj.ToString());
                connectionSettings.TS = updates.Ts;

                foreach (var update in updates.Updates)
                {
                    switch (int.Parse(update[0].ToString()))
                    {
                        case 4:
                            var message = new Message
                            {
                                Id = int.Parse(update[1].ToString()),
                                Body = update[6].ToString(),
                                UserId = int.Parse(update[3].ToString()),
                                UnixTime = int.Parse(update[4].ToString()),
                                Out = (int.Parse(update[2].ToString()) & 2) != 0, //+2 - OUTBOX   
                                IsNotRead = (int.Parse(update[2].ToString()) & 1) != 0 //+1 - UNREAD
                            };
                            GotNewMessageEvent?.Invoke(message);
                            break;
                        case 7:
                            var userId = int.Parse(update[1].ToString());
                            var lastReadMessageId = int.Parse(update[2].ToString());
                            MessageStateChangedToReadEvent?.Invoke(lastReadMessageId, userId);
                            break;
                        case 8:
                            UserBecameOnlineOrOfflineEvent?.Invoke(int.Parse(update[0].ToString()), true);
                            break;
                        case 9:
                            UserBecameOnlineOrOfflineEvent?.Invoke(int.Parse(update[0].ToString()), false);
                            break;
                        default:
                            break;
                    }



                }
            }
        }
    }
}
