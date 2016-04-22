﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Model;

namespace CryptIt.Converters
{
    public class IsUserNotSelectedToVisibilityConverter:IValueConverter
    {
       public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
       {
           var user = value as User;
           if (user == null)
               return Visibility.Visible;
           return Visibility.Collapsed;
       }

       public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
       {
           throw new NotImplementedException();
       }
    }
}
