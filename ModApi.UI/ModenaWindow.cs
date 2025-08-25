using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Button = System.Windows.Controls.Button;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using ModApi.UI;
using static ModApi.UI.NativeMethods;
using static ModApi.UI.SystemScaling;

namespace ModApi.UI
{
    [TemplatePart(Name = PartTitlebar, Type = typeof(Thumb))]
    [TemplatePart(Name = PartMinimizeButton, Type = typeof(Button))]
    [TemplatePart(Name = PartMaximizeButton, Type = typeof(Button))]
    [TemplatePart(Name = PartRestoreButton, Type = typeof(Button))]
    [TemplatePart(Name = PartCloseButton, Type = typeof(Button))]
    [TemplatePart(Name = PartThumbBottom, Type = typeof(Thumb))]
    [TemplatePart(Name = PartThumbTop, Type = typeof(Thumb))]
    [TemplatePart(Name = PartThumbBottomRightCorner, Type = typeof(Thumb))]
    [TemplatePart(Name = PartThumbTopRightCorner, Type = typeof(Thumb))]
    [TemplatePart(Name = PartThumbTopLeftCorner, Type = typeof(Thumb))]
    [TemplatePart(Name = PartThumbBottomLeftCorner, Type = typeof(Thumb))]
    [TemplatePart(Name = PartThumbRight, Type = typeof(Thumb))]
    [TemplatePart(Name = PartThumbLeft, Type = typeof(Thumb))]
    public class ModenaWindow : Window
    {
        const string PartTitlebar = "PART_Titlebar";
        const string PartMinimizeButton = "PART_MinimizeButton";
        const string PartMaximizeButton = "PART_MaximizeButton";
        const string PartRestoreButton = "PART_RestoreButton";
        const string PartCloseButton = "PART_CloseButton";
        const string PartThumbBottom = "PART_ThumbBottom";
        const string PartThumbTop = "PART_ThumbTop";
        const string PartThumbBottomRightCorner = "PART_ThumbBottomRightCorner";
        const string PartResizeGrip = "PART_ResizeGrip";
        const string PartThumbTopRightCorner = "PART_ThumbTopRightCorner";
        const string PartThumbTopLeftCorner = "PART_ThumbTopLeftCorner";
        const string PartThumbBottomLeftCorner = "PART_ThumbBottomLeftCorner";
        const string PartThumbRight = "PART_ThumbRight";
        const string PartThumbLeft = "PART_ThumbLeft";

        IntPtr _handle;

        public bool AnimateOnShowHide
        {
            get => (bool)GetValue(AnimateOnShowHideProperty);
            set => SetValue(AnimateOnShowHideProperty, (value));
        }

        public static readonly DependencyProperty AnimateOnShowHideProperty =
            DependencyProperty.Register("AnimateOnShowHide", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(true));

        bool _isWindowsXp
        {
            get => (Environment.OSVersion.Version.Major <= 5);
        }

        public double ShadowOpacity
        {
            get
            {
                if (WindowState == WindowState.Normal)
                {
                    ShiftShadowBehindWindow();
                    return (double)GetValue(ShadowOpacityProperty);
                }
                else
                {
                    return (double)0;
                }
            }
            set => SetValue(ShadowOpacityProperty, (value * Opacity));
        }

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.RegisterAttached("ShadowOpacity", typeof(double), typeof(ModenaWindow), new PropertyMetadata((double)1));

        new public PlexResizeMode ResizeMode
        {
            get => (PlexResizeMode)GetValue(ResizeModeProperty);
            set => SetValue(ResizeModeProperty, value);
        }

        new public static readonly DependencyProperty ResizeModeProperty =
            DependencyProperty.Register("ResizeMode", typeof(PlexResizeMode), typeof(ModenaWindow), new PropertyMetadata(PlexResizeMode.CanResize, OnResizeModePropertyChangedCallback));

        static void OnResizeModePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int GWL_STYLE = (-16);
            var window = (ModenaWindow)d;
            var hwnd = new WindowInteropHelper(window).Handle;

