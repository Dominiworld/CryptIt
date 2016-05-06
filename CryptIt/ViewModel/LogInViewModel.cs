using System;
using CryptIt.Commands;
using CryptIt.View;
using vkAPI;

namespace CryptIt.ViewModel
{
    public class LogInViewModel:ViewModelBase
    {

        private string _authorizeUrl;
        private bool _isBrowserLoading;

        public LogInViewModel()
        {
            AuthorizeUrl = AuthorizeService.Instance.GetAuthorizeUrl();
            AuthorizeCommand = new DelegateCommand(Authorize);
        }

        private void Authorize()
        {

            var window = new BrowserView();
            //ClearWebBrowser();
           
            window.WebBrowser.Navigate(new Uri(AuthorizeUrl));
            window.Show();
            OnClosingRequest();
        }

        public bool IsBrowserLoading
        {
            get { return _isBrowserLoading; }
            set
            {
                _isBrowserLoading = value;
                OnPropertyChanged();
            }
        }



        public DelegateCommand AuthorizeCommand { get; set; }

        public string AuthorizeUrl
        {
            get { return _authorizeUrl; }
            set
            {
                _authorizeUrl = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        ///clear webbrowser temp files to log out
        /// </summary>
        private void ClearWebBrowser()
        {
            string[] theCookies = System.IO.Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Cookies));
            foreach (string currentFile in theCookies)
            {
                try
                {
                    System.IO.File.Delete(currentFile);
                }
                catch (Exception )
                {
                }
            }
        }


    }
}
