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
        private Message _message;
        private Document _fileToUpload;
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

            Message  = new Message();

            SendMessageCommand = new DelegateCommand(SendMessage);
            UploadFileCommand = new DelegateCommand(OpenFileDialog);
            DownloadMessagesCommand = new DelegateCommand<ScrollChangedEventArgs>(DownloadMessages);
            DownloadFileCommand = new DelegateCommand<Attachment>(DownloadFile);
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

        public DelegateCommand UploadFileCommand { get; set; }

        private async void OpenFileDialog()
        {
            if (SelectedUser==null)
            {
                return;
            }
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog()==true)
            {
                if (Message.Attachments == null)
                {
                    Message.Attachments = new ObservableCollection<Attachment>();
                }
                //todo multiselect
                var pathItems = dialog.FileName.Split('\\');
                var attachment = new Attachment
                {
                    Document = new Document {FullName = pathItems[pathItems.Length-1]},
                    Type = "doc"
                };
                Message.Attachments.Add(attachment);
                OnPropertyChanged("Message");
                IsFileUploading = true;
                SignAndData.EncryptFile(dialog.FileName, "crypt.crypt", "babasahs"); //todo change key
                var uploadedFile = await _fileService.UploadFile("crypt.crypt", SelectedUser.Id);
                attachment.Document.Id = uploadedFile.Id;
                attachment.Document.OwnerId = uploadedFile.OwnerId;
                attachment.Document.Url = uploadedFile.Url;
                attachment.Document.FileName = uploadedFile.FileName;
                IsFileUploading = false;                
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
            TakeFileNamesFromBody(message);

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
            Friends.Add(new User {Id = AuthorizeService.Instance.CurrentUserId, FirstName = "z", LastName = "z"});
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
            //добавляем полные имена файлов для расшифровки (# - для отделения имен от body)
            if (Message.Attachments!=null)
            {
                Message.Body += '#' + string.Join(",", Message.Attachments.Select(a => a.Document.FullName).ToList());
            }

            try
            {           
                if (string.IsNullOrEmpty(Message.Body) && (Message.Attachments == null || !Message.Attachments.Any()))
                return;
                if (!string.IsNullOrEmpty(Message.Body))
                {
                     var cryptedMessage = SignAndData.MakingEnvelope(Message.Body);
                     Message.Body = cryptedMessage;
                     await _messageService.SendMessage(SelectedUser.Id, Message);
                     Message = new Message();

                }
            }
            catch (WebException)
            {
                ShowWebErrorMessage();
            }           
        }

        public Message Message
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
                var messages = (await _messageService.GetDialog(SelectedUser.Id)).ToList();
                foreach (var message in messages.ToArray())
                {
                    message.Body = SignAndData.SplitAndUnpackReceivedMessage(message.Body);
                    TakeFileNamesFromBody(message);
                }
                Messages = messages;
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

        private async void DownloadFile(Attachment attachment)
        {
            var dialog = new FolderBrowserDialog();
           
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var path = dialog.SelectedPath;
                var fileName = "crypt.crypt";
                if (!attachment.IsEncrypted)
                {
                    fileName = attachment.File.FileName;
                }
                await _fileService.DownloadFile(attachment.File.Url, path, fileName);

                if (attachment.IsEncrypted)
                {
                    SignAndData.DecryptFile(path + "\\crypt.crypt", path+"\\"+attachment.File.FileName, "babasahs");
                }
            }                
        }
        public DelegateCommand<Attachment> DownloadFileCommand { get; set; }

        private void TakeFileNamesFromBody(Message message)
        {
            if (message.Attachments != null && message.Attachments.Any())
            {
                //парсим имена файлов (текст#имя_файла1,имя_файла2)
                var probablyFiles = message.Body.Split('#').Last();
                var cryptedfileNames = probablyFiles.Split(',').ToList();
                //todo условие может поломаться!!!
                if (!message.Body.Contains('#') || cryptedfileNames.Count != message.Attachments.Count ||
                    (string.IsNullOrEmpty(cryptedfileNames[0]) && cryptedfileNames.Count == 1))
                    //сообщение не шифрованное или ошибка
                    return;
                message.Body = message.Body.Substring(0, message.Body.Length - probablyFiles.Length - 1);
                foreach (var attachment in message.Attachments) //восстанавливаем имена зашифрованных из message.body
                {
                    attachment.Document.FileName = cryptedfileNames[message.Attachments.IndexOf(attachment)];
                    attachment.IsEncrypted = true;
                }
            }
        }
    }
}
