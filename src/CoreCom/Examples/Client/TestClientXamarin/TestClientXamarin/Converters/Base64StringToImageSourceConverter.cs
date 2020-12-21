using System;
using System.Globalization;
using System.IO;
using Xamarin.Forms;

namespace TestClientXamarin.Converters
{
    public class Base64StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                byte[] fileBytes = System.Convert.FromBase64String(value as string);
                return ImageSource.FromStream(() => new MemoryStream(fileBytes));
            }
            return null;
            //using (MemoryStream ms = new MemoryStream(fileBytes, 0, fileBytes.Length))
            //{
            //    ms.Write(fileBytes, 0, fileBytes.Length);
            //    return ImageSource.FromStream(() => new MemoryStream(fileBytes));
            //    //BitmapImage bitmapImage = new BitmapImage();
            //    //bitmapImage.SetSource(ms);
            //    //return bitmapImage;
            //}
            //return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }


    }
}
