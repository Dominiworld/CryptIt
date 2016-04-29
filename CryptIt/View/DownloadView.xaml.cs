using System.Windows;
using CryptIt.ViewModel;

namespace CryptIt.View
{
    /// <summary>
    /// Interaction logic for DownloadView.xaml
    /// </summary>
    public partial class DownloadView : Window
    {
        public DownloadView()
        {
            InitializeComponent();
            DataContext = new DownloadViewModel();
        }

        
    }
}
