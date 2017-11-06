using System;
using System.Printing;

namespace Esatto.VirtualPrinter
{
    public enum VirtualPrinterConfigurationAccessLevel
    {
        ReadOnly = 0,
        ReadWrite = 1
    }

    internal static class VirtualPrinterConfigurationAccessLevelExtensions
    {
        public static PrintSystemDesiredAccess ToPrintSystemAccess(this VirtualPrinterConfigurationAccessLevel access)
        {
            switch (access)
            {
                case VirtualPrinterConfigurationAccessLevel.ReadOnly:
                    return PrintSystemDesiredAccess.EnumerateServer;

                case VirtualPrinterConfigurationAccessLevel.ReadWrite:
                    return PrintSystemDesiredAccess.AdministrateServer;

                default: throw new ArgumentOutOfRangeException("access");
            }
        }
    }
}

