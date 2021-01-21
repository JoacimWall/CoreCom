using System;
using System.Globalization;
using WallTec.CoreCom.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TestClientXamarin.Converters
{
    
    public class MessageSizeToStringConverter :  IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = (int)value;
            if (b < 1000)
               return string.Format("{0:0.##} byts", b.ToString());


            var kb = b / 1000;

            return string.Format("{0:0.##} kbyts", kb.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        
    }
}
