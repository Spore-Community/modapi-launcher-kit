using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ModAPI.Common;

namespace SporeModAPI_Launcher
{
    public class Injector
    {
        private const int WAIT_TIMEOUT = 0x102;
        private const uint MAXWAIT = 15000; //10000;
        private const uint AccessRequired = 0x0002 | 0x0020 | 0x0008 | 0x0400 | 0x0010; //0x0002 | 0x0020 | 0x0008; //0xF0000 | 0x00100000 | 0xFFFF;

        public static IntPtr InjectDLL(NativeTypes.PROCESS_INFORMATION pi, string dllPath)
        {
            IntPtr retLib = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("Kernel32.dll"), "LoadLibraryA");

            if (retLib == IntPtr.Zero)
            {
                throw new InjectException("LoadLibrary unreachable.");
            }

            IntPtr hProc = NativeMethods.OpenProcess(AccessRequired, false, pi.dwProcessId); //Open the process with all access

            // Allocate memory to hold the path to the DLL file in the process' memory
            IntPtr objPtr = NativeMethods.VirtualAllocEx(hProc, IntPtr.Zero, (uint)dllPath.Length + 1, NativeTypes.AllocationType.Commit, NativeTypes.MemoryProtection.ReadWrite);
            if (objPtr == IntPtr.Zero)
            {
                Program.ThrowWin32Exception("Virtual alloc failure.");
            }

            //Write the path to the DLL file in the location just created
            var bytes = new byte[dllPath.Length + 1];
            for (int i = 0; i < dllPath.Length; i++) {
                bytes[i] = (byte) dllPath[i];
            }
            bytes[dllPath.Length] = 0;

            UIntPtr numBytesWritten;
            bool writeProcessMemoryOutput = NativeMethods.WriteProcessMemory(hProc, objPtr, bytes, (uint)bytes.Length, out numBytesWritten);
            if (!writeProcessMemoryOutput || numBytesWritten.ToUInt32() != bytes.Length)
            {
                Program.ThrowWin32Exception("Write process memory failed.");
            }

            // Create a remote thread that begins at the LoadLibrary function and is passed as memory pointer
            IntPtr hRemoteThread = NativeMethods.CreateRemoteThread(hProc, IntPtr.Zero, 0, retLib, objPtr, 0, out var lpThreadId);

            // Wait for the thread to finish
            uint thread_result;
            if (hRemoteThread != IntPtr.Zero)
            {
                if (NativeMethods.WaitForSingleObject(hRemoteThread, MAXWAIT) == WAIT_TIMEOUT)
                {
                    Program.ThrowWin32Exception("Wait for single object failed. This usually occurs if something has become stuck during injection, or if another error was left open for too long.");
                }
                while (NativeMethods.GetExitCodeThread(hRemoteThread, out thread_result))
                {
                    if (thread_result != 0x103)
                        break;
                    
                    Thread.Sleep(100);
                }
            }
            else
            {
                Program.ThrowWin32Exception("Create remote thread failed.");
                return IntPtr.Zero; // silence a warning
            }

            NativeMethods.VirtualFreeEx(hProc, objPtr, (uint)0, NativeTypes.AllocationType.Release);

            NativeMethods.CloseHandle(hProc);

            return new IntPtr((Int64)thread_result);
        }

        public static void SetInjectionData(NativeTypes.PROCESS_INFORMATION pi, IntPtr hDLLInjectorHandle, bool is_disc_spore, List<string> dlls)
        {
            IntPtr hLocalDLLInjectorHandle = NativeMethods.LoadLibraryEx("ModAPI.DLLInjector.dll", IntPtr.Zero, NativeTypes.LoadLibraryFlags.DONT_RESOLVE_DLL_REFERENCES);
            IntPtr SetInjectDataPtr = NativeMethods.GetProcAddress(hLocalDLLInjectorHandle, "SetInjectionData");

            if (SetInjectDataPtr == IntPtr.Zero)
            {
                string additionalErrorText = "\n" + "hDLLInjectorHandle: " + hDLLInjectorHandle.ToString() + "\nProgram.processHandle: " + (Program.processHandle == IntPtr.Zero);
                Program.ThrowWin32Exception("Get proc address failure.", additionalErrorText);
            }

            SetInjectDataPtr = new IntPtr(hDLLInjectorHandle.ToInt64() + (SetInjectDataPtr.ToInt64() - hLocalDLLInjectorHandle.ToInt64()));

            if (!NativeMethods.FreeLibrary(hLocalDLLInjectorHandle))
            {
                Program.ThrowWin32Exception("Free library failed.");
            }

            IntPtr hProc = NativeMethods.OpenProcess(AccessRequired, false, pi.dwProcessId); //Open the process with all access

            int total_alloc_size = 1 + 4; //1 byte for if we are disc spore, 4 bytes for number of strings
            foreach (string dll in dlls)
            {
                total_alloc_size += 4 + Encoding.Unicode.GetByteCount(dll); //4 bytes for string length + string
            }

            IntPtr objPtr = NativeMethods.VirtualAllocEx(hProc, IntPtr.Zero, (uint)total_alloc_size, NativeTypes.AllocationType.Commit, NativeTypes.MemoryProtection.ReadWrite);
            if (objPtr == IntPtr.Zero)
            {
                string additionalErrorText = "\n" + "hProc: " + hProc.ToString() + "\nProgram.processHandle: " + (Program.processHandle == IntPtr.Zero);
                Program.ThrowWin32Exception("Virtual alloc failure.", additionalErrorText);
            }
            
            //write injection data
            var bytes = new byte[total_alloc_size];
            int byte_offset = 0;
            bytes[byte_offset++] = (byte)(is_disc_spore ? 1 : 0);
            foreach (byte b in BitConverter.GetBytes((uint)dlls.Count))
                bytes[byte_offset++] = b;

            foreach (string dll in dlls)
            {
                foreach (byte b in BitConverter.GetBytes((uint)dll.Length))
                    bytes[byte_offset++] = b;
                byte[] encoding = Encoding.Unicode.GetBytes(dll);
                foreach (byte t in encoding)
                    bytes[byte_offset++] = t;
            }


            bool writeProcessMemoryOutput = NativeMethods.WriteProcessMemory(hProc, objPtr, bytes, (uint)bytes.Length, out var numBytesWritten);
            if (!writeProcessMemoryOutput || numBytesWritten.ToUInt32() != bytes.Length)
            {
                Program.ThrowWin32Exception("Write process memory failed.");
            }

            // Create a remote thread that begins at the LoadLibrary function and is passed as memory pointer
            IntPtr hRemoteThread = NativeMethods.CreateRemoteThread(hProc, IntPtr.Zero, 0, SetInjectDataPtr, objPtr, 0, out var lpThreadId);

            // Wait for the thread to finish
            if (hRemoteThread != IntPtr.Zero)
            {
                if (NativeMethods.WaitForSingleObject(hRemoteThread, MAXWAIT) == WAIT_TIMEOUT)
                {
                    Program.ThrowWin32Exception("Wait for single object failed. This usually occurs if something has become stuck during injection, or if another error was left open for too long.");
                }
                while (NativeMethods.GetExitCodeThread(hRemoteThread, out var thread_result))
                {
                    if (thread_result != 0x103)
                        break;                    
                    Thread.Sleep(100);
                }
            }
            else
            {
                Program.ThrowWin32Exception("Create remote thread failed.");
            }

            NativeMethods.VirtualFreeEx(hProc, objPtr, (uint)0, NativeTypes.AllocationType.Release);

            NativeMethods.CloseHandle(hProc);
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
