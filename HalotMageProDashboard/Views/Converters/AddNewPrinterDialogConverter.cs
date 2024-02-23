using System.Globalization;
using System.Windows.Data;

namespace HalotMageProDashboard.Views.Converters {
    [ValueConversion(typeof(bool), typeof(string))]
    public class AddNewPrinterDialogConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (bool)value ? "追加済み" : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new InvalidOperationException();
        }
    }
}
