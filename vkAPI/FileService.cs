using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;

namespace vkAPI
{
    public class FileService:BaseService
    {
        string UploadFile() 
        {
            using (var client = new System.Net.WebClient())
            {
                var url = "https://api.vk.com/method/docs.getUploadServer&v=5.45";
                var fileUrl = GetUrl(url);
                url = "https://api.vk.com/method/docs.save&v=5.45";
            }
            return null;
        }
    }
}
