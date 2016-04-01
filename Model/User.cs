using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Model.Annotations;
using Newtonsoft.Json;

namespace Model
{
    public class User: INotifyPropertyChanged
    {
        private int? _numberOfNewMessages;

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }
        [JsonProperty("last_name")]
        public string LastName { get; set; }
        [JsonProperty("photo_50")]
        public string PhotoUrl { get; set; }
        [JsonProperty("online")]
        public int Online { get; set; }

        public string Status
        {
            get
            {
                if (Online==0)
                {
                    return "";
                }
                return "Online";
            }
        }

        public IEnumerable<User> Friends { get; set; }

        public string FullName => LastName + " " + FirstName;

        public int? NumberOfNewMessages
        {
            get { return _numberOfNewMessages; }
            set
            {
                _numberOfNewMessages = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
