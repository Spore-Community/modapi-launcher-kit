using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ModApi.InterimSetup
{
    public class ThicknessToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness inThickness = (Thickness)value;
            return new CornerRadius(inThickness.Left, inThickness.Top, inThickness.Right, inThickness.Bottom);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CornerRadius inRadius = (CornerRadius)value;
            return new Thickness(inRadius.TopLeft, inRadius.TopRight, inRadius.BottomRight, inRadius.BottomLeft);
        }
    }
}
