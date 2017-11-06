using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.CommonControls.Etw
{
    class UnsafeNativeMethods
    {
        public const String ADVAPI32 = "advapi32.dll";

        public const int ERROR_INVALID_HANDLE = 6;
        public const int ERROR_MORE_DATA = 234;
        public const int ERROR_ARITHMETIC_OVERFLOW = 534;
        public const int ERROR_NOT_ENOUGH_MEMORY = 8;

        internal unsafe delegate void EtwEnableCallback(
            [In] ref Guid sourceId,
            [In] int isEnabled,
            [In] byte level,
            [In] UInt64 matchAnyKeywords,
            [In] UInt64 matchAllKeywords,
            [In] void* filterData,
            [In] void* callbackContext);


        [DllImport(ADVAPI32, ExactSpelling = true, EntryPoint = "EventRegister", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern unsafe uint EventRegister(
            [In] ref Guid providerId,
            [In] EtwEnableCallback enableCallback,
            [In] IntPtr callbackContext,
            [In, Out] ref UInt64 registrationHandle);

        [DllImport(ADVAPI32, ExactSpelling = true, EntryPoint = "EventUnregister", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern uint EventUnregister([In] UInt64 registrationHandle);

        [DllImport(ADVAPI32, ExactSpelling = true, EntryPoint = "EventWrite", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern unsafe uint EventWrite(
            [In] UInt64 registrationHandle,
            [In] ref System.Diagnostics.Eventing.EventDescriptor eventDescriptor,
            [In] uint userDataCount,
            [In] EventData* userData);
    }
}
