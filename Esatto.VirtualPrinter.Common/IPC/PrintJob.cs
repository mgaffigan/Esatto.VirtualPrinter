using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.VirtualPrinter.IPC
{
    [ComVisible(true), Guid("2B920DD4-47CA-443E-9419-C1C0F2607D88")]
    [StructLayout(LayoutKind.Sequential)]
    public struct PrintJob
    {
        [MarshalAs(UnmanagedType.BStr)]
        public string PrinterName;

        public int JobId;

        public int SessionId;

        [MarshalAs(UnmanagedType.BStr)]
        public string UserSid;

        [MarshalAs(UnmanagedType.BStr)]
        public string DocumentName;

        [MarshalAs(UnmanagedType.BStr)]
        public string SpoolFilePath;
    }
}

