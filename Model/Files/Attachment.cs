using Newtonsoft.Json;

namespace Model.Files
{
    public class Attachment:BaseWebFile
    {

        /// <summary>
        /// "doc", "photo"
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
            get
            {
                return GetFile();
            }
        }

        public bool IsEncrypted { get; set; }

        public string EncryptedSymmetricKey { get; set; }


    }
}
