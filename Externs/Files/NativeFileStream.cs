using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.KtmIntegration;
using System.ComponentModel;

namespace Esatto.Win32.CommonControls.Files
{
    public static class NativeFileStream
    {
        public static FileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            NativeMethods.FileAccess nativeAccess =
                access == FileAccess.Read ? NativeMethods.FileAccess.GENERIC_READ :
                access == FileAccess.Write ? NativeMethods.FileAccess.GENERIC_WRITE :
                access == FileAccess.ReadWrite ? NativeMethods.FileAccess.GENERIC_READ | NativeMethods.FileAccess.GENERIC_WRITE : 0;
            
            var handle = NativeMethods.CreateFile(
                path,
                nativeAccess,
                (NativeMethods.FileShare)share,
                IntPtr.Zero,
                (NativeMethods.FileMode)mode,
                0, // Returns the directory handle
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                throw new Win32Exception();
            }

            return new FileStream(handle, access);
        }
    }
}
