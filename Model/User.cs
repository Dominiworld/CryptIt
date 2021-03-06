﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.CompilerServices;
using Model.Annotations;
using Newtonsoft.Json;

namespace Model
{
    public class User: INotifyPropertyChanged
    {
        private int? _numberOfNewMessages;
        private bool _keyExists;
        private string _key;

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
        [JsonProperty("online_app")]
        public int OnlineApp { get; set; }

        public string Status
        {
            get
            {
                if (Online==0)
                {
                    return "";
                }
                if (OnlineApp!= int.Parse(ConfigurationManager.AppSettings["app_id"]))
                {
                    return "Online";
                }
                return "Online_crypto";
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

        public bool KeyExists
        {
            get { return _keyExists; }
            set
            {
                if (value == _keyExists) return;
                _keyExists = value;
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
