using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace vkAPI
{
    public class BaseService
    {
        protected async Task<JObject> GetUrl(string url)
        {
            using (var client = new System.Net.WebClient())
            {
                client.Encoding = Encoding.UTF8;
                var response = await client.DownloadStringTaskAsync(url);
                return JObject.Parse(response);
            }
        }
    }
}
