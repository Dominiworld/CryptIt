using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace vkAPI
{
    public class FileService
    {
        MessageService _messageService = new MessageService();

        public async Task<string> UploadFile(string fileName, int userId) //returns file url
        {
            using (var client = new System.Net.WebClient())
            {
                var token = AuthorizeService.Instance.AccessToken;
                var u = "https://api.vk.com/method/docs.getUploadServer?access_token=" + token;
                var r = await client.DownloadStringTaskAsync(u);
                var j = JsonConvert.DeserializeObject(r) as JObject;
                
                var u2 = j["response"]["upload_url"].ToString();
                var r2 = Encoding.UTF8.GetString(await client.UploadFileTaskAsync(u2, "POST", fileName));
                var j2 = JsonConvert.DeserializeObject(r2) as JObject;
                if (j2["file"] == null)
                {
                    MessageBoxResult errorDialog = MessageBox.Show("Ошибка загрузки файла");
                    return null;
                }
                //
                var u3 = "https://api.vk.com/method/docs.save?access_token=" + token
                         + "&file=" + j2["file"];
                var r3 = await client.DownloadStringTaskAsync(u3);

                var j3 = JsonConvert.DeserializeObject(r3) as JObject;
                return j3["response"][0]["url"].ToString();
            }
        }
    }
}
