using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace ModAPI_Installers
{
    public static class Permissions
    {
        public static bool IsAdministrator()
        {
            /*if (Environment.OSVersion.Version.Major <= 5)
                return true;
            else
            {*/
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
            //}
        }

        public static Process RerunAsAdministrator()
        {
            return RerunAsAdministrator(GetProcessCommandLineArgs());
        }

        public static string GetProcessCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs().ToList();
            args.RemoveAt(0);
            string returnVal = string.Empty;
            foreach (string s in args)
                returnVal = returnVal + "\"" + s + "\" ";

            return returnVal;
        }

        public static Process RerunAsAdministrator(string args)
        {
            return RerunAsAdministrator(args, true);
        }

        public static Process RerunAsAdministrator(bool closeCurrent)
        {
            return RerunAsAdministrator(GetProcessCommandLineArgs(), closeCurrent);
        }

        public static Process RerunAsAdministrator(string args, bool closeCurrent)
        {
            //https://stackoverflow.com/questions/133379/elevating-process-privilege-programmatically/10905713
            var exeName = Process.GetCurrentProcess().MainModule.FileName;
            Process process = null;
            ProcessStartInfo startInfo = new ProcessStartInfo(exeName, args)
            {
                Verb = "runas"
            };
            try
            {
                //System.Windows.Forms.MessageBox.Show(args);
                process = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            if (closeCurrent && (process != null))
                Process.GetCurrentProcess().Kill();

            return process;
        }

        //https://stackoverflow.com/questions/9108399/how-to-grant-full-permission-to-a-file-created-by-my-application-for-all-users
        public static bool GrantAccess(string fullPath)
        {
            DirectoryInfo dInfo = new DirectoryInfo(fullPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl,
                                                             InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                             PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);
            return true;
        }
    }
}
