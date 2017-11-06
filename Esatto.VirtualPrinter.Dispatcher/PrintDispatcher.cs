using Esatto.VirtualPrinter;
using Esatto.VirtualPrinter.IPC;
using Esatto.Win32.Com;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using WpfDispatcher = System.Windows.Threading.Dispatcher;

namespace Esatto.VirtualPrinter.Dispatcher
{
    using static Win32.Com.NativeMethods;

    [ComVisible(true), ClassInterface(ClassInterfaceType.None), ProgId(IpcConstants.DispatcherProgId)]
    public sealed class PrintDispatcher : StandardOleMarshalObject, IDisposable, IPrintDispatcher
    {
        private readonly WpfDispatcher ThreadDispatcher;
        private readonly IPrintRedirector Redirector;

        private bool IsInPing;
        private readonly Stopwatch stpLastPing;
        private readonly Timer tmrIdleTimeout;
        private readonly Timer tmrPingOverrideTargets;

        private readonly Dictionary<string, IPrintTarget> OverrideTargets;

        #region ctor dtor

        public PrintDispatcher()
        {
            // required for regasm, not actually used.
        }

        internal PrintDispatcher(WpfDispatcher dispatcher)
        {
            Contract.Requires(dispatcher != null, nameof(dispatcher));

            this.ThreadDispatcher = dispatcher;
            this.OverrideTargets = new Dictionary<string, IPrintTarget>(StringComparer.InvariantCultureIgnoreCase);
            this.stpLastPing = Stopwatch.StartNew();

            // connect to the parent redirector
            this.Redirector = ((IPrintRedirector)ComInterop.CreateLocalServer(IpcConstants.RedirectorProgId));

            // start our ping monitoring timers
            this.tmrIdleTimeout = new Timer((_2) => dispatcher.BeginInvoke(new Action(this.tmrIdleTimeout_Tick)), null, IpcConstants.IdleTimeout, Timeout.InfiniteTimeSpan);
            this.tmrPingOverrideTargets = new Timer((_2) => dispatcher.BeginInvoke(new Action(this.tmrPingOverrideTarget_Tick)), null, Timeout.Infinite, Timeout.Infinite);
        }

        internal void Register()
        {
            this.Redirector.Register(this);
        }

        public void Dispose()
        {
            this.tmrIdleTimeout.Dispose();
            this.tmrPingOverrideTargets.Dispose();
            try
            {
                this.Redirector.Unregister(this);
                Marshal.ReleaseComObject(this.Redirector);
            }
            catch (Exception exception)
            {
                Log.Warn($"Exception while unregistering dispatcher:\r\n\r\n{exception}", 1043);
            }
        }

        #endregion

        #region HandleJob

