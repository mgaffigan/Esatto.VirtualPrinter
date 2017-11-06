using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.VirtualPrinter.IPC
{
    // Driver -> Redirector --cross session--> dispatcher -> target
    [ComVisible(true), Guid("2AC5EFB0-BD66-44A7-9711-F170AC58D564"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPrintRedirector
    {
        void HandleJob(PrintJob job);

        void Register(IPrintDispatcher dispatcher);

        void Unregister(IPrintDispatcher dispatcher);
    }
}

