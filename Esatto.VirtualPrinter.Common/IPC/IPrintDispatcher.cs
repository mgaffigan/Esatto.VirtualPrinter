using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.VirtualPrinter.IPC
{
    // Driver -> Redirector --cross session--> dispatcher -> target
    [ComVisible(true), Guid("2E787709-7BD0-4BF9-A6EE-9BC4D9D095E4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPrintDispatcher
    {
        void SetTarget(string printQueueName, IPrintTarget target);

        void UnsetTarget(IPrintTarget oldTarget);

        void Ping();

        void HandleJob(PrintJob job);
    }
}

