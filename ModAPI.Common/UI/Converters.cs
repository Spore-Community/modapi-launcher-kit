using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModAPI.Common.UI
{
    public class MultiplyDivideDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (double)value * (double)parameter;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (double)value / (double)parameter;
        }
    }

    public class PercentageDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            string param = (string)parameter;
            double first = double.Parse(param.Substring(0, param.IndexOf(" ")).Trim(' '));
            double second = double.Parse(param.Substring(param.IndexOf(" ")).Trim(' '));
            return ((double)value / second) * first;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LessThanOrEqualToConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (((double)value) <= double.Parse(parameter.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SubtractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (((double)value) - double.Parse(parameter.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsNullOrWhiteSpaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("IsNullOrWhiteSpaceConverter: " + value.ToString() + ", " + string.IsNullOrWhiteSpace(value.ToString()));
            return string.IsNullOrWhiteSpace(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WidthToHalfWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return ((double)value) / 2;
            }
            else
            {
                string paramString = (string)parameter;
                return ((double)value) / System.Convert.ToDouble(paramString);
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return ((double)value) * 2;
            }
            else
            {
                string paramString = (string)parameter;
                return ((double)value) * System.Convert.ToDouble(paramString);
            }
        }
    }

    public class NullableBoolToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (((bool?)value).Value == true)
            {
                return true;
            }
            else if (((bool?)value).Value == false)
            {
                return false;
            }
            else if (parameter != null)
            {
                if (bool.Parse(parameter.ToString()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            /*bool? val = false;
            if (bool.Parse(value.ToString()))
                val = true;

            return val;*/
            bool? val = value as bool?;

            if (val.Value == true)
                return true;
            else
                return false;
        }
    }

    public class BoolToNullableBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? val = false;
            if (bool.Parse(value.ToString()))
                val = true;

            return val;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (((bool?)value).Value == true)
            {
                return true;
            }
            else if (((bool?)value).Value == false)
            {
                return false;
            }
            else if (parameter != null)
            {
                if (bool.Parse(parameter.ToString()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

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

    public class NullableBoolToBoolInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? inValue = (bool?)value;
            if (inValue == false)
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inValue = bool.Parse(value.ToString());

            bool? outValue = true;

            if (inValue)
                outValue = false;

            return outValue;
        }
    }

    public class ThicknessToDoubleConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType,
            Object parameter, CultureInfo culture)
        {
            Thickness val = (Thickness)value;
            var param = parameter.ToString().ToLower();
            if (param == "t")
            {
                return val.Top;
            }
            else if (param == "r")
            {
                return val.Right;
            }
            else if (param == "b")
            {
                return val.Bottom;
            }
            else
            {
                return val.Left;
            }
        }

        public Object ConvertBack(Object value, Type targetType,
            Object parameter, CultureInfo culture)
        {
            var param = parameter.ToString().ToLower();
            if (param == "t")
            {
                return (Double)(((Thickness)(value)).Top);
            }
            else if (param == "r")
            {
                return (Double)(((Thickness)(value)).Right);
            }
            else if (param == "b")
            {
                return (Double)(((Thickness)(value)).Bottom);
            }
            else
            {
                return (Double)(((Thickness)(value)).Left); //return new Thickness((double)value, 0, (double)value * -1, 0);
            }
        }
    }

    public class DoubleComparisonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            double val = double.Parse(value.ToString());
            int paramFirstNumerical = 0;
            for (int i = 0; i < parameter.ToString().Length; i++)
            {
                if (char.IsNumber(parameter.ToString().ElementAt(i)))
                {
                    paramFirstNumerical = i;
                    break;
                }
            }

            double param = double.Parse(parameter.ToString().Substring(paramFirstNumerical));

            string opr = parameter.ToString().Substring(0, paramFirstNumerical);

            if (opr == ">")
                return val > param;
            else if (opr == "<")
                return val < param;
            else if (opr == ">=")
                return val >= param;
            else if (opr == "<=")
                return val <= param;
            else
                return val >= param;
        }

        public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleAdderConverter : IValueConverter
    {
        public Object Convert(
            Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            return Double.Parse(value.ToString()) + Double.Parse(parameter.ToString());
        }

        public Object ConvertBack(
            Object value, Type targetTypes, Object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SolidColorBrushToColorConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            var color = Color.FromArgb(0xFF, 0xFF, 0x00, 0xFF);

            if ((value != null) && (value is SolidColorBrush))
                color = (value as SolidColorBrush).Color;
            else if ((parameter != null) && (parameter is Color))
                color = (Color)parameter;

            return color;
        }

        public Object ConvertBack(Object value, Type targetTypes, Object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IconToImageBrushConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            var icon = (value as System.Drawing.Icon);
            if (icon != null)
            {
                int param = 32;

                int validateParam = param;

                if (parameter != null)
                    int.TryParse((string)parameter, out validateParam);

                if (validateParam > 0)
                    param = validateParam;

                int targetSize = param;

                if (param >= 256)
                    targetSize = SystemScaling.WpfUnitsToRealPixels(param);

                return new ImageBrush(Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(targetSize, targetSize)));
            }
            else return new ImageBrush();
        }

        public Object ConvertBack(Object value, Type targetType,
            Object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetMaximizeBorderThicknessConverter : IValueConverter
    {
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double verticalWidth = SystemScaling.RealPixelsToWpfUnits(GetSystemMetrics(32));
            double horizontalHeight = SystemScaling.RealPixelsToWpfUnits(GetSystemMetrics(33));
            //double borderSize = SystemParameters.ResizeFrameVerticalBorderWidth + SystemParameters.FixedFrameVerticalBorderWidth - SystemParameters.BorderWidth;
            return new Thickness(verticalWidth - 1, horizontalHeight - 1, (verticalWidth - 1) * -1, (horizontalHeight - 1) * -1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetMaximizeOffsetDoubleConverter : IValueConverter
    {
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var win = (value as Window);

            var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)win.Left, (int)win.Top));

            double returnValue = 0;

            if ((parameter != null) && (parameter.ToString().ToLowerInvariant() == "vertical"))
                returnValue = (double)SystemScaling.RealPixelsToWpfUnits(screen.WorkingArea.Location.Y);
            else
                returnValue = (double)SystemScaling.RealPixelsToWpfUnits(screen.WorkingArea.Location.X);

            Debug.WriteLine("returnValue: " + returnValue.ToString() + " " + (parameter != null).ToString());
            return returnValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}