using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;

namespace ModApi.Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            /*EmbeddedAssembly.Load("ModApi.Updater.MddApi.UI.dll", "MddApi.UI.dll");

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);*/
            if (File.Exists(Environment.ExpandEnvironmentVariables(@"%appdata%\Spore ModAPI Launcher\WpfUseSoftwareRendering.info")))
                System.Windows.Media.RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            Start();
        }

        void Start()
        {
            /*if (!Permissions.IsAdministrator())
                Permissions.RerunAsAdministrator();*/

            var win = new MainWindow();
            MainWindow = (Window)win;
            win.Show();
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return EmbeddedAssembly.Get(args.Name);
        }
    }
}
