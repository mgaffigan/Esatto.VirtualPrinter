using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Esatto.Win32.CommonControls.Etw
{
    public abstract class NativeEventProvider
    {
        UInt64 traceRegistrationHandle;
        byte currentTraceLevel;
        UInt64 anyKeywordMask;
        UInt64 allKeywordMask;
        bool isProviderEnabled;
        Guid providerId;

        // this has to have a reference always on it for the lifetime of the object
        // as it can cause NullReferenceExceptions / AV when it is collected, and ETW
        // calls back
        UnsafeNativeMethods.EtwEnableCallback enableCallback;

        unsafe protected NativeEventProvider(Guid providerGuid)
        {
            this.providerId = providerGuid;

            enableCallback = new UnsafeNativeMethods.EtwEnableCallback(EtwEnableCallBack);
            uint etwRegistrationStatus = UnsafeNativeMethods.EventRegister(ref this.providerId,
                enableCallback, IntPtr.Zero, ref this.traceRegistrationHandle);

            if (etwRegistrationStatus != 0)
            {
                throw new InvalidOperationException("ETW Registration failed");
            }
        }

        ~NativeEventProvider()
        {
            this.isProviderEnabled = false;

            if (this.traceRegistrationHandle != 0)
            {
                UnsafeNativeMethods.EventUnregister(this.traceRegistrationHandle);
                this.traceRegistrationHandle = 0;
            }
        }

        unsafe private void EtwEnableCallBack(ref Guid sourceId, int isEnabled, byte setLevel,
            UInt64 anyKeyword, UInt64 allKeyword, void* filterData, void* callbackContext)
        {
            this.isProviderEnabled = (isEnabled != 0);
            this.currentTraceLevel = setLevel;
            this.anyKeywordMask = anyKeyword;
            this.allKeywordMask = allKeyword;
        }

        protected bool IsEnabled()
        {
            return this.isProviderEnabled;
        }

        protected bool IsEnabled(byte level, UInt64 keywords)
        {
            if (this.isProviderEnabled)
            {
                if ((level <= this.currentTraceLevel) ||
                    (this.currentTraceLevel == 0)) // This also covers the case of Level == 0.
                {
                    if ((keywords == 0) ||
                        (((keywords & this.anyKeywordMask) != 0) &&
                         ((keywords & this.allKeywordMask) == this.allKeywordMask)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected unsafe void WriteEventCore(
            System.Diagnostics.Eventing.EventDescriptor eventId,
            uint eventDataCount, EventData* data)
        {
            if (!isProviderEnabled)
                return;

            uint result = UnsafeNativeMethods.EventWrite(traceRegistrationHandle,
                ref eventId, eventDataCount, data);

            if (result != 0)
            {
                throw new InvalidOperationException("Exception while trying to write a message");
            }
        }

    }
}