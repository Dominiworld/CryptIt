using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Navigation;
using CryptIt.Commands;
using CryptIt.View;
using Model;
using vkAPI;
using Message = Model.Message;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace CryptIt.ViewModel
{
    public class MainViewModel:ViewModelBase
    {
        private LongPollServerService _longPollServer;
        private MessageService _messageService;
        private UserService _userService;
        private FileService _fileService;
        private User _selectedUser;
        private List<Message> _messages;
        private List<User> _friends;
        private string _message;
        private UploadFile _fileToUpload;
        private bool _isFileUploading;


        public MainViewModel()
        {
            _messageService = new MessageService();
            _userService = new UserService();
            _longPollServer = new LongPollServerService();
            _fileService = new FileService();
            _longPollServer.GotNewMessageEvent += AddMessages;
            _longPollServer.MessageStateChangedToReadEvent += ChangeMessagesStateToRead;
            _longPollServer.UserBecameOnlineOrOfflineEvent += ChangeUserOnlineStatus;
            _longPollServer.ConnectToLongPollServer();

            SendMessageCommand = new DelegateCommand(SendMessage);
            UploadFileCommand = new DelegateCommand(OpenFileDialog);
            GetFriends();
        }

        public bool IsFileUploading
        {
            get { return _isFileUploading; }
            set
            {
                _isFileUploading = value;
                OnPropertyChanged();
            }
        }


        public UploadFile FileToUpload
        {
            get { return _fileToUpload; }
            set
            {
                _fileToUpload = value;
                OnPropertyChanged();
            }
        }

        public DelegateCommand UploadFileCommand { get; set; }

        private async void OpenFileDialog()
        {
            if (SelectedUser==null)
            {
                return;
            }
            var dialog = new OpenFileDialog {Multiselect = true};
            if (dialog.ShowDialog()==true)
            {
                FileToUpload = new UploadFile {FileName = dialog.FileName};
                IsFileUploading = true;
                FileToUpload.Url = await _fileService.UploadFile(dialog.FileName, SelectedUser.Id);
                IsFileUploading = false;
                FileToUpload.FileName = dialog.FileName;
            }
        }

        private void ChangeUserOnlineStatus(int userId, bool online)
        {
            var friend = Friends.FirstOrDefault(f => f.Id == userId);
            if (friend != null) friend.Status = online ? "Online" : "";
        }

        private void ChangeMessagesStateToRead(int lastReadId, int peerId)
        {
            if (SelectedUser.Id == peerId)
            {
                var messages = Messages;
                foreach (var message in messages.ToArray().Where(m=>m.Id <= lastReadId))
                {
                    message.IsNotRead = false;                   
                }
                Messages = messages;
            }
        }

        private async void AddMessages(Message message)
        {
            if (message.UserId == SelectedUser.Id)
            {
                message.User = message.Out ? AuthorizeService.Instance.CurrentUser : SelectedUser;
                if (!message.Out)
                {
                    message.IsNotRead = false;
                    _messageService.MarkMessagesAsRead(new List<int> { message.Id }, SelectedUser.Id);
                }
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
            if (IsFileUploading)
            {
                MessageBoxResult errorDialog = MessageBox.Show("Подождите окончания загрузки");
                return;
                
            }
            //todo обработка потери соединения
            if (string.IsNullOrEmpty(Message) && string.IsNullOrEmpty(FileToUpload.Url))
                return;
            await _messageService.SendMessage(SelectedUser.Id, Message);
            Message = String.Empty;
            
            if (FileToUpload != null)
            {
                await _messageService.SendMessage(SelectedUser.Id, HttpUtility.HtmlEncode(FileToUpload.Url));
                FileToUpload = null;
            }
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
