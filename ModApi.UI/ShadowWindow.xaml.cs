using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ModApi.UI;
using static ModApi.UI.NativeMethods;

namespace ModApi.UI
{
    /// <summary>
    /// Interaction logic for ShadowWindow.xaml
    /// </summary>
    public partial class ShadowWindow : Window
    {
        //public ModenaWindow ModenaWindow;

        public ModenaWindow ModenaWindow
        {
            get => (ModenaWindow)GetValue(ModenaWindowProperty);
            set => SetValue(ModenaWindowProperty, (value));
        }

        public static readonly DependencyProperty ModenaWindowProperty =
            DependencyProperty.Register("ModenaWindow", typeof(ModenaWindow), typeof(ShadowWindow), new PropertyMetadata(null));

        public ShadowWindow(ModenaWindow window)
        {
            Opacity = 0;
            InitializeComponent();
            ModenaWindow = window;

            Binding renderTransformBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("RenderTransform"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(this, ShadowWindow.RenderTransformProperty, renderTransformBinding);

            Binding opacityBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("Opacity"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(ShadowGrid, System.Windows.Controls.Grid.OpacityProperty, opacityBinding);

            Binding shadowOpacityBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("ShadowOpacity"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(this, ShadowWindow.OpacityProperty, shadowOpacityBinding);

            Binding topmostBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("Topmost"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(this, ShadowWindow.TopmostProperty, topmostBinding);

            Binding visibilityBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("Visibility"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(this, ShadowWindow.VisibilityProperty, visibilityBinding);

            /*Binding leftBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("Left"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new ShadowLeftConverter(),
                ConverterParameter = ModenaWindow
            };
            BindingOperations.SetBinding(this, ShadowWindow.LeftProperty, leftBinding);

            Binding topBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("Top"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new ShadowTopConverter(),
                ConverterParameter = ModenaWindow
            };
            BindingOperations.SetBinding(this, ShadowWindow.TopProperty, topBinding);

            Binding widthBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("Width"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new ShadowWidthConverter(),
                ConverterParameter = ModenaWindow
            };
            BindingOperations.SetBinding(this, ShadowWindow.WidthProperty, widthBinding);

            Binding heightBinding = new Binding()
            {
                Source = ModenaWindow,
                Path = new PropertyPath("Height"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new ShadowHeightConverter(),
                ConverterParameter = ModenaWindow
            };
            BindingOperations.SetBinding(this, ShadowWindow.HeightProperty, heightBinding);*/

            Loaded += (sender, e) =>
            {
                ModenaWindow.ShiftShadowBehindWindow();
            };

            IsVisibleChanged += (sender, e) =>
            {
                ModenaWindow.ShiftShadowBehindWindow();
            };/*

            Closed += (sneder, args) =>
            {
                Debug.WriteLine("SHADOW CLOSED");
            };*/

            ModenaWindow.StateChanged += ModenaWindow_StateChanged;
        }

        private void ModenaWindow_StateChanged(object sender, EventArgs e)
        {
            if (ModenaWindow.WindowState == WindowState.Normal)
                ShadowGrid.Visibility = Visibility.Visible;
            else
                ShadowGrid.Visibility = Visibility.Collapsed;

            //Debug.WriteLine("ModenaWindow.WindowState: " + ModenaWindow.WindowState.ToString());
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            SetWindowLong(helper.Handle, -20, (int)(GetWindowLong(helper.Handle, -20)) | 0x00000080 | 0x00000020);
            ModenaWindow.ShiftShadowBehindWindow();
        }
    }

    public class RawOpacityToMultipliedOpacityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)values[0] * (double)values[1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShadowLeftConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("LEFT CONVERTER");
            //(double)value - ((Thickness)parameter).Left
            var param = parameter as ModenaWindow;
            return param.Left - param.ShadowOffsetThickness.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        //_shadowWindow.Left = (Left - _shadowOffsetThickness.Left) + Padding.Left;

        /*public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ShadowWindow shadow = (ShadowWindow)parameter;
            ModenaWindow modena = shadow.ModenaWindow;
            return (modena.Left - modena.ShadowOffsetThickness.Left) + modena.Padding.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }*/
    }

    public class ShadowTopConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("TOP CONVERTER");
            //return (double)value - ((Thickness)parameter).Top;
            var param = parameter as ModenaWindow;
            return param.Top - param.ShadowOffsetThickness.Top;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        //_shadowWindow.Top = (Top - _shadowOffsetThickness.Top) + Padding.Top;

        /*public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ShadowWindow shadow = (ShadowWindow)parameter;
            ModenaWindow modena = shadow.ModenaWindow;
            return (modena.Top - modena.ShadowOffsetThickness.Top) + modena.Padding.Top;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }*/
    }

    public class ShadowWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("WIDTH CONVERTER");
            /*double val = (double)value;
            Thickness offset = (Thickness)parameter;
            return val + offset.Left + offset.Right;*/
            var param = parameter as ModenaWindow;
            return param.Width + param.ShadowOffsetThickness.Left + param.ShadowOffsetThickness.Right;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShadowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("HEIGHT CONVERTER");
            /*double val = (double)value;
            Thickness offset = (Thickness)parameter;
            return val + offset.Top + offset.Bottom;*/
            var param = parameter as ModenaWindow;
            return param.Height + param.ShadowOffsetThickness.Top + param.ShadowOffsetThickness.Bottom;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}