using System.Windows;
using CryptIt.ViewModel;

namespace CryptIt.View
{
    /// <summary>
    /// Interaction logic for LogInView.xaml
    /// </summary>
    public partial class LogInView: Window
    {

        public LogInView()
        {
            var model = new LogInViewModel();
            DataContext = model;
            InitializeComponent();
            model.ClosingRequest += (sender, args) => this.Close();

        }

        private void Button_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }
    }
}
