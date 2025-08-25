using EnvDTE80;
using ModAPI.Common;
using System;
using System.Collections;
using System.Runtime.InteropServices.ComTypes;

namespace SporeModAPI_Launcher
{
    //https://www.codeproject.com/Articles/7984/Automating-a-specific-instance-of-Visual-Studio-NE
    public class DebugHelper
    {
        private static Hashtable GetRunningObjectTable()
        {
            Hashtable result = new Hashtable();

            IRunningObjectTable runningObjectTable;   
            IEnumMoniker monikerEnumerator;
            IMoniker[] monikers = new IMoniker[1];

            NativeMethods.GetRunningObjectTable(0, out runningObjectTable);    
            runningObjectTable.EnumRunning(out monikerEnumerator);
            monikerEnumerator.Reset();          
    
            while (monikerEnumerator.Next(1, monikers, IntPtr.Zero) == 0)
            {     
                IBindCtx ctx;
                NativeMethods.CreateBindCtx(0, out ctx);     
            
                string runningObjectName;
                monikers[0].GetDisplayName(ctx, null, out runningObjectName);

                object runningObjectVal;  
                runningObjectTable.GetObject( monikers[0], out runningObjectVal); 

                result[ runningObjectName ] = runningObjectVal;
            } 

            return result;
        }

        public static DTE2 GetActiveDebugger()
        {
            int pid = System.Diagnostics.Process.GetCurrentProcess().Id;

            Hashtable runningObjects = GetRunningObjectTable();
            IDictionaryEnumerator rotEnumerator = runningObjects.GetEnumerator();
            while (rotEnumerator.MoveNext())
            {
                string candidateName = (string) rotEnumerator.Key;
                if (!candidateName.StartsWith("!VisualStudio.DTE"))
                    continue;

                DTE2 ide = rotEnumerator.Value as DTE2;
                if (ide == null)
                    continue;

                foreach(EnvDTE.Process proc in ide.Debugger.DebuggedProcesses)
                {
                    if (proc.ProcessID == pid)
                    {
                        return ide;   
                    }
                }
            }

            return null;
        }
    }
}
