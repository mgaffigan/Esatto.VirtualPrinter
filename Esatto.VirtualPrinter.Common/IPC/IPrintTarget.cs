using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.VirtualPrinter.IPC
{
    // Driver -> Redirector --cross session--> dispatcher -> target
    [ComVisible(true), Guid("A48B942F-74FA-489D-9AD6-4D7056577A55"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPrintTarget
    {
        void HandleJob(PrintJob job);

        void Ping();
    }
}

