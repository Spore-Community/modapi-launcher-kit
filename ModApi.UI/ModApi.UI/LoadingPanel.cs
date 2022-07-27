using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ModApi.UI
{
    [TemplatePart(Name = PartFlashGrid, Type = typeof(Grid))]
    public class LoadingPanel : ContentControl
    {
        const String PartFlashGrid = "PART_FlashGrid";


        public LoadingPanel()
        {

        }

        Grid _flashGrid;
        int _currentFlashCell = 0;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _flashGrid = GetTemplateChild(PartFlashGrid) as Grid;

            if (_flashGrid != null)
            {
                _currentFlashCell = 0;

                DoubleAnimation opacityDoubleAnimation = new DoubleAnimation()
                {
                    BeginTime = TimeSpan.FromMilliseconds(0),
                    Duration = TimeSpan.FromMilliseconds(250),
                    From = 1,
                    To = 0
                };
                opacityDoubleAnimation.Completed += delegate
                {
                    _flashGrid.Children[_currentFlashCell].BeginAnimation(Canvas.OpacityProperty, null);

                    if (_currentFlashCell >= _flashGrid.Children.Count)
                        _currentFlashCell = 0;
                    else
                        _currentFlashCell++;

                    (_flashGrid.Children[_currentFlashCell] as Canvas).BeginAnimation(Canvas.OpacityProperty, opacityDoubleAnimation);
                };

                (_flashGrid.Children[_currentFlashCell] as Canvas).BeginAnimation(Canvas.OpacityProperty, opacityDoubleAnimation);
            }
        }
    }
}
