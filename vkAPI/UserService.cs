using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace vkAPI
{
    public class UserService:BaseService
    {     
        public async Task<IEnumerable<User>> GetUsers(List<int> ids) //id, first_name, last_name, photoUrl
        {                 
            var token = AuthorizeService.Instance.AccessToken;
            var usersString = string.Join(",", ids);
            var url =
                $"https://api.vk.com/method/users.get?v=5.45&fields=photo_50,online&access_token={token}&user_ids={usersString}";
            var obj = await GetUrl(url);
            return JsonConvert.DeserializeObject<IEnumerable<User>>(obj["response"].ToString());            
        }

        public async Task<User> GetUser(int? id) //id, first_name, last_name, photoUrl
        {
            if (id == null) return null;
            return (await GetUsers(new List<int> { id.Value })).ToList().FirstOrDefault();
        }


        public async Task<IEnumerable<User>> GetFriends(int userId)//only ids
        {
            var token = AuthorizeService.Instance.AccessToken;
            var url = $"https://api.vk.com/method/friends.get?user_id={userId}&fields=nickname,photo_50,online&v=5.45&access_token={token}";
            var obj = await GetUrl(url);
            return JsonConvert.DeserializeObject<IEnumerable<User>>(obj["response"]["items"].ToString());
        }

        public async Task<IEnumerable<User>> GetUsersFromDialogs()
        {

            var token = AuthorizeService.Instance.AccessToken;
            var url = $"https://api.vk.com/method/messages.getDialogs?v=5.45&access_token={token}&count=200";
            var obj = JsonConvert.DeserializeObject((await GetUrl(url)).ToString()) as JObject;
            if (obj == null && obj["response"] == null)
                return new List<User>();
            var items = obj["response"]["items"].ToList();
            var userIds = items.Select(m => JsonConvert.DeserializeObject<DialogInfo>(m.ToString()).Message.UserId).ToList();
            return await  GetUsers(userIds.ToList());
        }


    }
}
