using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CryptIt.Converters
{
    public class InvertBooleanToVisibilityCollapsedConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
