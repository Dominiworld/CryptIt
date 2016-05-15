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
            var model = new DownloadViewModel();
            DataContext = model;
            Closed += (sender, args) =>
            {
                model.CancelDownload();
            };
        }

        
    }
}
