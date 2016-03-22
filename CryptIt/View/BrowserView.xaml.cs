using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CryptIt.ViewModel;
using vkAPI;

namespace CryptIt.View
{
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class BrowserView : Window
    {
        public BrowserView()
        {
            InitializeComponent();
            var model = new LogInViewModel();
            DataContext = model;
            model.ClosingRequest += (sender, args) => this.Close();
        }

        private void WebBrowser_OnNavigated(object sender, NavigationEventArgs e)
        {
            var urlParams = HttpUtility.ParseQueryString(e.Uri.Fragment.Substring(1));
            AuthorizeService.Instance.AccessToken = urlParams.Get("access_token");
            AuthorizeService.Instance.CurrentUserId =int.Parse(urlParams.Get("user_id"));
            AuthorizeService.Instance.GetCurrentUser();
            var window = new MainView();
            window.Show();
            Close();           
        }

        
    }
}
