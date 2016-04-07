using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Navigation;
using CryptingTool;
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
        private List<User> _foundFriends;
        private string _searchString;
        private string _errorMessage;

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
            DownloadMessagesCommand = new DelegateCommand<ScrollChangedEventArgs>(DownloadMessages);
            GetStartInfo();
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        private async void GetDialogsInfo()
        {
            try
            {
                var unreadDialogs = await _messageService.GetDialogs(true);
                foreach (var dialog in unreadDialogs)
                {
                    var friend = FoundFriends.FirstOrDefault(f => f.Id == dialog.Message.UserId);
                    if (friend != null)
                    {
                        friend.NumberOfNewMessages = dialog.UnreadMessagesAmount;
                        OnPropertyChanged("FoundFriends");
                    }
                }
            }
            catch (WebException)
            {
                
            }

        }

        public DelegateCommand<ScrollChangedEventArgs> DownloadMessagesCommand { get; set; }

        private async void DownloadMessages(ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.ExtentHeight - e.ViewportHeight - e.VerticalOffset) > 0.05) return;
            if (SelectedUser == null) return;
            try
            {
                var nextMessages = (await _messageService.GetDialog(SelectedUser.Id, Messages.Count)).ToList();
                //todo сделать лучше биндинг
                var messages = new List<Message>();
                messages.AddRange(Messages);
                messages.AddRange(nextMessages);
                Messages = messages;
            }
            catch (Exception)
            {
                // ignored
            }
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

        public List<User> FoundFriends
        {
            get
            {
                return _foundFriends;
            }
            set
            {
                _foundFriends = value;
                OnPropertyChanged();
            }
        }

        private void SearchFriends()
        {
            FoundFriends = string.IsNullOrEmpty(SearchString) ? 
                Friends :
                Friends.Where(f => f.FullName.ToLower().Contains(SearchString.ToLower())).ToList();
        }

        public string SearchString
        {
            get { return _searchString; }
            set
            {
                _searchString = value;
                SearchFriends();
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
            if (friend != null) friend.Online = online? 1 : 0;
        }

        private void ChangeMessagesStateToRead(int lastReadId, int peerId)
        {
            if (SelectedUser?.Id == peerId)
            {
                foreach (var message in Messages.ToArray().Where(m=>m.Id <= lastReadId))
                {
                    message.IsNotRead = false;                   
                }
                OnPropertyChanged("Messages");
            }
        }

        private async void AddMessages(Message message)
        {

            if (SelectedUser == null)
                return;

            var decryptedMessage = SignAndData.SplitAndUnpackReceivedMessage(message.Body);
            message.Body = decryptedMessage;
            if (message.UserId!= SelectedUser.Id && !message.Out)
            {
               
                var friend = FoundFriends.FirstOrDefault(f => f.Id == message.UserId);
                if (friend!=null )
                {
                    if (friend.NumberOfNewMessages == null)
                    {
                        friend.NumberOfNewMessages = 1;
                    }
                    else
                    {
                        friend.NumberOfNewMessages++;
                    }
                }
                OnPropertyChanged("FoundFriends");
            }
            if (message.UserId != SelectedUser.Id) return;
            message.User = message.Out ? AuthorizeService.Instance.CurrentUser : SelectedUser;
          
            if (!message.Out)
            {
                try
                {
                    message.IsNotRead = false;
                    _messageService.MarkMessagesAsRead(new List<int> { message.Id }, SelectedUser.Id);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            //todo переделать биндинг
            var messages = new List<Message>
            {
                message
            };
            messages.AddRange(Messages);
            Messages = messages;

        }

        public DelegateCommand SendMessageCommand { get; set; }
        private async void GetStartInfo()
        {
            try
            {
                Friends = (await _userService.GetFriends(AuthorizeService.Instance.CurrentUserId)).ToList();
            }
            catch (WebException)
            {
                //todo вывод ошибки
                return;
            }
            Friends = Friends.OrderBy(f => f.LastName).ToList();
            FoundFriends = Friends;
            GetDialogsInfo();
        }


        private async void SendMessage()
        {

            if (IsFileUploading)
            {
                var errorDialog = MessageBox.Show("Подождите окончания загрузки");               
                return;               
            }
            try
            {
                //todo шифровка Message
                              
                if (string.IsNullOrEmpty(Message) && FileToUpload == null)
                return;

                var cryptedMessage = SignAndData.MakingEnvelope(Message);

                await _messageService.SendMessage(SelectedUser.Id, cryptedMessage);
                Message = string.Empty;

                if (FileToUpload != null && !string.IsNullOrEmpty(FileToUpload.Url))
                {
                    await _messageService.SendMessage(SelectedUser.Id, FileToUpload.Url);
                    FileToUpload = null;
                }
            }
            catch (WebException)
            {
                ShowWebErrorMessage();
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
            }
        }

        public User SelectedUser
        {
            get {
                return _selectedUser; }
            set
            {
                _selectedUser = value;
                GetMessages();
                OnPropertyChanged();
            }
        }

        private async void GetMessages()
        {
            if (SelectedUser == null)
                return;
            try
            {
                Messages = (await _messageService.GetDialog(SelectedUser.Id)).ToList();
            }
            catch (NullReferenceException ex)
            {
                return;
            }
            catch (WebException ex)
            {
                Messages = null;
                ShowWebErrorMessage();
                return;
            }
            SelectedUser.NumberOfNewMessages = null;
            if (Messages.Count!=0 && !Messages[0].Out)
            {
               _messageService.MarkMessagesAsRead(new List<int> {Messages[0].Id}, SelectedUser.Id );
            }
        }

        private void ShowWebErrorMessage()
        {
           // MessageBox.Show("Потеряно соединение с сервером", "Ошибка");
            ErrorMessage = "Потеряно соединение с сервером";
        }

    }
}
