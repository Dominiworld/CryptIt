using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace vkAPI
{
    public class FileService:BaseService
    {
        MessageService _messageService = new MessageService();

      

        public async Task<Document> UploadFile(string fileName, int userId) 
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
                var u3 = "https://api.vk.com/method/docs.save?v=5.45&access_token=" + token
                         + "&file=" + j2["file"];
                var docObj = await GetUrl(u3);
            
                var doc = JsonConvert.DeserializeObject<Document>(docObj["response"][0].ToString());
                return doc;
            }
        }

        public async Task DownloadFile(string url, string path, string fileName)
        {           
            using (var client = new WebClient())
            {
                client.DownloadFile(url, path + "\\"+fileName);     
                //await client.DownloadFileTaskAsync(url , path + "\\"+fileName);
            }

        }

        public async Task<List<Document>> GetDocuments(List<string> fullIds)
        {
            if (!fullIds.Any() || (fullIds.Count==1 && fullIds[0]==string.Empty))
            {
                return null;
            }
            var token = AuthorizeService.Instance.AccessToken;
            var ids = string.Join(",", fullIds);
            var url = $"https://api.vk.com/method/docs.getById?docs={ids}&access_token={token}";
            var objs = await GetUrl(url);
            var docs = JsonConvert.DeserializeObject<List<Document>>(objs["response"].ToString());
            return docs.ToList();
        }
    }
    
}
