using System;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CryptIt.ViewModel;
using vkAPI;

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
    }
}
