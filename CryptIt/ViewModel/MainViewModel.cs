using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;
using System.Windows.Navigation;
using CryptIt.Commands;
using vkAPI;



namespace CryptIt.ViewModel
{
    public class MainViewModel:ViewModelBase
    {
        private string _authorizeUrl;
        private MessageService _messageService;
        private UserService _userService;

        public MainViewModel()
        {
            _messageService = new MessageService();
            _userService = new UserService();
            
            
            AuthorizeUrl = AuthorizeService.Instance.GetAuthorizeUrl(5296011);
            GetDialogsCommand = new DelegateCommand(GetDialogs); 
        }

        private void GetDialogs()
        {
           var dialogs =  _messageService.GetDialogs();
            var messages = _messageService.GetMessages(1);
            var friends = _userService.GetFriends(int.Parse(AuthorizeService.Instance.CurrentUserId));
        }

        public DelegateCommand GetDialogsCommand { get; set; }


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
