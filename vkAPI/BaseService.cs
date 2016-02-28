using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace vkAPI
{
    public class BaseService
    {
        protected JObject GetUrl(string url)
        {
            

            using (var client = new System.Net.WebClient())
            {
                client.Encoding = Encoding.UTF8;
                var response = client.DownloadString(url);
                Console.WriteLine(response);
                return JObject.Parse(response);
            }
        }
    }
}
