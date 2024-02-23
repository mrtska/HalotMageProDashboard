using System.Globalization;
using System.Windows.Data;

namespace HalotMageProDashboard.Views.Converters {
    [ValueConversion(typeof(bool), typeof(string))]
    public class ConnectionBooleanConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (bool)value ? "接続済み" : "未接続";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new InvalidOperationException();
        }
    }
}
