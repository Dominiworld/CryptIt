using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Model.Annotations;
using Model.Files;
using Newtonsoft.Json;

namespace Model
{
    public class Message:INotifyPropertyChanged
    {

        private bool _isNotRead;

        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("user_id")]
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime Date
        {
            get
            {
                DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc);
                return dtDateTime.AddSeconds(UnixTime).ToLocalTime();
            }
        }

        [JsonProperty("date")]
        public int UnixTime { get; set; }//количство секунд с 1.01.1970
        [JsonProperty("out")]
        public bool Out { get; set;  } //0-полученное, 1-отправленное
        [JsonProperty("body")]
        public string Body { get; set; } //принятое сообщение

        public bool IsNotRead
        {
            get { return _isNotRead; }
            set
            {
                _isNotRead = value; 
                OnPropertyChanged();
            }
        }

        [JsonProperty("attachments")]
        public ObservableCollection<Attachment> Attachments { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
