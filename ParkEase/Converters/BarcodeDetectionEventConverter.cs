using Camera.MAUI.ZXingHelper;
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
            BarcodeEventArgs args = value as BarcodeEventArgs;
            if (args != null && args.Result.Any())
                return $"{args.Result[0].Text}";
            else
                return string.Empty;
        }
            

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 1 : 0;
        }
    }
}
