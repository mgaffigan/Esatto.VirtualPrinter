using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    public static class ProcessExtensions
    {
        public static void AllowSetForegroundWindow(this Process process)
        {
            Contract.Requires(process != null);

            if (!NativeMethods.AllowSetForegroundWindow(process.Id))
            {
                throw new Win32Exception();
            }
        }
    }
}
