using Esatto.VirtualPrinter.IPC;
using Esatto.Win32.Com;
using System;

namespace Esatto.VirtualPrinter
{
    public static class SessionPrintDispatcher
    {
        public static IPrintDispatcher GetCurrent() =>
            ((IPrintDispatcher)ComInterop.CreateLocalServer(IpcConstants.DispatcherProgId));
    }
}

