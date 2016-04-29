using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Model;

namespace CryptIt.Converters
{
    public class IsUserSelectedToVisibilityConverter:IValueConverter
    {
       public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
       {
           var user = value as User;
           if (user == null)
               return Visibility.Collapsed;
           return Visibility.Visible;
       }

       public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
       {
           throw new NotImplementedException();
       }
    }
}
