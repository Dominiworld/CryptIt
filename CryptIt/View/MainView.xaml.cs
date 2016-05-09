using System.Windows;
using CryptIt.ViewModel;

namespace CryptIt.View
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            var model = new MainViewModel();
            DataContext = model;
            model.ClosingRequest += (sender, args) => Close();
        }
    }
}
