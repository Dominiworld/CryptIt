using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CryptIt.Commands;
using CryptIt.View;
using Model;
using vkAPI;

namespace CryptIt.ViewModel
{
    public class MainViewModel:ViewModelBase
    {
        private MessageService _messageService;
        private UserService _userService;
        private User _selectedUser;
        private List<Message> _messages;
        private List<User> _friends;
        private string _message;


        public MainViewModel()
        {
            _messageService = new MessageService();
            _userService = new UserService();
            _messageService.GotNewMessageEvent += RefreshMessages;
            _messageService.ConnectToLongPollServer();

            SendMessageCommand = new DelegateCommand(SendMessage);
            GetFriends();
        }

        private async void RefreshMessages(Message message)
        {
            if (message.UserId == SelectedUser.Id)
            {
                message.User = message.Out ? AuthorizeService.Instance.CurrentUser : SelectedUser;
                var messages = new List<Message>
                {
                    message
                };
                messages.AddRange(Messages);
                Messages = messages;
            }
        }

        public DelegateCommand SendMessageCommand { get; set; }
        private async void GetFriends()
        {
            Friends = (await _userService.GetFriends(AuthorizeService.Instance.CurrentUserId)).ToList();
        }

        private async void SendMessage()
        {
            //todo обработка потери соединения
           _messageService.SendMessage(SelectedUser.Id, Message);           
           Message = String.Empty;
        }

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value; 
                OnPropertyChanged();
            }
        }

        public List<User> Friends
        {
            get { return _friends; }
            set
            {
                _friends = value;
                OnPropertyChanged();
            }
        }

        public List<Message> Messages
        {
            get { return _messages; }
            set
            {
                _messages = value;
                OnPropertyChanged();
            }
        }

        public User SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                _selectedUser = value;
                GetMessages();
                OnPropertyChanged();
            }
        }

        private async void GetMessages()
        {
            Messages = (await _messageService.GetDialog(SelectedUser.Id)).ToList();
        }

    }
}
