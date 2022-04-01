using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    public static class WindowFocus
    {
        public static void CoAllowSetForegroundWindow(object comProxy)
        {
            Com.NativeMethods.CoAllowSetForegroundWindow(comProxy, IntPtr.Zero);
        }

        public static void CoAllowSetForegroundWindowSafe(object comProxy)
        {
            try
            {
                CoAllowSetForegroundWindow(comProxy);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CoAllowSetForegroundWindow failed\r\n{ex}");
            }
        }

        public static void AllowSetForegroundWindow()
            => AllowSetForegroundWindow(Com.NativeMethods.ASFW_ANY);

        private static void AllowSetForegroundWindow(int pid) 
            => Com.NativeMethods.AllowSetForegroundWindow(pid);
    }
}
