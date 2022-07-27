using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ModApi.UI;
using ModApi.UI.Windows;

namespace FrontEndTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DecoratableWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void HideWindowButton_Click(object sender, RoutedEventArgs e)
        {
            Timer timer = new Timer(1);
            int interval = 0;
            timer.Elapsed += (sneder, args) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    interval++;
                    if (interval > 150)
                    {
                        Show();
                        Debug.WriteLine("interval: " + interval);
                        timer.Stop();
                    }
                }));
            timer.Start();
            Hide();
        }
    }
}
