using ModApi.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Spore_ModAPI_Easy_Installer
{
    /// <summary>
    /// Interaction logic for ModInstalledWindow.xaml
    /// </summary>
    public partial class ModInstalledWindow : Window
    {
        bool _preventClose = true;
        private ModInstalledWindow()
        {
            InitializeComponent();
        }
        public static ModInstalledWindow GetDialog(string content, string title)
        {
            ModInstalledWindow window = new ModInstalledWindow()
            {
                Title = title
            };
            window.BodyTextBlock.Text = content;
            return window;
        }

        private void ModInstalledWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
            Activate();
            Topmost = true;
        }

        private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
        {
            _preventClose = false;
            Close();
        }
        private void ModInstalledWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = _preventClose;
        }
    }
}
