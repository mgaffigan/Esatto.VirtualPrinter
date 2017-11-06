using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.VirtualPrinter
{
    public static class DriverConstants
    {
        // All changes must be mirrored in DriverConstants.h and esattovp2.inf
        public const string DispatcherFilename = "Esatto.VirtualPrinter.Dispatcher.exe";
        public const string DriverManufacturer = "In Touch Pharmaceuticals";
        public const string DriverModelName = "Esatto Virtual Printer v2";
        public const string PortDescription = "Esatto Virtual Printer Port";
        public const string PortDisplayName = "Esatto Virtual Printer Port";
        public const string PortDllName = "EsPortMon2.dll";
        public const string PortName = "ESVP2:";
    }
}

