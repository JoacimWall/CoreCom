using System;
using System.Globalization;
using WallTec.CoreCom.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TestClientXamarin.Converters
{
    
    public class TransferStatusEnumConverter :  IValueConverter
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
