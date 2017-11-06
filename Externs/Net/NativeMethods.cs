using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Esatto.Win32.CommonControls.Net
{
    class NativeMethods
    {
        const string KERNEL32 = "kernel32.dll";
        const int ERROR_MORE_DATA = 234;

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetComputerNameEx(
            [In] ComputerNameFormat nameType,
            [In, Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpBuffer,
            [In, Out] ref int size);

        public static string GetComputerName(ComputerNameFormat nameType)
        {
            int length = 0;
            if (!GetComputerNameEx(nameType, null, ref length))
            {
                int error = Marshal.GetLastWin32Error();

                if (error != ERROR_MORE_DATA)
                {
                    throw new System.ComponentModel.Win32Exception(error);
                }
            }

            if (length < 0)
            {
                throw new InvalidOperationException("GetComputerName returned an invalid length: " + length);
            }

            StringBuilder stringBuilder = new StringBuilder(length);
            if (!GetComputerNameEx(nameType, stringBuilder, ref length))
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error);
            }

            return stringBuilder.ToString();
        }

        public enum ComputerNameFormat
        {
            NetBIOS,
            DnsHostName,
            Dns,
            DnsFullyQualified,
            PhysicalNetBIOS,
            PhysicalDnsHostName,
            PhysicalDnsDomain,
            PhysicalDnsFullyQualified
        }
    }
}
