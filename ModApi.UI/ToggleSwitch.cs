﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using static ModApi.UI.NativeMethods;
using static ModApi.UI.SystemScaling;

namespace ModApi.UI
{
    [TemplatePart(Name = PartGrip, Type = typeof(Button))]
    [TemplatePart(Name = PartOffsetter, Type = typeof(Canvas))]
    [TemplatePart(Name = PartStateText, Type = typeof(TextBlock))]

    public partial class ToggleSwitch : CheckBox
    {
        const string PartGrip = "PART_Grip";
        const string PartOffsetter = "PART_Offsetter";
        const string PartStateText = "PART_StateText";

        public string TrueText
        {
            get => (string)GetValue(TrueTextProperty);
            set => SetValue(TrueTextProperty, value);
        }

        public static readonly DependencyProperty TrueTextProperty =
            DependencyProperty.RegisterAttached("TrueText", typeof(string), typeof(ToggleSwitch),
                new PropertyMetadata("True"));

        public string FalseText
        {
            get => (string)GetValue(FalseTextProperty);
            set => SetValue(FalseTextProperty, value);
        }

        public static readonly DependencyProperty FalseTextProperty =
            DependencyProperty.RegisterAttached("FalseText", typeof(string), typeof(ToggleSwitch),
                new PropertyMetadata("False"));

        public string NullText
        {
            get => (string)GetValue(NullTextProperty);
            set => SetValue(NullTextProperty, value);
        }

        public static readonly DependencyProperty NullTextProperty =
            DependencyProperty.RegisterAttached("NullText", typeof(string), typeof(ToggleSwitch),
                new PropertyMetadata("Null"));

        public double OffsetterWidth
        {
            get => (double)GetValue(OffsetterWidthProperty);
            set => SetValue(OffsetterWidthProperty, value);
        }

