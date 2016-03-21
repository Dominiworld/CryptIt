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
            
            
            AuthorizeUrl = AuthorizeService.Instance.GetAuthorizeUrl(5296011);
            AuthorizeCommand = new DelegateCommand(Authorize);
        }

        private void Authorize()
        {
            var window = new BrowserView();
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

   
    }
}
