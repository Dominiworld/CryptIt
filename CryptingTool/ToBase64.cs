using System;
using System.Text;

namespace CryptingTool
{
    public static class StringBase64Extension
    {
        public static string ToBase64(this string str)
        {
            try
            {
                var codedStr = Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
                //вк кушает плюсы
                return codedStr.Replace('+', '-');

            }
            catch (Exception)
            {
                return null;
            }
        }
        public static string FromBase64(this string str)
        {
            try
            {
                str = str.Replace('-', '+');
                return Encoding.UTF8.GetString(Convert.FromBase64String(str));
            }
            catch (Exception)
            {
                return null;
            }
            
        }
    }
}
