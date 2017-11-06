using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Com
{
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using static NativeMethods;

#if ESATTO_WIN32
    public
#else
    internal
#endif
        sealed class RotRegistration : IDisposable
    {
        private IRunningObjectTable rot;
        private readonly int hRotEntry;
        private object Target;
        private bool isDisposed;

        public RotRegistration(string moniker, object o)
        {
            Contract.Requires(!String.IsNullOrEmpty(moniker));
            Contract.Requires(o != null);

            this.Target = o;

            rot = GetRunningObjectTable(0);
            var imoniker = CreateItemMoniker("!", moniker);
            hRotEntry = rot.Register(ROTFLAGS_REGISTRATIONKEEPSALIVE, o, imoniker);
        }

        public static object GetRegisteredObject(string moniker)
        {
            Contract.Requires(!String.IsNullOrEmpty(moniker));

            var rot = GetRunningObjectTable(0);
            var imoniker = CreateItemMoniker("!", moniker);

            object utobj;
            var hr = rot.GetObject(imoniker, out utobj);
            if (hr == MK_E_UNAVAILABLE)
            {
                throw new KeyNotFoundException();
            }
            if (hr != S_OK)
            {
                throw Marshal.GetExceptionForHR(hr);
            }
            return utobj;
        }

        #region IDisposable Support

        void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;

            try
            {
                rot.Revoke(hRotEntry);
            }
            catch (Exception) when (!disposing)
            {
                // no-op, on finalizer
            }
            this.rot = null;
            this.Target = null;
        }

        ~RotRegistration()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
