using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Esatto.Win32.CommonControls.Etw
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct EventData
    {
        [FieldOffset(0)]
        public void* DataPointer;
        [FieldOffset(8)]
        public int Size;
        [FieldOffset(12)]
        public int Reserved;
    }
}
