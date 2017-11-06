using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.VirtualPrinter.IPC
{
    public static class IpcConstants
    {
        public static readonly TimeSpan PrintTargetPingInterval = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan DispatcherPingInterval = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);

        public const string DispatcherProgId = "Esatto.VirtualPrinter.Dispatcher";
        public const string RedirectorProgId = "Esatto.VirtualPrinter.Redirector";
    }
}

