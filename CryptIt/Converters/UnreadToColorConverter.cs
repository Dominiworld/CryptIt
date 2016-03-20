using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace CryptIt.Converters
{
    public class UnreadToColorConverter:IValueConverter
    {
        public SolidColorBrush UnRead { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isUnread = (bool) value;
            var color = UnRead ?? new SolidColorBrush(Colors.LightBlue);
            if (isUnread)
            {
                return color;
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
