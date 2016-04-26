using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Navigation;
using CryptingTool;
using CryptIt.Commands;
using CryptIt.View;
using Model;
using Model.Files;
using vkAPI;
using ListView = System.Windows.Controls.ListView;
using Message = Model.Message;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace CryptIt.ViewModel
{
    public class MainViewModel:ViewModelBase
    {

        #region private variables

        private readonly LongPollServerService _longPollServer = new LongPollServerService();
        private readonly MessageService _messageService = new MessageService();
        private readonly UserService _userService = new UserService();
        private readonly FileService _fileService = new FileService();
        private User _selectedUser;
        private List<Message> _messages;
        private List<User> _friends;
        private Message _message;
        private List<User> _foundFriends;
        private string _searchString;
        private string _keysFileName = "keys.txt";
        private readonly string _keysFolderNameInAppSetting = "key_folder";

        private readonly CryptTool _cryptTool = CryptTool.Instance;

        private delegate void CancelFileUploadDelegate(Attachment attachment);

        private CancelFileUploadDelegate _cancelFileUploadEvent;
        private bool _isConnectionFailed;
        private bool _isMessageSending;
        private bool _isMessageLoaderVisible;

        private bool ScrollToEnd { get; set; }
        private DownloadView DownloadView { get; set; }


        #endregion private variables

        public MainViewModel()
        {          
            _longPollServer.GotNewMessageEvent += AddMessages;
            _longPollServer.MessageStateChangedToReadEvent += ChangeMessagesStateToRead;
            _longPollServer.UserBecameOnlineOrOfflineEvent += ChangeUserOnlineStatus;
            _longPollServer.ConnectToLongPollServer();

            Message  = new Message();
            DownloadView = new DownloadView();
            DownloadView.Closed += (sender, args) =>
            {
                DownloadView = new DownloadView();
            };

            SetFriendKeyCommand = new DelegateCommand<bool>(SetFriendKey);
            SendMessageCommand = new DelegateCommand(SendMessage);
            UploadFileCommand = new DelegateCommand(OpenFileDialog);
            DownloadMessagesCommand = new DelegateCommand<ScrollChangedEventArgs>(DownloadMessages);
            DownloadFileCommand = new DelegateCommand<Attachment>(DownloadFile);
            CancelFileUploadCommand = new DelegateCommand<Attachment>(CancelFileUpload);
            OpenDialogCommand = new DelegateCommand<User>(OpenDialog);
            GetStartInfo();

            SaveKeysFileDialog();
        }

        #region commands
        public DelegateCommand<User> OpenDialogCommand { get; set; }
        public DelegateCommand<ScrollChangedEventArgs> DownloadMessagesCommand { get; set; }
        public DelegateCommand UploadFileCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand<bool> SetFriendKeyCommand { get; set; }
        public DelegateCommand<Attachment> DownloadFileCommand { get; set; }
        public DelegateCommand<Attachment> CancelFileUploadCommand { get; set; }
        #endregion commands

        #region public properties
        public bool IsConnectionFailed
        {
            get { return _isConnectionFailed; }
            set
            {
                _isConnectionFailed = value;
                if (!_isConnectionFailed)
                {
                    OpenDialog(SelectedUser);
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSendButtonEnabled));
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
        public int UploadingFilesAmount { get; set; }
        public bool IsMessageSending
        {
            get { return _isMessageSending; }
            set
            {
                _isMessageSending = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSendButtonEnabled));
            }
        }
        public bool IsSendButtonEnabled
        {
            get
            {
                return SelectedUser != null && SelectedUser.KeyExists && !IsMessageSending && !IsConnectionFailed;
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
        public bool IsMessageLoaderVisible
        {
            get { return _isMessageLoaderVisible; }
            set
            {
                _isMessageLoaderVisible = value;
                OnPropertyChanged();
            }
        }
        public User SelectedUser
        {
            get
            {
                return _selectedUser;
            }
            set
            {
                if (value == _selectedUser)
                {
                    return;
                }
                _selectedUser = value;
                GetKey(_keysFileName);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSendButtonEnabled));

            }
        }
        #endregion public properties

        #region key functions

        //создание(true)/считывание(false) ключей
        private bool SetKeys(string myPublicKeyFileName, string myPrivateKeyFileName, string path)
        {
            var myId = AuthorizeService.Instance.CurrentUserId;
            if (File.Exists(path + "\\" + myPublicKeyFileName) && File.Exists(path + "\\" + myPrivateKeyFileName))
            {

                var reader = new StreamReader(path + "\\" + myPrivateKeyFileName);
                var line = reader.ReadToEnd();
                if (line != string.Empty)
                {
                    _cryptTool.keyRSAPrivate = Encoding.Default.GetBytes(line.FromBase64());
                    reader = new StreamReader(path + "\\" + myPublicKeyFileName);
                    line = reader.ReadToEnd();
                    var data = line.Split(' ');
                    if (line != string.Empty && data.Length == 2)
                    {
                        int id;
                        if (int.TryParse(data[0], out id) && id == myId)
                        {
                            _cryptTool.keyRSAPublic = Encoding.Default.GetBytes(data[1].FromBase64());
                            reader.Dispose();
                            return false;
                        }
                    }
                }
                reader.Dispose();
            }

            _cryptTool.CreateRSAKey();
            var writer = new StreamWriter(path + "\\"+myPublicKeyFileName);
            writer.Write(myId + " " + Encoding.Default.GetString(_cryptTool.keyRSAPublic).ToBase64());
            writer.Close();
            writer = new StreamWriter(path + "\\" + myPrivateKeyFileName);
            writer.Write(Encoding.Default.GetString(_cryptTool.keyRSAPrivate).ToBase64());
            writer.Close();
            return true;
        }
        //выбор папки сохранения сгенерированных приватного и публичного ключей
        private void SaveKeysFileDialog()
        {
           
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = config.AppSettings.Settings;
            if (settings[_keysFolderNameInAppSetting] == null || settings[_keysFolderNameInAppSetting].Value == "")
            {
                MessageBox.Show(
               "Не определен путь к файлам с ключами. Выберите папку, в которой лежат ключи или где хотите сохранить новые ключи, если они отсутствуют");

                var dialog = new FolderBrowserDialog { ShowNewFolderButton = true };
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                if (settings[_keysFolderNameInAppSetting] != null)
                {
                    settings[_keysFolderNameInAppSetting].Value = dialog.SelectedPath;
                }
                else
                {
                    settings.Add(_keysFolderNameInAppSetting, dialog.SelectedPath);
                }          
                config.Save(ConfigurationSaveMode.Modified);
            }
           
            if (SetKeys("public_key.txt", "private_key.txt", settings[_keysFolderNameInAppSetting].Value))
            {
                MessageBox.Show("Создана новая пара ключей. Пожалуйста, передайте свой публичный ключ (public_key.txt) собеседникам.");
            }
           
        } 
        //считываение публичного ключа друга из файла keys.txt
        private void GetKey(string fileName)
        {
            fileName = ConfigurationManager.AppSettings[_keysFolderNameInAppSetting] + "\\" + fileName;

            if (SelectedUser == null)
                return;

            if (File.Exists(fileName))
            {
                using (var reader = new StreamReader(fileName))
                {
                    var keyFound = false;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var data = line.Split(' ');
                        int id;
                        if (int.TryParse(data[0], out id) && id == SelectedUser.Id && data.Length == 2)
                        {
                            _cryptTool.SetRSAKey(data[1].FromBase64());
                            keyFound = true;
                            break;
                        }
                    }
                    SelectedUser.KeyExists = keyFound;
                }
            }
        } 
        //загрузка публичного ключа друга в диалоге (нужно для отправки сообщений)
        private void SetFriendKey(bool changeExisting)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() != true)
                return;

            var fileName = dialog.FileName;
            string key;
            using (var reader = new StreamReader(fileName))
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    MessageBox.Show("Выбран неверный файл или файл поврежден!");
                    return;
                }
                var data = line.Split(' ');
                int id;
                if (!(int.TryParse(data[0], out id) && data.Length == 2 && SelectedUser.Id == id))
                {
                    MessageBox.Show("Выбран неверный файл или файл поврежден!");
                    return;
                }
                SelectedUser.KeyExists = true;
                key = data[1];
                _cryptTool.SetRSAKey(key.FromBase64());
            }
            OnPropertyChanged(nameof(IsSendButtonEnabled));

            var keysFileName = ConfigurationManager.AppSettings[_keysFolderNameInAppSetting] + "\\" + _keysFileName;
            if (changeExisting)
            {
                var lines = new List<string>();

                using (var reader = new StreamReader(keysFileName))
                {
                    string line;
                    while ((line = reader.ReadLine())!=null)
                    {
                        lines.Add(line);
                    }
                    var usersLine = lines.FirstOrDefault(l => l.StartsWith(SelectedUser.Id.ToString()));
                    lines.Remove(usersLine);
                }
                using (var writer = new StreamWriter(keysFileName,false))
                {
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                    writer.Close();
                }
            }

            using (var writer = new StreamWriter(keysFileName, true))
            {
                writer.WriteLine(SelectedUser.Id + " " + key);
                writer.Close();
            }
        }

        #endregion key functions

        #region functions for files
        private async void OpenFileDialog()
        {
            if (SelectedUser == null)
            {
                return;
            }
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                if (Message.Attachments == null)
                {
                    Message.Attachments = new ObservableCollection<Attachment>();
                }
                //todo multiselect
                var pathItems = dialog.FileName.Split('\\');
                var attachment = new Attachment
                {
                    Document = new Document { FullName = pathItems.Last() },
                    Type = "doc",
                    Path = string.Join("//", pathItems.Take(pathItems.Length - 2)) //отрезали имя файла
                };
                Message.Attachments.Add(attachment);
                OnPropertyChanged("Message");

                UploadingFilesAmount++;

                var key = _cryptTool.EncryptFile(dialog.FileName, "crypt.crypt" + UploadingFilesAmount);

                var uploadedFile = await UploadFile("crypt.crypt" + UploadingFilesAmount, SelectedUser.Id, attachment);
                UploadingFilesAmount--;
                if (uploadedFile == null)
                {
                    Message.Attachments.Remove(attachment);
                    return;
                }

                attachment.Document.Id = uploadedFile.Id;
                attachment.Document.OwnerId = uploadedFile.OwnerId;
                attachment.Document.Url = uploadedFile.Url;
                attachment.Document.FileName = uploadedFile.FileName;
                attachment.EncryptedSymmetricKey = key;

            }
        }
        private async Task<Document> UploadFile(string fileName, int userId, Attachment attachment)
        {

            using (var client = new WebClient())
            {
                var url = await _fileService.GetUploadUrl(fileName, userId);
                client.UploadProgressChanged += (sender, args) =>
                {
                    attachment.Progress = 100 * (float)args.BytesSent / args.TotalBytesToSend;
                };
                client.UploadFileCompleted += (sender, args) =>
                {

                    attachment.IsNotCompleted = false;
                };

                _cancelFileUploadEvent += attachment1 =>
                {
                    if (attachment.File == attachment1.File)
                    {
                        client.CancelAsync();
                        client.Dispose();
                    }
                };

                byte[] file = null;
                try
                {
                    file = await client.UploadFileTaskAsync(url, fileName);
                }
                catch (WebException)
                {
                    return null;
                }
                return await _fileService.UploadFile(fileName, file);
            }



        }
        private void DownloadFile(Attachment attachment)
        {
            var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var dc = DownloadView.DataContext as DownloadViewModel;
                attachment.Path = dialog.SelectedPath;
                dc?.Files.Add(attachment);

                DownloadView.Show();
            }
        }
        private void CancelFileUpload(Attachment attachment)
        {
            if (attachment.IsNotCompleted)
            {
                _cancelFileUploadEvent.Invoke(attachment);
            }

            Message.Attachments.Remove(attachment);
            OnPropertyChanged("Message");
        }
        private void TakeFileNamesFromBody(Message message)
        {
            if (message.Attachments != null && message.Attachments.Any())
            {
                //парсим имена файлов (текст#имя_файла1,имя_файла2)
                var probablyFiles = message.Body.Split('#').Last();
                var cryptedfileNamesWithKeys = probablyFiles.Split(',').ToList();
                //todo условие может поломаться!!!
                if (!message.Body.Contains('#') || cryptedfileNamesWithKeys.Count != message.Attachments.Count ||
                    (string.IsNullOrEmpty(cryptedfileNamesWithKeys[0]) && cryptedfileNamesWithKeys.Count == 1))
                    //сообщение не шифрованное или ошибка
                    return;
                message.Body = message.Body.Substring(0, message.Body.Length - probablyFiles.Length - 1);
                foreach (var attachment in message.Attachments) //восстанавливаем имена зашифрованных из message.body
                {
                    var items = cryptedfileNamesWithKeys[message.Attachments.IndexOf(attachment)].Split(':');
                    attachment.Document.FileName = items[0];
                    attachment.IsEncrypted = true;
                    attachment.EncryptedSymmetricKey = items[1];
                }
            }
        }

        #endregion functions for files

        #region functions for messages
        private async void SendMessage()
        {
            IsMessageSending = true;
            if (Message.Attachments != null && Message.Attachments.Any(a => a.IsNotCompleted))
            {
                var errorDialog = MessageBox.Show("Подождите окончания загрузки");
                return;
            }
            //добавляем полные имена файлов для расшифровки (#имя:ключ,имя:ключ)
            if (Message.Attachments != null && Message.Attachments.Any())
            {
                Message.Body += '#' + string.Join(",", Message.Attachments.Select(a => a.Document.FullName + ":" + a.EncryptedSymmetricKey).ToList());
            }

            try
            {
                if (string.IsNullOrEmpty(Message.Body) && (Message.Attachments == null || !Message.Attachments.Any()))
                    return;
                if (!string.IsNullOrEmpty(Message.Body))
                {
                    var simpleMessage = new Message
                    {
                        Body = Message.Body,
                        UserId = SelectedUser.Id,
                        Out = true,
                        Attachments = Message.Attachments

                    };
                    var cryptedMessage = _cryptTool.MakingEnvelope(Message.Body);
                    Message.Body = cryptedMessage;
                    await _messageService.SendMessage(SelectedUser.Id, Message);
                    Message = new Message();
                    AddMessages(simpleMessage);

                }
            }
            catch (WebException)
            {
                ShowWebErrorMessage();
            }
            finally
            {
                IsMessageSending = false;
            }
        }

        private async Task GetMessages(User user)
        {
            var previousSelected = SelectedUser;
            try
            {
                SelectedUser = user;
                var messages = (await _messageService.GetDialog(SelectedUser.Id)).OrderBy(m => m.Date).ToList();
                foreach (var message in messages.ToArray())
                {
                    message.Body = _cryptTool.SplitAndUnpackReceivedMessage(message.Body);
                    TakeFileNamesFromBody(message);
                    if (message.Attachments != null &&
                        message.Attachments.Where(a => a.File == null).ToList().Count != 0)
                    {
                        message.HasUndefinedContent = true;
                        message.Attachments =
                            new ObservableCollection<Attachment>(message.Attachments.Where(a => a.File != null));
                    }
                }

                if (messages.Count > 0 && messages[0].UserId != SelectedUser.Id)
                {
                    var inMessage = messages.FirstOrDefault(m => !m.Out);
                    SelectedUser = inMessage != null ? inMessage.User : await _userService.GetUser(SelectedUser.Id);
                }

                Messages = messages;

            }
            catch (NullReferenceException ex)
            {
                SelectedUser = previousSelected;
                return;
            }
            catch (WebException ex)
            {
                Messages = null;
                ShowWebErrorMessage();
                return;
            }
            finally
            {
                ScrollToEnd = true;
            }
            SelectedUser.NumberOfNewMessages = null;
            if (Messages.Count != 0 && !Messages[0].Out)
            {
                _messageService.MarkMessagesAsRead(new List<int> { Messages[0].Id }, SelectedUser.Id);
            }
        }

        private async void OpenDialog(User user)
        {
            IsMessageLoaderVisible = true;
            await GetMessages(user);
            IsMessageLoaderVisible = false;
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

        private async void DownloadMessages(ScrollChangedEventArgs e)
        {
            if (SelectedUser == null) return;


            if (e.VerticalOffset == 0 && e.VerticalChange < 0)
            {
                try
                {
                    var nextMessages = (await _messageService.GetDialog(SelectedUser.Id, Messages.Count)).OrderBy(m => m.Date).ToList();
                    //todo сделать лучше биндинг
                    var messages = new List<Message>();
                    messages.AddRange(nextMessages);
                    messages.AddRange(Messages);

                    var vizibleCount = ((ScrollViewer)e.OriginalSource).ViewportHeight;

                    var listView = ((ScrollViewer)e.Source).TemplatedParent as ListView;
                    if (listView != null)
                    {
                        var currentIndex = Messages.IndexOf((Message)listView.Items.CurrentItem);
                        var current = Messages[currentIndex + (int)vizibleCount - 1];
                        Messages = messages;

                        listView.ScrollIntoView(current);
                    }

                }
                catch (Exception)
                {
                    // ignored
                }
            }


            if (ScrollToEnd)
            {
                ((ScrollViewer)e.OriginalSource).ScrollToBottom();
                ScrollToEnd = false;
            }
        }
        private void AddMessages(Message message)
        {
            if (SelectedUser == null ||
                (message.Out && message.Body.StartsWith(_cryptTool._isCryptedFlag))) //не выводим свое отправленное зашифрованное сообщение - незачем
                return;

            var decryptedMessage = _cryptTool.SplitAndUnpackReceivedMessage(message.Body);
            message.Body = decryptedMessage;
            TakeFileNamesFromBody(message);
            if (message.Attachments != null && message.Attachments.Where(a => a.File == null).ToList().Count != 0)
            {
                message.HasUndefinedContent = true;
                message.Attachments = new ObservableCollection<Attachment>(message.Attachments.Where(a => a.File != null));
            }

            if (message.UserId != SelectedUser.Id && !message.Out)
            {

                var friend = FoundFriends.FirstOrDefault(f => f.Id == message.UserId);
                if (friend != null)
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
            var messages = new List<Message>();
            messages.AddRange(Messages);
            messages.Add(message);
            Messages = messages;
            ScrollToEnd = true;

        }
        #endregion functions for messages

        #region functions for users

        private void SearchFriends()
        {
            FoundFriends = string.IsNullOrEmpty(SearchString) ? 
                Friends :
                Friends.Where(f => f.FullName.ToLower().Contains(SearchString.ToLower())).ToList();
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

        #endregion functions for users

        #region functions for web errors
        private async void ShowWebErrorMessage()
        {
            IsConnectionFailed = true;

            while (IsConnectionFailed)
            {
                await Task.Run(() => PingServer());
            }
        }

        private void PingServer()
        {
            try
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send("vk.com");

                if (reply != null && reply.Status == IPStatus.Success)
                {
                    IsConnectionFailed = false;

                }
            }
            catch (Exception)
            {
                return;
            }
        }
        #endregion functions for web errors

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
    }
}
