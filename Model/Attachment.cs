using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Model.Annotations;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Model
{
    public class Attachment
    {
        /// <summary>
        /// "doc", "photo", "audio", "video", "link"
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("doc")]
        public Document Document { get; set; }
        [JsonProperty("photo")]
        public Photo Photo { get; set; }

        private IBaseFile GetFile()
        {
            switch (Type)
            {
                case "doc":
                    return Document;
                case "photo":
                    return Photo;
                default:
                    return null;
            }
        }

        public IBaseFile File
        {
            get { return GetFile(); }
        }

        public bool IsEncrypted { get; set; }

    }
  
}
