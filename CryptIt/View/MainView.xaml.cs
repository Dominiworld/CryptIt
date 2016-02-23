using System;
using System.Web;
using System.Windows;
using System.Windows.Navigation;
using CryptIt.ViewModel;
using vkAPI;

namespace CryptIt.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView
    {

        public MainView()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void Authorize(object sender, RoutedEventArgs e)
        {
            var model = DataContext as MainViewModel;
            if (model!=null)
            {
                WebBrowser.Navigate(new Uri(model.AuthorizeUrl));
            }         
        }

        private void WebBrowser_OnNavigated(object sender, NavigationEventArgs e)
        {          
            var urlParams = HttpUtility.ParseQueryString(e.Uri.Fragment.Substring(1));
            AuthorizeService.Instance.AccessToken = urlParams.Get("access_token");
            AuthorizeService.Instance.CurrentUserId = urlParams.Get("user_id");       
        }
    }
}