        void IPrintDispatcher.HandleJob(PrintJob job)
        {
            ThreadDispatcher.VerifyAccess();

            IPrintTarget overrideTarget;
            this.OverrideTargets.TryGetValue(job.PrinterName, out overrideTarget);

            // try override target if registered
            if (overrideTarget != null)
            {
                try
                {
                    overrideTarget.HandleJob(job);
                    return;
                }
                catch (Exception exception)
                {
                    Log.Warn($"Could not deliver print job for printer '{job.PrinterName}' to connected override.\r\n\r\nException:\r\n{exception}", 10945);
                }
            }

            // try default handler
            try
            {
                using (var config = new VirtualPrinterSystemConfiguration(VirtualPrinterConfigurationAccessLevel.ReadOnly))
                {
                    var printerConfig = config.Printers[job.PrinterName];
                    printerConfig.GetHandler().HandleJob(job);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not deliver print job for printer '{job.PrinterName}' to default handler.\r\n\r\nException:\r\n{ex}", 14559);
            }
        }

        #endregion

        #region Override targets

        void IPrintDispatcher.SetTarget(string printQueue, IPrintTarget target)
        {
            Contract.Assert(target != null, "target != null");
            ThreadDispatcher.VerifyAccess();

            this.OverrideTargets[printQueue] = target;
            this.RescheduleOverridePing();
        }

        void IPrintDispatcher.UnsetTarget(IPrintTarget oldTarget)
        {
            Contract.Assert(oldTarget != null, "target != null");
            ThreadDispatcher.VerifyAccess();

            this.RemoveOverrideTargetInternal(oldTarget);
            this.RescheduleOverridePing();
        }

        private void RemoveOverrideTargetInternal(IPrintTarget oldTarget)
        {
            foreach (var kvp in this.OverrideTargets.Where(k => k.Value == oldTarget).ToArray())
            {
                this.OverrideTargets.Remove(kvp.Key);
            }
        }

        #endregion

        #region Pings

        private void tmrIdleTimeout_Tick()
        {
            ThreadDispatcher.VerifyAccess();

            Log.Warn($"Shutting down dispatcher after {this.stpLastPing.Elapsed}", 0x414);
            Application.Current.Shutdown();
        }

        private void tmrPingOverrideTarget_Tick()
        {
            ThreadDispatcher.VerifyAccess();

            Contract.Assert(!IsInPing, "!IsInPing");
            this.IsInPing = true;
            var pendingPing = this.OverrideTargets.Values.ToList();

            var deadTargets = new List<IPrintTarget>();
            foreach (var target in pendingPing)
            {
                // a single target may register for multiple printers
                if (deadTargets.Contains(target))
                {
                    continue;
                }

                bool succeeded = false;
                try
                {
                    target.Ping();
                    succeeded = true;
                }
                catch (COMException exception) when ((exception.HResult == RPC_S_SERVER_UNAVAILABLE))
                {
                    Log.Debug("Override target disconnected without unregistering, collecting...");
                }
                catch (Exception exception2)
                {
                    Log.Error($"Unknown exception while pinging override target\r\n\r\n{exception2}", 1460);
                }

                if (!succeeded)
                {
                    deadTargets.Add(target);
                }
            }

            foreach (var dead in deadTargets)
            {
                this.RemoveOverrideTargetInternal(dead);
            }

            this.IsInPing = false;
            this.RescheduleOverridePing();
        }

        void IPrintDispatcher.Ping()
        {
            this.stpLastPing.Restart();
            this.tmrIdleTimeout.Change(IpcConstants.IdleTimeout, Timeout.InfiniteTimeSpan);
        }

        private void RescheduleOverridePing()
        {
            if (this.IsInPing)
            {
                return;
            }

            if (this.OverrideTargets.Any())
            {
                this.tmrPingOverrideTargets.Change(IpcConstants.PrintTargetPingInterval, Timeout.InfiniteTimeSpan);
            }
            else
            {
                this.tmrPingOverrideTargets.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        #endregion

        #region Registration

        [ComRegisterFunction]
        internal static void RegasmRegisterLocalServer(string path)
        {
            // path is HKEY_CLASSES_ROOT\\CLSID\\{clsid}", we only want CLSID...
            path = path.Substring(@"HKEY_CLASSES_ROOT\".Length);
            using (RegistryKey keyCLSID = Registry.ClassesRoot.OpenSubKey(path, true))
            {
                // Remove the auto-generated InprocServer32 key after registration
                // (REGASM puts it there but we are going out-of-proc).
                keyCLSID.DeleteSubKeyTree("InprocServer32");

                // add server registration
                using (RegistryKey rkLocalServer = keyCLSID.CreateSubKey("LocalServer32"))
                {
                    rkLocalServer.SetValue(null, Assembly.GetExecutingAssembly().Location);
                }
            }
        }

        [ComUnregisterFunction]
        internal static void RegasmUnregisterLocalServer(string path)
        {
            // path is HKEY_CLASSES_ROOT\\CLSID\\{clsid}", we only want CLSID...
            path = path.Substring(@"HKEY_CLASSES_ROOT\".Length);

            Registry.ClassesRoot.DeleteSubKeyTree(path, false);
        }

        #endregion
    }
}

