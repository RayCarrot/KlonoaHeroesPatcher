using System;
using System.Globalization;

namespace KlonoaHeroesPatcher;

public class InvertedBooleanConverter : BaseValueConverter<InvertedBooleanConverter>
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !(bool)value;

    public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => !(bool)value;
}