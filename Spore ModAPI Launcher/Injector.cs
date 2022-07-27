using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;

namespace SporeModAPI_Launcher
{
    public class Injector
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize,
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        


        private const int WAIT_TIMEOUT = 0x102;
        private const uint MAXWAIT = 15000; //10000;


        public static void InjectDLL(PROCESS_INFORMATION pi, string dllPath)
        {
            IntPtr retLib = GetProcAddress(GetModuleHandle("Kernel32.dll"), "LoadLibraryA");

            if (retLib == IntPtr.Zero)
            {
                throw new InjectException("LoadLibrary unreachable.");
            }
            /*IntPtr hProc;
            if (Program.processHandle == IntPtr.Zero)*/
                IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.AccessRequired, false, pi.dwProcessId); //Open the process with all access
            //else hProc = Program.processHandle;
            // Allocate memory to hold the path to the DLL file in the process' memory
            IntPtr objPtr = VirtualAllocEx(hProc, IntPtr.Zero, (uint)dllPath.Length + 1, AllocationType.Commit, MemoryProtection.ReadWrite);
            if (objPtr == IntPtr.Zero)
            {
                int lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                System.Windows.Forms.MessageBox.Show("Error: " + lastError.ToString() + "\n" + "hProc: " + hProc.ToString() + "\nProgram.processHandle: " + (Program.processHandle == IntPtr.Zero), "Virtual alloc failure.");
                throw new System.ComponentModel.Win32Exception(lastError);
                //throw new InjectException("Virtual alloc failure.");
            }

            //Write the path to the DLL file in the location just created
            var bytes = new byte[dllPath.Length + 1];
            for (int i = 0; i < dllPath.Length; i++) {
                bytes[i] = (byte) dllPath[i];
            }
            bytes[dllPath.Length] = 0;

            UIntPtr numBytesWritten;
            Program.ERROR_TESTING_MSG("Beginning WriteProcessMemory");
            bool writeProcessMemoryOutput = WriteProcessMemory(hProc, objPtr, bytes, (uint)bytes.Length, out numBytesWritten);
            if (!writeProcessMemoryOutput || numBytesWritten.ToUInt32() != bytes.Length)
            {
                /*throw new InjectException("Write process memory failed.");*/
                Program.ThrowWin32Exception("Write process memory failed.");
            }
            Program.ERROR_TESTING_MSG("WriteProcessMemory output: " + writeProcessMemoryOutput.ToString());

            // Create a remote thread that begins at the LoadLibrary function and is passed as memory pointer
            IntPtr lpThreadId;
            IntPtr hRemoteThread = CreateRemoteThread(hProc, IntPtr.Zero, 0, retLib, objPtr, 0, out lpThreadId);

            // Wait for the thread to finish
            if (hRemoteThread != IntPtr.Zero)
            {
                if (WaitForSingleObject(hRemoteThread, MAXWAIT) == WAIT_TIMEOUT)
                {
                    //throw new InjectException("Wait for single object failed.");
                    Program.ThrowWin32Exception("Wait for single object failed. This usually occurs if something has become stuck during injection, or if another error was left open for too long.");
                }
            }
            else
            {
                //throw new InjectException("Create remote thread failed.");
                int lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                System.Windows.Forms.MessageBox.Show("Error: " + lastError.ToString(), "Create remote thread failed.");
                throw new System.ComponentModel.Win32Exception(lastError);
            }

            VirtualFreeEx(hProc, objPtr, (uint)bytes.Length, AllocationType.Release);

            //CloseHandle(pi.hProcess);
            CloseHandle(hProc);
        }
    }

    [Serializable()]
    public class InjectException : Exception
    {
        public InjectException() : base() { }
        public InjectException(string message) : base(message) { }
        public InjectException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected InjectException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }
}
