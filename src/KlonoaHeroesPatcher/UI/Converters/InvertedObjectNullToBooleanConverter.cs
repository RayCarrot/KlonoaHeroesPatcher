using System;
using System.Globalization;

namespace KlonoaHeroesPatcher;

public class InvertedObjectNullToBooleanConverter : BaseValueConverter<InvertedObjectNullToBooleanConverter>
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value != null;
}