            if (((window.ResizeMode == PlexResizeMode.Manual) & (!(window.ShowMaxRestButton))) | ((window.ResizeMode == PlexResizeMode.CanResize) | (window.ResizeMode == PlexResizeMode.CanResizeWithGrip)))
            {
                SetWindowLong(hwnd, GWL_STYLE, 0x16CF0000);
            }
            else if (((window.ResizeMode == PlexResizeMode.Manual) & (window.ShowMinButton)) | (window.ResizeMode == PlexResizeMode.CanMinimize))
            {
                SetWindowLong(hwnd, GWL_STYLE, 0x16CA0000);
            }
            else
            {
                SetWindowLong(hwnd, GWL_STYLE, 0x16C80000);
            }
            window.RepairWindowStyle();
        }

        void RepairWindowStyle()
        {
            SetWindowLong(_handle, GwlStyle, (GetWindowLong(_handle, GwlStyle).ToInt32() & ~262144));
            //SetWindowLong(_handle, GwlExstyle, (GetWindowLong(_handle, GwlExstyle).ToInt32() & 0x00000020));
        }

        /*void SyncBaseResizeMode()
        {
            if (ResizeMode == PlexResizeMode.CanMinimize)
                base.ResizeMode = System.Windows.ResizeMode.CanMinimize;
            else if (ResizeMode == PlexResizeMode.CanResize)
                base.ResizeMode = System.Windows.ResizeMode.CanResize;
            else if (ResizeMode == PlexResizeMode.CanResizeWithGrip)
                base.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;
            else
                base.ResizeMode = System.Windows.ResizeMode.NoResize;
        }*/

