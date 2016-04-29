using System.Web;
using System.Windows;
using System.Windows.Navigation;
using CryptIt.ViewModel;
using vkAPI;

namespace CryptIt.View
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class BrowserView : Window
    {
        private LogInViewModel _model = new LogInViewModel();

        public BrowserView()
        {
            InitializeComponent();
            
            DataContext = _model;
            _model.ClosingRequest += (sender, args) => this.Close();
        }

        private void WebBrowser_OnNavigated(object sender, NavigationEventArgs e)
        {
            _model.IsBrowserLoading = false;
            
            var urlParams = HttpUtility.ParseQueryString(e.Uri.Fragment.Substring(1));
            AuthorizeService.Instance.AccessToken = urlParams.Get("access_token");
            AuthorizeService.Instance.CurrentUserId = int.Parse(urlParams.Get("user_id"));
            AuthorizeService.Instance.GetCurrentUser();          
            var window = new MainView();
            window.Show();
            Close();           
        }


        private void WebBrowser_OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            _model.IsBrowserLoading = true;
        }

    }
}
