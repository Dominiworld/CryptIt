using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CryptingTool;
using CryptIt.Commands;
using Model.Files;

namespace CryptIt.ViewModel
{
    public class DownloadViewModel : ViewModelBase
    {
        private readonly CryptTool _cryptTool = CryptTool.Instance;

        public DownloadViewModel()
        {
            Files = new ObservableCollection<Attachment>();
            Files.CollectionChanged += DownloadFile;
            OpenFolderCommand = new DelegateCommand<Attachment>(OpenFolder);

        }
        public ObservableCollection<Attachment> Files { get; set; }

        public DelegateCommand<Attachment> OpenFolderCommand { get; set; }

        private void OpenFolder(Attachment file)
        {
            var fullName = file.Path+"\\"+ file.File.FileName;
            Process.Start("explorer.exe","/select," + fullName);
        }
        private async void DownloadFile(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems==null)
            {
                return;
            }
            var files = e.NewItems.Cast<Attachment>().ToList();
            foreach (var file in files)
            {                       
                await DownloadFile(file);                    
            }
        }

        public async Task DownloadFile(Attachment file)
        {
            using (var client = new WebClient())
            {
                var fileName = file.File.FileName;
                
                client.DownloadProgressChanged += (o, e) =>
                {
                    file.Progress = e.ProgressPercentage;
                };
                client.DownloadFileCompleted += (o, e) =>
                {
                    if (file.IsEncrypted)
                    {
                        _cryptTool.DecryptFile(file.Path + "\\" + fileName,
                            file.Path + "\\" + file.File.FileName,
                                             file.EncryptedSymmetricKey);
                    }
                    file.IsNotCompleted = false;
                };
                await client.DownloadFileTaskAsync(file.File.Url, file.Path + "\\" + fileName);

            }
        }

    }
}