        public static readonly DependencyProperty MaximizedProperty =
            DependencyProperty.Register("Maximized", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty MinimizedProperty =
            DependencyProperty.Register("Minimized", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty WindowRectProperty = DependencyProperty.Register("WindowRect", typeof(Rect),
            typeof(ModenaWindow), new PropertyMetadata(new Rect(), OnWindowRectPropertyChangedCallback));

        static void OnWindowRectPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MoveWindow(new WindowInteropHelper((ModenaWindow)d).Handle, (int)((Rect)e.NewValue).Left, (int)((Rect)e.NewValue).Top, (int)((Rect)e.NewValue).Width, (int)((Rect)e.NewValue).Height, true);
        }

        public static readonly DependencyProperty FullWindowContentProperty =
            DependencyProperty.RegisterAttached("FullWindowContent", typeof(object), typeof(ModenaWindow),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TitleBarContentProperty =
            DependencyProperty.RegisterAttached("TitleBarContent", typeof(object), typeof(ModenaWindow),
                new PropertyMetadata(null));

        /*public static readonly DependencyProperty TitleBarHeightProperty = DependencyProperty.Register("TitleBarHeight",
            typeof(double), typeof(ModenaWindow),
            new FrameworkPropertyMetadata((double)24, FrameworkPropertyMetadataOptions.AffectsRender));*/

        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register("ShowTitle", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowTitleBarProperty =
            DependencyProperty.Register("ShowTitleBar", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty ShadowOffsetThicknessProperty =
            DependencyProperty.Register("ShadowOffsetThickness", typeof(Thickness), typeof(ModenaWindow), new PropertyMetadata(new Thickness(0)));

        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        public static readonly DependencyProperty ShowCloseButtonProperty =
            DependencyProperty.Register("ShowCloseButton", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(true));

        public bool ShowMaxRestButton
        {
            get => (bool)GetValue(ShowMaxRestButtonProperty);
            set => SetValue(ShowMaxRestButtonProperty, value);
        }

        public static readonly DependencyProperty ShowMaxRestButtonProperty =
            DependencyProperty.Register("ShowMaxRestButton", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(true));

        public bool ShowMinButton
        {
            get => (bool)GetValue(ShowMinButtonProperty);
            set => SetValue(ShowMinButtonProperty, value);
        }

        public static readonly DependencyProperty ShowMinButtonProperty =
            DependencyProperty.Register("ShowMinButton", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(true));

        public bool ShowResizeEdges
        {
            get => (bool)GetValue(ShowResizeEdgesProperty);
            set => SetValue(ShowResizeEdgesProperty, value);
        }

        public static readonly DependencyProperty ShowResizeEdgesProperty =
            DependencyProperty.Register("ShowResizeEdges", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(true));

        public bool ShowResizeGrip
        {
            get => (bool)GetValue(ShowResizeGripProperty);
            set => SetValue(ShowResizeGripProperty, value);
        }

        public static readonly DependencyProperty ShowResizeGripProperty =
            DependencyProperty.Register("ShowResizeGrip", typeof(bool), typeof(ModenaWindow), new PropertyMetadata(true));

        TimeSpan AnimateInDuration = TimeSpan.FromMilliseconds(500);
        TimeSpan AnimateMidDuration = TimeSpan.FromMilliseconds(500);
        TimeSpan AnimateOutDuration = TimeSpan.FromMilliseconds(1000);

        ScaleTransform scaleTransform = new ScaleTransform()
        {
            /*CenterX = (ActualWidth / 2),
            CenterY = (ActualHeight / 2),*/
            ScaleX = 1,
            ScaleY = 1
        };


        /// <summary>
        ///     Interaction logic for ModenaWindow.xaml
        /// </summary>
        public ModenaWindow()
        {
            var dictionary = new ResourceDictionary();
            try
            {
                dictionary.Source = new Uri(@"pack://application:,,,/ModApi.UI;component/Themes/Modena.xaml");
                Resources.MergedDictionaries.Add(dictionary);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            Style = (Style)Resources["ModenaWindowStyle"];
            _handle = new WindowInteropHelper(this).EnsureHandle();

            _shadowWindow = new ShadowWindow(this);
            WindowStyle = WindowStyle.None;
            //AllowsTransparency = true;
            RenderTransform = scaleTransform;
            /*scaleTransform.Changed += (sneder, args) =>
            {
                int left = (int)((Width - (Width * scaleTransform.ScaleX)) / 2);
                int top = (int)((Height - (Height * scaleTransform.ScaleY)) / 2);
                int width = ((int)WpfUnitsToRealPixels(Width) + 1) - (left * 2);
                int height = ((int)WpfUnitsToRealPixels(Height) + 1) - (top * 2);
                //IntPtr hRgn = CreateRectRgn(0, 0, 0, 0);
                //CombineRgn(hRgn, CreateRectRgn(0, 0, 3, 3), CreateRoundRectRgn(left, top, width, height, _cornerRadius, _cornerRadius), 2);
                Debug.WriteLine(left + ", " + top + ", " + width + ", " + height + ", " + scaleTransform.ScaleX + ", " + scaleTransform.ScaleY);
                SetWindowRgn(_handle, /*hRgn/CreateRoundRectRgn(left, top, width, height, _cornerRadius, _cornerRadius), true);
            };*/
            RenderTransformOrigin = new Point(0.5, 0.5);
            RepairWindowStyle();
            Loaded += ModenaWindow_Loaded;
            IsVisibleChanged += ModenaWindow_IsVisibleChanged;
            Activated += ModenaWindow_WindowFocusChanged;
            Deactivated += ModenaWindow_WindowFocusChanged;
            var restoreMinSettings = new RoutedCommand();
            restoreMinSettings.InputGestures.Add(new KeyGesture(Key.Down, ModifierKeys.Windows));
            CommandBindings.Add(new CommandBinding(restoreMinSettings, RestoreMinimizeWindow));
            //Closing += ModenaWindow_Closing;
            Closed += ModenaWindow_Closed;
            MouseEnter += ModenaWindow_MouseTransfer;
            MouseLeave += ModenaWindow_MouseTransfer;
            SizeChanged += ModenaWindow_SizeChanged;

            if (Environment.OSVersion.Version.Major <= 5)
            {
                base.ResizeMode = System.Windows.ResizeMode.NoResize;
            }
            else
                base.ResizeMode = System.Windows.ResizeMode.CanResize;

            //Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 120 });
        }

        void ModenaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializationComplete();
        }

        private void ModenaWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SyncShadowToWindowSize();
        }

        private void ModenaWindow_MouseTransfer(object sender, MouseEventArgs e)
        {
            ShiftShadowBehindWindow();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ShiftShadowBehindWindow();
        }

        public bool Maximized
        {
            get => (bool)GetValue(MaximizedProperty);
            set => SetValue(MaximizedProperty, value);
        }

        public bool Minimized
        {
            get => (bool)GetValue(MinimizedProperty);
            set => SetValue(MinimizedProperty, value);
        }

        public Rect WindowRect
        {
            get => (Rect)GetValue(WindowRectProperty);
            set => SetValue(WindowRectProperty, value);
        }

        public object FullWindowContent
        {
            get => GetValue(FullWindowContentProperty);
            set => SetValue(FullWindowContentProperty, value);
        }

        public object TitleBarContent
        {
            get => GetValue(TitleBarContentProperty);
            set => SetValue(TitleBarContentProperty, value);
        }

        /*public double TitleBarHeight
        {
            get => (double)GetValue(TitleBarHeightProperty);
            set => SetValue(TitleBarHeightProperty, value);
        }*/

        public bool ShowTitle
        {
            get => (bool)GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        public bool ShowTitleBar
        {
            get => (bool)GetValue(ShowTitleBarProperty);
            set => SetValue(ShowTitleBarProperty, value);
        }

        public Thickness ShadowOffsetThickness
        {
            get => (Thickness)GetValue(ShadowOffsetThicknessProperty);
            set => SetValue(ShadowOffsetThicknessProperty, value);
        }

        readonly ShadowWindow _shadowWindow;

        LinearGradientBrush _bodyLinearGradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection(new List<GradientStop>
            {
                new GradientStop
                {
                    Offset = 0,
                    Color = Colors.White
                },
                new GradientStop
                {
                    Offset = 1,
                    Color = Color.FromArgb(0xFF, 0xC8, 0xD4, 0xE7)
                }
            })
        };

        Button _closeButton;
        Button _maxButton;
        Button _minButton;
        Button _restButton;

        /*public Thickness ShadowOffsetThickness = new Thickness(75, 150, 75, 50);*/
        Thumb _thumbBottom;
        Thumb _thumbBottomLeftCorner;
        Thumb _thumbBottomRightCorner;
        Thumb _resizeGrip;
        Thumb _thumbLeft;
        Thumb _thumbRight;
        Thumb _thumbTop;
        Thumb _thumbTopLeftCorner;
        Thumb _thumbTopRightCorner;

        Thumb _titlebar;

        void RestoreMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
            }
        }

        void InitializationComplete()
        {
            SyncShadowToWindowSize();
            if (IsVisible)
            {
                Show();
            }

            /*Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri(@"pack://application:,,,/ModAPI.Installers;component/FallbackLanguage.xaml")
            });
            
            if (System.IO.Directory.Exists(ManagerSettings.AppDataSubfolder))
            {
                Resources.MergedDictionaries.Add(ManagerSettings.CurrentLanguage);
            }*/
        }

        new public void Show()
        {
            ShiftShadowBehindWindow();
            base.Show();
            ShiftShadowBehindWindow();
            ShowWindow();
            ShiftShadowBehindWindow();
        }

        new public bool? ShowDialog()
        {
            ShiftShadowBehindWindow();
            ShowWindow();
            ModenaWindow_Loaded(this, null);
            bool? value = base.ShowDialog();
            ShiftShadowBehindWindow();
            return value;
        }

        CubicEase circleEase = new CubicEase()
        {
            EasingMode = EasingMode.EaseOut
        };

        private void ShowWindow()
        {
            if (AnimateOnShowHide && (!_isWindowsXp))
            {
                ShiftShadowBehindWindow();
                /*scaleTransform.CenterX = (ActualWidth / 2);
                scaleTransform.CenterY = (ActualHeight / 2);*/

                DoubleAnimation windowOpacityAnimation = new DoubleAnimation()
                {
                    From = 0,
                    To = 1,
                    Duration = AnimateInDuration,
                    EasingFunction = circleEase
                };

                DoubleAnimation windowSizeAnimation = new DoubleAnimation()
                {
                    From = 0.75,
                    To = 1,
                    Duration = AnimateInDuration,
                    EasingFunction = circleEase
                };
                /*System.Timers.Timer timer = new System.Timers.Timer(10);
                timer.Elapsed += (sneder, args) =>
                {
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                    {
                        int left = (int)(scaleTransform.ScaleX * Width);
                        int top = (int)(scaleTransform.ScaleX * Height);
                        int width = ((int)WpfUnitsToRealPixels(Width) + 1) - (left * 2);
                        int height = ((int)WpfUnitsToRealPixels(Height) + 1) - (top * 2);
                        SetWindowRgn(_handle, CreateRoundRectRgn(left, top, width, height, _cornerRadius, _cornerRadius), true);
                    }));
                };*/
                windowOpacityAnimation.Completed += (sendurr, args) =>
                {
                    //timer.Stop();
                    SyncShadowToWindowSize();
                    ShiftShadowBehindWindow();
                    ModenaWindow_ResetTransformProperties();
                };


                //AnimateWindow(_handle, 500, AnimateWindowFlags.AW_BLEND);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, windowSizeAnimation);
                //timer.Start();
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, windowSizeAnimation);
                BeginAnimation(Window.OpacityProperty, windowOpacityAnimation);
                ShiftShadowBehindWindow();
            }
            else
            {
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
                Opacity = 1;
                ShiftShadowBehindWindow();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            try
            {
                _titlebar = GetTemplateChild(PartTitlebar) as Thumb;
                _titlebar.PreviewMouseLeftButtonDown += Titlebar_PreviewMouseLeftButtonDown;
                _titlebar.PreviewMouseDoubleClick += Titlebar_PreviewMouseDoubleClick;
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("TITLEBAR \n" + ex);
            }

            try
            {
                _minButton = GetTemplateChild(PartMinimizeButton) as Button;
                _minButton.Click += (sneder, args) => { WindowState = WindowState.Minimized; };
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("MINBUTTON \n" + ex);
            }

            try
            {
                _maxButton = GetTemplateChild(PartMaximizeButton) as Button;
                _maxButton.Click += (sneder, args) =>
                {
                    WindowState = WindowState.Maximized;
                };
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("MAXBUTTON \n" + ex);
            }

            try
            {
                _restButton = GetTemplateChild(PartRestoreButton) as Button;
                _restButton.Click += (sneder, args) =>
                {
                    WindowState = WindowState.Normal;
                };
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("RESTBUTTON \n" + ex);
            }

            try
            {
                _closeButton = GetTemplateChild(PartCloseButton) as Button;
                _closeButton.Click += (sneder, args) =>
                {
                    Close();
                };
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("CLOSEBUTTON \n" + ex);
            }


            try
            {
                _thumbBottom = GetTemplateChild(PartThumbBottom) as Thumb;
                _thumbBottom.DragDelta += ThumbBottom_DragDelta;


                _thumbTop = GetTemplateChild(PartThumbTop) as Thumb;
                _thumbTop.DragDelta += ThumbTop_DragDelta;


                _thumbBottomRightCorner = GetTemplateChild(PartThumbBottomRightCorner) as Thumb;
                _thumbBottomRightCorner.DragDelta += ThumbBottomRightCorner_DragDelta;


                _thumbTopRightCorner = GetTemplateChild(PartThumbTopRightCorner) as Thumb;
                _thumbTopRightCorner.DragDelta += ThumbTopRightCorner_DragDelta;


                _thumbTopLeftCorner = GetTemplateChild(PartThumbTopLeftCorner) as Thumb;
                _thumbTopLeftCorner.DragDelta += ThumbTopLeftCorner_DragDelta;


                _thumbBottomLeftCorner = GetTemplateChild(PartThumbBottomLeftCorner) as Thumb;
                _thumbBottomLeftCorner.DragDelta += ThumbBottomLeftCorner_DragDelta;


                _thumbRight = GetTemplateChild(PartThumbRight) as Thumb;
                _thumbRight.DragDelta += ThumbRight_DragDelta;


                _thumbLeft = GetTemplateChild(PartThumbLeft) as Thumb;
                _thumbLeft.DragDelta += ThumbLeft_DragDelta;
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("THUMBS \n" + ex);
            }

            try
            {
                _resizeGrip = GetTemplateChild(PartResizeGrip) as Thumb;
                _resizeGrip.DragDelta += ThumbBottomRightCorner_DragDelta;
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("RESIZEGRIP \n" + ex);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            SyncShadowToWindowSize();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            SyncShadowToWindow();
        }

        private void HideWindow()
        {
            base.Hide();
        }

        new public void Hide()
        {
            ShiftShadowBehindWindow();
            if (AnimateOnShowHide && (!_isWindowsXp))
            {
                /*scaleTransform.CenterX = (ActualWidth / 2);
                scaleTransform.CenterY = (ActualHeight / 2);*/

                DoubleAnimation windowOpacityAnimation = new DoubleAnimation()
                {
                    From = 1,
                    To = 0,
                    Duration = AnimateOutDuration,
                    EasingFunction = circleEase
                };

                DoubleAnimation windowSizeAnimation = new DoubleAnimation()
                {
                    From = 1,
                    To = 0.75,
                    Duration = AnimateOutDuration,
                    EasingFunction = circleEase
                };
                windowOpacityAnimation.Completed += (sendurr, args) =>
                {
                    HideWindow();
                };
                BeginAnimation(Window.OpacityProperty, windowOpacityAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, windowSizeAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, windowSizeAnimation);
            }
            else
            {
                HideWindow();
            }
        }

        bool isHiding = false;

        private void ModenaWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                ShiftShadowBehindWindow();
                Show();
                ShiftShadowBehindWindow();
            }
            else if (!isHiding)
            {
                Hide();
                isHiding = true;
            }
            else
            {
                Visibility = Visibility.Hidden;
            }

            ShiftShadowBehindWindow();
        }

