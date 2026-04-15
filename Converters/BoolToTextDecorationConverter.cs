using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ToDo.Converters;

public class BoolToTextDecorationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? TextDecorations.Strikethrough : new TextDecorationCollection();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}