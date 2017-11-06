using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace Esatto.Win32.Com
{
    using static ComInterop;
    using static NativeMethods;

    internal sealed class StaClassFactory : IClassFactory
    {
        private readonly Func<object> Constructor;
        private readonly Type ClassType;
        private readonly Dictionary<Guid, Type> InterfaceMap;

        public StaClassFactory(Type tClass, Func<object> constructor)
        {
            Contract.Requires(constructor != null);
            Contract.Requires(tClass != null);
            Contract.Requires(tClass.IsClass);

            this.Constructor = constructor;
            this.ClassType = tClass;

            this.InterfaceMap = new Dictionary<Guid, Type>();
            foreach (var tInt in tClass.GetInterfaces())
            {
                InterfaceMap.Add(tInt.GUID, tInt);
            }
        }

        IntPtr IClassFactory.CreateInstance(IntPtr pUnkOuter, Guid riid)
        {
            if (pUnkOuter != IntPtr.Zero)
            {
                throw GetNoAggregationException();
            }

            var instance = Constructor();
            if (instance == null)
            {
                throw new InvalidOperationException("Constructor returned null");
            }

            Type tInterface;
            if (riid == IID_IUnknown)
            {
                return Marshal.GetIUnknownForObject(instance);
            }
            else if (riid == IID_IDispatch)
            {
                return Marshal.GetIDispatchForObject(instance);
            }
            else if (InterfaceMap.TryGetValue(riid, out tInterface))
            {
                return Marshal.GetComInterfaceForObject(instance, tInterface);
            }
            else throw GetNoInterfaceException();
        }

        void IClassFactory.LockServer(bool fLock)
        {
            // no-op
        }
    }
}
