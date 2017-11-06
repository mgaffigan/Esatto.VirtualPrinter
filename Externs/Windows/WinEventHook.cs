﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    public sealed class WinEventHook : IDisposable, IObservable<WinEventEventArgs>
    {
        public event EventHandler<WinEventEventArgs> HookTriggered;
        private readonly NativeMethods.WinEventProc EventProcHandle;
        private GCHandle EventProcHandleGCRoot;
        private IntPtr Handle;
        private readonly Thread thCreated;
        private List<IObserver<WinEventEventArgs>> Observers;

        private readonly Process Process;
        private readonly int? ThreadID;
        private readonly WinEvent MinEvent;
        private readonly WinEvent MaxEvent;

        public WinEventHook(Process process, int? threadId, WinEvent minEvent, WinEvent maxEvent, HookOptions options = HookOptions.None)
        {
            this.thCreated = Thread.CurrentThread;
            this.Observers = new List<IObserver<WinEventEventArgs>>();
            this.Process = process;
            this.ThreadID = threadId;
            this.MinEvent = minEvent;
            this.MaxEvent = maxEvent;

            // EventProcHandle is the callback to CLR code, and must be valid.  We take a new GCRoot
            // and only release once a successfull Unhook occurs
            this.EventProcHandle = new NativeMethods.WinEventProc(Hook_EventProc);
            this.EventProcHandleGCRoot = GCHandle.Alloc(EventProcHandle);

            if (!options.HasFlag(HookOptions.StartDisabled))
            {
                StartInternal();
            }
        }

        public bool IsEnabled
        {
            get { return this.Handle != IntPtr.Zero; }
            set
            {
                if (IsEnabled != value)
                {
                    if (value)
                    {
                        StartInternal();
                    }
                    else
                    {
                        StopInternal();
                    }
                }
            }
        }

        private void StartInternal()
        {
            Contract.Assert(!IsEnabled);
            AssertThreading();

            this.Handle = NativeMethods.SetWinEventHook(MinEvent, MaxEvent, IntPtr.Zero,
                            EventProcHandle, Process?.Id ?? 0, ThreadID ?? 0,
                            NativeMethods.WinEventFlags.WINEVENT_OUTOFCONTEXT);
            if (Handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not create hook");
            }
        }

        private void StopInternal()
        {
            Contract.Assert(IsEnabled);
            AssertThreading();

            if (!NativeMethods.UnhookWinEvent(Handle))
            {
                throw new InvalidOperationException("Could not unhook");
            }
            Handle = IntPtr.Zero;
        }

        private void Hook_EventProc(IntPtr hWinEventHook, WinEvent @event, IntPtr hwnd,
            WinObject objectId, WinChild childId, int thread, uint time)
        {
            var args = new WinEventEventArgs(this, @event, hwnd, objectId, childId, thread, time);

            HookTriggered?.Invoke(this, args);
            foreach (var s in Observers)
            {
                s.OnNext(args);
            }
        }

        #region IObservable

        public IDisposable Subscribe(IObserver<WinEventEventArgs> observer)
        {
            AssertThreading();

            immutableMutate(newList => newList.Add(observer));

            return new SubscribedObserver(() =>
            {
                AssertThreading();

                immutableMutate(newList => newList.Remove(observer));
            });
        }

        private void immutableMutate(Action<List<IObserver<WinEventEventArgs>>> action)
        {
            while (true)
            {
                var oldObservers = Observers;
                var newObservers = new List<IObserver<WinEventEventArgs>>(oldObservers);
                action(newObservers);
                if (Interlocked.CompareExchange(ref Observers, newObservers, oldObservers) == oldObservers)
                {
                    break;
                }
            }
        }

        private sealed class SubscribedObserver : IDisposable
        {
            private Action DisposeAction;

            public SubscribedObserver(Action disposer)
            {
                this.DisposeAction = disposer;
            }

            public void Dispose() => DisposeAction();
        }

        #endregion

        #region IDisposable Support

        private bool isDisposed = false;

        public void Dispose()
        {
            // Unhook only works from original thread
            AssertThreading();
            if (isDisposed)
            {
                return;
            }

            IsEnabled = false;
            EventProcHandleGCRoot.Free();
            isDisposed = true;
        }

        #endregion

        private void AssertThreading()
        {
            if (Thread.CurrentThread != thCreated)
            {
                throw new InvalidOperationException("Invalid attempt to access hook from a different thread");
            }
        }
    }
}