        public static readonly DependencyProperty OffsetterWidthProperty =
            DependencyProperty.RegisterAttached("OffsetterWidth", typeof(double), typeof(ToggleSwitch),
                new PropertyMetadata((double)0));

        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
            IsCheckedProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));
        }

        public ToggleSwitch()
        {
            //Click += delegate { OnClick(); };
            Loaded += ToggleSwitch_Loaded;
            SizeChanged += ToggleSwitch_SizeChanged;
        }

        private void ToggleSwitch_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OffsetterWidth = GetOffsetterWidth();
        }

        /*protected override Size MeasureOverride(Size constraint)
        {
            constraint.Width = GetOffsetterWidth();

            if (double.IsPositiveInfinity(constraint.Width))
                constraint.Width = double.MaxValue;

            return constraint;
        }*/

        private double GetOffsetterWidth()
        {
            double widthValue = 0;
            if ((IsChecked == null) & (IsThreeState))
            {
                widthValue = (ActualWidth / 2) - (_grip.ActualWidth / 2);
            }
            else if (IsChecked == false)
            {
                widthValue = 0;
            }
            else
            {
                widthValue = ActualWidth - _grip.ActualWidth;
            }
            return widthValue;
        }

        private void ToggleSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            OnIsCheckedChanged(this, new DependencyPropertyChangedEventArgs());
        }

        /*protected override void OnChildDesiredSizeChanged(UIElement child)
        {
            base.OnChildDesiredSizeChanged(child);
            HalfWidth = Width / 2;
        }*/

        static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toggle = (d as ToggleSwitch);

            toggle.AnimateGripPosition();

            try
            {
                if (toggle.IsChecked == true)
                {
                    toggle._stateText.Text = toggle.TrueText;
                }
                else if (toggle.IsChecked == false)
                {
                    toggle._stateText.Text = toggle.FalseText;
                }
                else
                {
                    toggle._stateText.Text = toggle.NullText;
                }
            }
            catch { }
        }

        public void AnimateGripPosition()
        {
            DoubleAnimation animation = new DoubleAnimation()
            {
                Duration = TimeSpan.FromMilliseconds(125),
                EasingFunction = new QuinticEase()
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            double targetWidth = GetOffsetterWidth();

            animation.To = targetWidth;

            animation.Completed += delegate
            {
                OffsetterWidth = targetWidth;
                BeginAnimation(ToggleSwitch.OffsetterWidthProperty, null);
            };

            BeginAnimation(ToggleSwitch.OffsetterWidthProperty, animation);
        }

        Button _grip = new Button();
        Canvas _offsetter = new Canvas();
        TextBlock _stateText = new TextBlock();

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _grip = GetTemplateChild(PartGrip) as Button;
            _grip.PreviewMouseLeftButtonDown += (sendurr, args) => ToggleSwitch_PreviewMouseLeftButtonDown(this, args);
            _offsetter = GetTemplateChild(PartOffsetter) as Canvas;
            _stateText = GetTemplateChild(PartStateText) as TextBlock;
            OnIsCheckedChanged(this, new DependencyPropertyChangedEventArgs());
        }

        private void ToggleSwitch_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bool? originalValue = (sender as ToggleSwitch).IsChecked;
            //var toggleSwitch = (sender as ToggleSwitch);

            bool isDragging = false;
            double offsetter = OffsetterWidth;
            //var grip = toggleSwitch._grip;

            double toggleX = RealPixelsToWpfUnits((sender as ToggleSwitch).PointToScreen(new System.Windows.Point(0, 0)).X);
            double gripInitialX = RealPixelsToWpfUnits((sender as ToggleSwitch)._grip.PointToScreen(new System.Windows.Point(0, 0)).X);
            double gripX = RealPixelsToWpfUnits((sender as ToggleSwitch)._grip.PointToScreen(new System.Windows.Point(0, 0)).X);

            double cursorStartX = RealPixelsToWpfUnits(System.Windows.Forms.Cursor.Position.X);
            double cursorCurrentX = RealPixelsToWpfUnits(System.Windows.Forms.Cursor.Position.X);
            double cursorChange = (cursorCurrentX - cursorStartX);
            double offset = (gripX - toggleX) + (cursorCurrentX - cursorStartX);
            //System.Windows.Point cursorStartOffsetPoint = new System.Windows.Point(toggleSwitch.Margin.Left, grip.Margin.Top);

            var timer = new System.Timers.Timer(1);

            timer.Elapsed += delegate
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        //toggleX = DpiModApi.Manager.ConvertPixelsToWpfUnits((sender as ToggleSwitch).PointToScreen(new System.Windows.Point(0, 0)).X);
                        cursorCurrentX = RealPixelsToWpfUnits(System.Windows.Forms.Cursor.Position.X);

                        cursorChange = (cursorCurrentX - cursorStartX);

                        offset = cursorChange + (gripX - toggleX);
                        ////Debug.WriteLine(cursorChange.ToString() + "," + offset.ToString());

                        if ((cursorChange > 2) | (cursorChange < -2))
                        {
                            isDragging = true;
                        }

                        OffsetterWidth = offsetter + cursorChange;
                    }
                    else
                    {
                        timer.Stop();
                        //offset = (cursorCurrentX - cursorStartX);
                        if (isDragging)
                        {
                            double isCheckedOffset = 0;
                            if (IsChecked == true)
                            {
                                isCheckedOffset = ActualWidth - _grip.ActualWidth;
                            }
                            else if (IsChecked == null)
                            {
                                isCheckedOffset = (ActualWidth / 2) - (_grip.ActualWidth / 2);
                            }

                            double toggleChange = cursorChange + isCheckedOffset;
                            if (IsThreeState)
                            {
                                if (toggleChange < (ActualWidth / 3))
                                {
                                    IsChecked = false;
                                    ////Debug.WriteLine("VERTICT: false");
                                }
                                else if (toggleChange > ((ActualWidth / 3) * 2))
                                {
                                    IsChecked = true;
                                    ////Debug.WriteLine("VERTICT: true");
                                }
                                else
                                {
                                    IsChecked = null;
                                    ////Debug.WriteLine("VERTICT: null");
                                }
                            }
                            else
                            {
                                if ((ActualWidth > (_grip.ActualWidth * 2.5)) && (_grip.ActualWidth > (_grip.ActualHeight * 2)))
                                {
                                    if (originalValue == true)
                                    {
                                        if (toggleChange < (ActualWidth - (_grip.ActualWidth / 2)))
                                        {
                                            IsChecked = false;
                                            ////Debug.WriteLine("VERTICT: false");
                                        }
                                        else
                                        {
                                            IsChecked = true;
                                            ////Debug.WriteLine("VERTICT: true");
                                        }
                                    }
                                    else
                                    {
                                        if (toggleChange < (_grip.ActualWidth / 2))
                                        {
                                            IsChecked = false;
                                            ////Debug.WriteLine("VERTICT: false");
                                        }
                                        else
                                        {
                                            IsChecked = true;
                                            ////Debug.WriteLine("VERTICT: true");
                                        }
                                    }
                                }
                                else
                                {
                                    if (toggleChange < ((ActualWidth / 2) - (_grip.ActualWidth / 2)))
                                    {
                                        IsChecked = false;
                                        ////Debug.WriteLine("VERTICT: false");
                                    }
                                    else
                                    {
                                        IsChecked = true;
                                        ////Debug.WriteLine("VERTICT: true");
                                    }
                                }
                            }
                        }
                        else
                        {
                            base.OnClick();
                        }
                        if (originalValue == IsChecked)
                        {
                            AnimateGripPosition();
                        }
                    }
                }));
            };
            timer.Start();
        }
    }
}