        bool _isClosingNow = false;

        //private void ModenaWindow_Closing(object sender, CancelEventArgs e)
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!_isWindowsXp)
            {
                bool animate = false;

                if (!(e.Cancel) & !_isClosingNow)
                {
                    e.Cancel = true;
                    _isClosingNow = true;
                    animate = true;
                    //Debug.WriteLine("e.Cancel and _isClosingNow are both false");
                }

                if (animate == true) //(cancel == true) & (_isClosingNow == true))
                {
                    /*scaleTransform.CenterX = (ActualWidth / 2);
                    scaleTransform.CenterY = (ActualHeight / 2);*/

                    DoubleAnimation windowOpacityAnimation = new DoubleAnimation()
                    {
                        From = 1,
                        To = 0,
                        Duration = AnimateOutDuration,
                        EasingFunction = circleEase
                    };

                    DoubleAnimation windowSizeAnimation = new DoubleAnimation()
                    {
                        From = 1,
                        To = 0.75,
                        Duration = AnimateOutDuration,
                        EasingFunction = circleEase
                    };
                    BeginAnimation(Window.OpacityProperty, windowOpacityAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, windowSizeAnimation);
                    windowSizeAnimation.Completed += (sendurr, args) =>
                    {
                        Close();
                    };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, windowSizeAnimation);
                }
            }
        }

        private void ModenaWindow_Closed(object sender, EventArgs e)
        {
            _shadowWindow.Close();
            //Debug.WriteLine("CLOSED");
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            Screen s = Screen.FromHandle(hwnd);
            if (WindowState == WindowState.Maximized)
            {
                Maximized = true;
                _maxButton.Visibility = Visibility.Hidden;
                _restButton.Visibility = Visibility.Visible;
                MaxWidth = RealPixelsToWpfUnits(s.WorkingArea.Width);
                MaxHeight = RealPixelsToWpfUnits(s.WorkingArea.Height);
                //Margin = new Thickness(SystemParameters.BorderWidth + 2, SystemParameters.BorderWidth - 2, (SystemParameters.BorderWidth + 2) * -1, (SystemParameters.BorderWidth + 2) * -1);
                //Margin = new Thickness(SystemParameters.BorderWidth + 5, SystemParameters.BorderWidth + 2, (SystemParameters.BorderWidth + 6) * -1, (SystemParameters.BorderWidth + 6) * -1);
                //Margin = new Thickness(SystemParameters.BorderWidth, SystemParameters.BorderWidth - 2, (SystemParameters.BorderWidth + 6) * -1, (SystemParameters.BorderWidth + 6) * -1);
                //////Margin = new Thickness(SystemParameters.BorderWidth, SystemParameters.BorderWidth, (SystemParameters.BorderWidth + 6) * -1, (SystemParameters.BorderWidth + 6) * -1);
            }
            else
            {
                _maxButton.Visibility = Visibility.Visible;
                _restButton.Visibility = Visibility.Hidden;
              ////////Margin = new Thickness(0);
                //Padding = new Thickness(0);
                MaxWidth = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;

                if (WindowState == WindowState.Minimized)
                {
                    if (!Minimized)
                    {
                        if (Maximized)
                        {
                            WindowState = WindowState.Maximized;
                        }
                        else
                        {
                            WindowState = WindowState.Normal;
                        }
                        ModenaWindow_AnimateMinimize();
                        Minimized = true;
                    }
                }
                else
                {
                    if (Minimized)
                    {
                        ModenaWindow_AnimateRestoreUp();
                    }
                    else if (Maximized)
                    {
                        /*WindowRect = new Rect(Left + 100, Top + 100, Width - 200, Height - 200);
                        ShiftShadowBehindWindow();
                        WindowRect = new Rect(Left - 100, Top - 100, Width + 200, Height + 200);*/
                    }
                }
                Maximized = false;
            }

            ShiftShadowBehindWindow();

            if (Maximized)
            {
                //Screen s = Screen.FromHandle(_handle);
                //SetWindowRgn(_handle, CreateRectRgn(0, 0, s.WorkingArea.Width, s.WorkingArea.Height), true);
            }
            else
                SyncShadowToWindowSize();
        }

        Rect RestoreTo = new Rect(0, 0, 0, 0);

        public void ModenaWindow_AnimateRestoreUp()
        {
            if (!_isWindowsXp)
            {
                /*scaleTransform.CenterX = (ActualWidth / 2);
                scaleTransform.CenterY = ActualHeight;*/
                DoubleAnimation windowSizeAnimation = new DoubleAnimation()
                {
                    From = 0.5,
                    To = 1,
                    Duration = AnimateMidDuration/*,
                    EasingFunction = circleEase*/
                };

                DoubleAnimation windowTopAnimation = new DoubleAnimation()
                {
                    To = RestoreTo.Y,
                    From = RealPixelsToWpfUnits(Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea.Bottom),
                    Duration = AnimateMidDuration,
                    EasingFunction = circleEase
                };
                windowTopAnimation.Completed += (sneder, args) =>
                {
                    Minimized = false;
                    ModenaWindow_ResetTransformProperties();
                };
                //scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, windowSizeAnimation);
                //scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, windowSizeAnimation);
                //AnimateWindow(_handle, 250, AnimateWindowFlags.AW_BLEND | AnimateWindowFlags.AW_ACTIVATE);
                BeginAnimation(ModenaWindow.TopProperty, windowTopAnimation);
            /*  /*for (double i = 0; i < 100; ++i)
                {
                    this.Opacity = i / 100;
             / }*/
                BeginAnimation(ModenaWindow.OpacityProperty, windowSizeAnimation);
            }
            else
                Minimized = false;
        }

        public void ModenaWindow_AnimateMinimize()
        {
            if (!_isWindowsXp)
            {
                RestoreTo = new Rect(Left, Top, ActualWidth, ActualHeight);
                var windowRect = WindowRect;
                /*scaleTransform.CenterX = (ActualWidth / 2);
                scaleTransform.CenterY = ActualHeight;*/
                CircleEase circleEase = new CircleEase()
                {
                    EasingMode = EasingMode.EaseIn
                };

                DoubleAnimation windowSizeAnimation = new DoubleAnimation()
                {
                    From = 1,
                    To = 0,
                    Duration = AnimateMidDuration//,
                    //EasingFunction = circleEase
                };

                //double topTarget = System.Windows.Forms.Screen.FromRectangle(System.Drawing.Rectangle.FromLTRB((int)WindowRect.X, (int)WindowRect.Y, (int)WindowRect.Right, (int)WindowRect.Bottom)).WorkingArea.Bottom - 1;
                double origTop = Top;
                double topTarget = Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea.Bottom - 1;
                DoubleAnimation windowTopAnimation = new DoubleAnimation()
                {
                    To = topTarget,
                    //From = Top,
                    Duration = AnimateMidDuration,
                    EasingFunction = new CircleEase() { EasingMode = EasingMode.EaseIn }
                };
                windowTopAnimation.Completed += (sneder, args) =>
                {
                    BeginAnimation(ModenaWindow.TopProperty, null);
                    Top = origTop;
                    ModenaWindow_ResetTransformProperties();
                    Minimized = true;
                    WindowRect = windowRect;
                    WindowState = WindowState.Minimized;
                };
                //scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, windowSizeAnimation);
                //scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, windowSizeAnimation);
                BeginAnimation(ModenaWindow.OpacityProperty, windowSizeAnimation);
                //AnimateWindow(_handle, 250, AnimateWindowFlags.AW_BLEND & AnimateWindowFlags.AW_HIDE);
                BeginAnimation(ModenaWindow.TopProperty, windowTopAnimation);
            }
            else
            {
                Minimized = true;
                WindowState = WindowState.Minimized;
            }
        }

        void ModenaWindow_ResetTransformProperties()
        {
            if (!_isWindowsXp)
            {
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
                BeginAnimation(ModenaWindow.LeftProperty, null);
                BeginAnimation(ModenaWindow.TopProperty, null);
                BeginAnimation(ModenaWindow.WidthProperty, null);
                BeginAnimation(ModenaWindow.HeightProperty, null);
                BeginAnimation(ModenaWindow.OpacityProperty, null);
                BeginAnimation(ModenaWindow.WindowRectProperty, null);
                Opacity = 1;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            ModenaWindow_WindowFocusChanged(this, null);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            ModenaWindow_WindowFocusChanged(this, null);
        }

        public void ShiftShadowBehindWindow()
        {
            if (_shadowWindow != null)
            {
                SetWindowPos(new WindowInteropHelper(_shadowWindow).Handle, new WindowInteropHelper(this).Handle, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0010);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }


        void ModenaWindow_WindowFocusChanged(object sender, EventArgs e)
        {
            ShiftShadowBehindWindow();
        }

        public void SyncShadowToWindow()
        {
            _shadowWindow.Left = (Left - ShadowOffsetThickness.Left) + Padding.Left;
            _shadowWindow.Top = (Top - ShadowOffsetThickness.Top) + Padding.Top;
        }

        int _cornerRadius = (int)WpfUnitsToRealPixels(4);

        public void SyncShadowToWindowSize()
        {
            //SetWindowRgn(_handle, CreateRoundRectRgn(0, 0, (int)WpfUnitsToRealPixels(Width) + 1, (int)WpfUnitsToRealPixels(Height) + 1, _cornerRadius, _cornerRadius), true);
            SyncShadowToWindow();
            _shadowWindow.Width = ((Width - Padding.Left) - Padding.Right) + (ShadowOffsetThickness.Left + ShadowOffsetThickness.Right);
            _shadowWindow.Height = ((Height - Padding.Top) - Padding.Bottom) + (ShadowOffsetThickness.Top + ShadowOffsetThickness.Bottom);
        }

        /*public void SyncShadowToWindowScale()
        {
            var scaleTransform = RenderTransform as ScaleTransform;
            _shadowWindow.RenderTransform = new ScaleTransform()
            {
                ScaleX = 1,
                ScaleY = 1,
                /*CenterX = scaleTransform.CenterX,
                CenterY = scaleTransform.CenterY/
            };
            (_shadowWindow.RenderTransform as ScaleTransform).ScaleX = scaleTransform.ScaleX;
            (_shadowWindow.RenderTransform as ScaleTransform).ScaleY = scaleTransform.ScaleY;
        }*/

        void ModenaWindow_StateChanged(object sender, EventArgs e)
        {
            ShiftShadowBehindWindow();
        }

        void Titlebar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Titlebar_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        void ResizeLeft(DragDeltaEventArgs e)
        {
            double newWidth = Width - e.HorizontalChange;

            if (newWidth >= MinWidth)
            {
                Left = Left + e.HorizontalChange;
                Width = newWidth;
            }
            else if (e.HorizontalChange > 0)
            {
                double difference = (Width - MinWidth);
                Width = MinWidth;
                Left = Left + difference;
            }
            //Debug.WriteLine(Width + ", " + MinWidth + ", " + (MinWidth > Width).ToString());
        }

        void ResizeTop(DragDeltaEventArgs e)
        {
            /*if (Top + e.VerticalChange > MinHeight)
            {
                Top += e.VerticalChange;
                Height -= e.VerticalChange;
            }*/
            double newHeight = Height - e.VerticalChange;

            if (newHeight >= MinHeight)
            {
                Top = Top + e.VerticalChange;
                Height = newHeight;
            }
            else if (e.VerticalChange > 0)
            {
                double difference = (Height - MinHeight);
                Height = MinHeight;
                Top = Top + difference;
            }
        }

        void ResizeRight(DragDeltaEventArgs e)
        {
            if (Width + e.HorizontalChange > MinWidth)
                Width += e.HorizontalChange;
        }

        void ResizeBottom(DragDeltaEventArgs e)
        {
            if (Height + e.VerticalChange > MinHeight)
                Height += e.VerticalChange;
        }

        void ThumbBottomRightCorner_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeRight(e);
            ResizeBottom(e);
        }

        void ThumbTopRightCorner_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeRight(e);
            ResizeTop(e);
        }

        void ThumbTopLeftCorner_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeTop(e);
            ResizeLeft(e);
        }

        void ThumbBottomLeftCorner_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeLeft(e);
            ResizeBottom(e);
        }

        void ThumbRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeRight(e);
        }

        void ThumbLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeLeft(e);
        }

        void ThumbBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeBottom(e);
        }

        void ThumbTop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeTop(e);
        }
    }

    public enum PlexResizeMode
    {
        CanResize,
        CanResizeWithGrip,
        CanMinimize,
        NoResize,
        Manual
    };
}