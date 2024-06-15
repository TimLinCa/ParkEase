using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.Net.Maui;

namespace ParkEase.Converters
{
    internal class BarcodeDetectionEventConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BarcodeDetectionEventArgs args = value as BarcodeDetectionEventArgs;
            if (args != null && args.Results.Any())
                return $"{args.Results[0].Value}";
            else
                return string.Empty;
        }
            

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 1 : 0;
        }
    }
}
