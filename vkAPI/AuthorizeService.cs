using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Model;

namespace vkAPI
{

    public class AuthorizeService:BaseService
    {

        private static readonly AuthorizeService instance = new AuthorizeService();

        public static AuthorizeService Instance
        {
            get { return instance; }
        }

        protected AuthorizeService() { }

        public string AccessToken { get; set; }
        public int CurrentUserId { get; set; }

        public User CurrentUser { get; set; }

        public string GetAuthorizeUrl(int appId)
        {
            var url =
                $"https://oauth.vk.com/authorize?client_id={appId}&display=popup&revoke=1&scope=friends,messages,docs&response_type=token&redirect_uri=https://oauth.vk.com/blank.html&v=5.45";
            return url;
        }

        public async void GetCurrentUser()
        {
            CurrentUser = await new UserService().GetUser(CurrentUserId);
        }

    }
}
