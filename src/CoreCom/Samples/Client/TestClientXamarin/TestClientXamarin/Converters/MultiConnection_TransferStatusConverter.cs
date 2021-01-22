using System;
using System.Globalization;
using WallTec.CoreCom.Models;
using Xamarin.Forms;

namespace TestClientXamarin.Converters
{
    public class MultiConnection_TransferStatusConverter : IMultiValueConverter
    {
        public MultiConnection_TransferStatusConverter()
        {
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is ConnectionStatusEnum)
                {
                    return value.ToString();
                    // Alternatively, return BindableProperty.UnsetValue to use the binding FallbackValue
                }
                else if (value is TransferStatusEnum)
                {
                    return value.ToString(); 
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
