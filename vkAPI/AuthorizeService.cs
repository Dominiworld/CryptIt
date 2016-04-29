using System.Configuration;
using Model;

namespace vkAPI
{

    public class AuthorizeService:BaseService
    {
        public static AuthorizeService Instance { get; } = new AuthorizeService();

        protected AuthorizeService() { }


        public string AccessToken { get; set; }
        public int CurrentUserId { get; set; }

        public User CurrentUser { get; set; }

        public string GetAuthorizeUrl()
        {
            var appId = int.Parse(ConfigurationManager.AppSettings["app_id"]);
            var url =
                $"https://oauth.vk.com/authorize?client_id={appId}&display=popup&revoke=1&scope=friends,messages,docs&response_type=token&redirect_uri=https://oauth.vk.com/blank.html&v=5.45";

            //var url =
            //    $"https://oauth.vk.com/authorize?client_id={appId}&display=popup&scope=friends,messages,docs,photos&response_type=token&redirect_uri=https://oauth.vk.com/blank.html&v=5.45";
            return url;
        }

        public async void GetCurrentUser()
        {
            CurrentUser = await new UserService().GetUser(CurrentUserId);
        }
    }
}
