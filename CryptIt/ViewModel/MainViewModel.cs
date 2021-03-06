﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using CryptingTool;
using CryptIt.Commands;
using CryptIt.View;
using GalaSoft.MvvmLight.CommandWpf;
using Model;
using Model.Files;
using vkAPI;
using ListView = System.Windows.Controls.ListView;
using Message = Model.Message;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace CryptIt.ViewModel
{
    public class MainViewModel : ViewModelBase
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
        private string _publicFileName = $"_public.txt";
        private string _keysFileName = "keys.txt";
        private readonly string _keysFolderNameInAppSetting = "key_folder";
        private readonly string _requestKeyString = "Key request";
        private string _keysPath;

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

            Message = new Message();
            RenewDownloadView();

            SendMessageCommand = new DelegateCommand(SendMessage);
            UploadFileCommand = new DelegateCommand(OpenFileDialog);
            DownloadMessagesCommand = new DelegateCommand<ScrollChangedEventArgs>(DownloadMessages);
            DownloadFileCommand = new DelegateCommand<Attachment>(DownloadFile);
            CancelFileUploadCommand = new DelegateCommand<Attachment>(CancelFileUpload);
            OpenDialogCommand = new DelegateCommand<User>(OpenDialog);
            LogOutCommand = new DelegateCommand(LogOut);
            SendKeyRequestCommand = new DelegateCommand(SendKeyRequest);

            GetStartInfo();
            SaveKeysFileDialog();

        }

        #region commands
        public DelegateCommand<User> OpenDialogCommand { get; set; }
        public DelegateCommand<ScrollChangedEventArgs> DownloadMessagesCommand { get; set; }
        public DelegateCommand UploadFileCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand<Attachment> DownloadFileCommand { get; set; }
        public DelegateCommand<Attachment> CancelFileUploadCommand { get; set; }
        public DelegateCommand LogOutCommand { get; set; }
        public DelegateCommand SendKeyRequestCommand { get; set; }
        

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
                    _cryptTool.keyRSAPrivate = Convert.FromBase64String(line);
                    reader = new StreamReader(path + "\\" + myPublicKeyFileName);
                    line = reader.ReadToEnd();
                    var data = line.Split(' ');
                    if (line != string.Empty && data.Length == 2)
                    {
                        int id;
                        if (int.TryParse(data[0], out id) && id == myId)
                        {
                            _cryptTool.keyRSAPublic = Convert.FromBase64String(data[1]);
                            reader.Dispose();
                            return false;
                        }
                    }
                }
                reader.Dispose();
            }

            _cryptTool.CreateRSAKey();

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var settings = config.AppSettings.Settings;
            var confName = "public_key_id";
            var docIdPath = settings[confName];

            if (docIdPath != null)
            {
                settings.Remove(confName);                
                config.Save(ConfigurationSaveMode.Modified);
            }

            var writer = new StreamWriter(path + "\\" + myPublicKeyFileName);
            writer.Write(myId + " " + Convert.ToBase64String(_cryptTool.keyRSAPublic));
            writer.Close();
            writer = new StreamWriter(path + "\\" + myPrivateKeyFileName);
            writer.Write(Convert.ToBase64String(_cryptTool.keyRSAPrivate));
            writer.Close();
            return true;
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
                            _cryptTool.keyRSARemote = Convert.FromBase64String(data[1]);
                            keyFound = true;
                            break;
                        }
                    }
                    SelectedUser.KeyExists = keyFound;
                }
            }
        }

        //запрос ключа друга
        private async void SendKeyRequest()
        {
            var message = new Message { Body = _requestKeyString };
            await _messageService.SendMessage(SelectedUser.Id, message);
        }

        //поиск запроса ключа и ответ на него - вызывать для всех "новых" сообщений
        private async Task<bool> FindKeyRequestAndReply(Message message)
        {
            if (message.Body == _requestKeyString && !message.Out)
            {
                await SendPublicKey(message.UserId, message.Id);
                return true;
            }
            return false;
        }

        //поиск файла с ключом среди сообщений - вызывать для всех "новых" сообщений
        private async Task GetKeyFileFromMessage(Message message)
        {
            if (message.Attachments == null)
                return;
            foreach (var attachment in message.Attachments)
            {
                var fileName = attachment.File?.FileName;
                if (fileName == null)
                    continue;
                if (fileName == message.UserId + _publicFileName)
                {
                    using (var client = new WebClient())
                    {
                        try
                        {
                            await client.DownloadFileTaskAsync(attachment.File.Url, fileName);
                        }
                        catch (WebException)
                        {
                            MessageBox.Show("Ошибка загрузки файла с ключом. Попробуйте зайти в диалог еще раз.");
                            return;
                        }
                        SetFriendKey(SelectedUser.KeyExists, fileName);
                        File.Delete(fileName);
                    }                   
                }
            }
        }

        //ищем в сообщениях запрос ключа и сам ключ
        private async void PraseMessages(List<Message> messages)
        {
            bool keySend = false;
            foreach (var message in messages)
            {
                if (!keySend)
                {
                    keySend = await FindKeyRequestAndReply(message);
                }
                if (!SelectedUser.KeyExists)
                {
                    await GetKeyFileFromMessage(message);
                }
                if (keySend && SelectedUser.KeyExists)
                    break;
            }
            
        }

        //загрузка публичного ключа друга в диалоге (нужно для отправки сообщений)
        private void SetFriendKey(bool changeExisting, string fileName)
        {
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
                _cryptTool.keyRSARemote = Convert.FromBase64String(key);
            }
            OnPropertyChanged(nameof(IsSendButtonEnabled));

            var keysFileName = _keysPath + "\\" + _keysFileName;
            if (changeExisting)
            {
                var lines = new List<string>();

                using (var reader = new StreamReader(keysFileName))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    var usersLine = lines.FirstOrDefault(l => l.StartsWith(SelectedUser.Id.ToString()));
                    lines.Remove(usersLine);
                }
                using (var writer = new StreamWriter(keysFileName, false))
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

        //загрузка своего ключа в документы
        private async Task<Document> SavePublicKeyInVkDocs(string path)
        {
            using (var client = new WebClient())
            {
                var url = await _fileService.GetUploadUrl(path);

                byte[] file = null;
                try
                {
                    file = await client.UploadFileTaskAsync(url, path);
                }
                catch (WebException)
                {
                    return null;
                }
                return await _fileService.UploadFile(path, file);
            }
        }

        //отправить свой ключ другу - автоматом
        private async Task SendPublicKey(int userId, int messageToRemove)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            
            var settings = config.AppSettings.Settings;
            var confName = "public_key_id";
            var docIdPath = settings[confName];

            var id = AuthorizeService.Instance.CurrentUserId;

            Document doc = null;
            if (docIdPath != null && docIdPath.Value!="")
            {
                //берем ключ с документов вк      
                doc = (await _fileService.GetDocumentById(id + "_" + docIdPath.Value));
            }

            if (doc == null)
            {
                //есл в документах нет, загружаем
                if ((doc = await SavePublicKeyInVkDocs(Path.Combine(_keysPath, id + _publicFileName))) == null)
                    return;

                //сохраняем id, чтобы потом файл с вк вытащить
                if (docIdPath!=null)
                {
                    settings.Remove(confName);
                }
                settings.Add(confName, doc.Id.ToString());
                config.Save(ConfigurationSaveMode.Modified);

            }
            var message = new Message
            {
                Attachments = new ObservableCollection<Attachment>
                {
                    new Attachment {Document = doc, Type = "doc"}
                },
                Body = "key"
            };
            await _messageService.SendMessage(userId, message);
            await _messageService.RemoveMessage(messageToRemove);

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

            var id = AuthorizeService.Instance.CurrentUserId;
            if (SetKeys(id+_publicFileName, id+"_private.txt", settings[_keysFolderNameInAppSetting].Value))
            {
                MessageBox.Show("Создана новая пара ключей. Пожалуйста, передайте свой публичный ключ собеседникам.");
            }

            _keysPath = settings[_keysFolderNameInAppSetting].Value;
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

                var fileNameHash = _cryptTool.CreateHash(dialog.FileName)+".txt";
                
                var key = _cryptTool.EncryptFile(dialog.FileName, fileNameHash);
                var uploadedFile = await UploadFile(fileNameHash, SelectedUser.Id, attachment);
                File.Delete(fileNameHash);
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
                var url = await _fileService.GetUploadUrl(fileName);
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
            if (!IsSendButtonEnabled)
            {
                return;
            }
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
                IsMessageSending = true;

                if (string.IsNullOrEmpty(Message.Body) && (Message.Attachments == null || !Message.Attachments.Any()))
                    return;
                if (!string.IsNullOrEmpty(Message.Body))
                {
                    var simpleMessage = new Message
                    {
                        Body = Message.Body,
                        UserId = SelectedUser.Id,
                        Out = true,
                        Attachments = Message.Attachments,
                        UnixTime =(int) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                        IsNotRead = true
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
                var query =(await _messageService.GetDialog(SelectedUser.Id)).ToList();

                PraseMessages(query);

                var messages = query.OrderBy(m => m.Date).ToList();

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

            var sortedUnreadList =
                FoundFriends.Where(f => f.NumberOfNewMessages != null).ToList();
            sortedUnreadList.AddRange(FoundFriends.Where(f => f.NumberOfNewMessages == null).OrderBy(f => f.FullName).ToList());
            FoundFriends = new List<User>(sortedUnreadList);

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
                    if (dialog.Message.ChatId != 0)                   
                        continue;
                    
                    var friend = FoundFriends.FirstOrDefault(f => f.Id == dialog.Message.UserId);
                    if (friend != null)
                    {
                        friend.NumberOfNewMessages = dialog.UnreadMessagesAmount;
                        //UnreadDialogs.Add(friend);
                    }
                }
                var newDialogFriends = FoundFriends.Where(f => f.NumberOfNewMessages != null).ToList();
                var readDialofFriends = FoundFriends.Where(f => f.NumberOfNewMessages == null).ToList();
                newDialogFriends.AddRange(readDialofFriends);
                FoundFriends = newDialogFriends;
                OnPropertyChanged("FoundFriends");
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
                    foreach (var message in nextMessages.ToArray())
                    {
                        message.Body = _cryptTool.SplitAndUnpackReceivedMessage(message.Body);
                    }
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
                catch (Exception ex)
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
        private async void AddMessages(Message message)
        { 
            if (message.Out && message.Body.StartsWith(_cryptTool._isCryptedFlag)) //не выводим свое отправленное зашифрованное сообщение - незачем
                return;

            if ((SelectedUser == null || message.UserId != SelectedUser.Id) && !message.Out)
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
                    //UnreadDialogs.Add(friend);
                    FoundFriends.Remove(friend);
                    FoundFriends.Insert(0, friend);
                    FoundFriends = new List<User>(FoundFriends);
                }
                OnPropertyChanged("FoundFriends");
            }

            if (SelectedUser == null)
                return;


            var decryptedMessage = _cryptTool.SplitAndUnpackReceivedMessage(message.Body);
            message.Body = decryptedMessage;
            TakeFileNamesFromBody(message);
            if (message.Attachments != null && message.Attachments.Where(a => a.File == null).ToList().Count != 0)
            {
                message.HasUndefinedContent = true;
                message.Attachments = new ObservableCollection<Attachment>(message.Attachments.Where(a => a.File != null));
            }


            if (message.UserId != SelectedUser.Id) return;
            message.User = message.Out ? AuthorizeService.Instance.CurrentUser : SelectedUser;

            if (!message.Out)
            {
                try
                {
                    message.IsNotRead = false;
                    _messageService.MarkMessagesAsRead(new List<int> { message.Id }, SelectedUser.Id);
                    await FindKeyRequestAndReply(message);
                    await GetKeyFileFromMessage(message);
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
            if (friend != null) friend.Online = online ? 1 : 0;
        }
        private void ChangeMessagesStateToRead(int lastReadId, int peerId)
        {
            if (SelectedUser?.Id == peerId)
            {
                foreach (var message in Messages.ToArray().Where(m => m.Id <= lastReadId))
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
            Friends.Add(AuthorizeService.Instance.CurrentUser);
            FoundFriends = Friends;
            GetDialogsInfo();
        }

        private void LogOut()
        {
            var window = new BrowserView();
            window.WebBrowser.Navigate(new Uri(AuthorizeService.Instance.GetAuthorizeUrl()));
            window.Show();
            OnClosingRequest();
        }

        private void RenewDownloadView()
        {
            DownloadView = new DownloadView();
            DownloadView.Closed += (sender, args) =>
            {
                RenewDownloadView();
            };
        }    
    }
}
