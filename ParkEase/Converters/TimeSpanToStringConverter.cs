using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			TimeSpan timeSpan = (TimeSpan)value;
			return timeSpan.ToString(@"HH\:mm");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return TimeSpan.Parse((string)value);
		}
	}
}
