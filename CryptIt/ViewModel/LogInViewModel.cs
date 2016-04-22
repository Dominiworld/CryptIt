using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;
using System.Windows.Navigation;
using CryptIt.Commands;
using CryptIt.View;
using vkAPI;
using System.Runtime.InteropServices;

namespace CryptIt.ViewModel
{
    public class LogInViewModel:ViewModelBase
    {

   
        private string _authorizeUrl;
        private MessageService _messageService;
        private UserService _userService;



        public LogInViewModel()
        {
            _messageService = new MessageService();
            _userService = new UserService();
            
            
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
                catch (Exception ex)
                {
                }
            }
        }


    }
}
