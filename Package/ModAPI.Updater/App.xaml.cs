using System;
using System.Windows;

namespace ModAPI.Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            try
            {
                Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void Start()
        {
            var win = new MainWindow();
            MainWindow = (Window)win;
            win.Show();
        }
    }
}
