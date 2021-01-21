using System;
using System.Globalization;
using Xamarin.Forms;
namespace TestClientXamarin.Converters
{
    
    public class ConnectionStatusConverter :  IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        
    }
}
