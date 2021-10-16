﻿using System;
using System.Globalization;
using System.Windows;

namespace KlonoaHeroesPatcher
{
    public class InvertedObjectNullToVisibilityConverter : BaseValueConverter<InvertedObjectNullToVisibilityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value == null ? Visibility.Collapsed : Visibility.Visible;
    }
}