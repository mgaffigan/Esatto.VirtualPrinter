using Esatto.VirtualPrinter;
using Esatto.Win32.Com;
using System;
using System.Windows;

namespace Esatto.VirtualPrinter.Dispatcher
{
    internal sealed class PrintDispatcherApplication : Application
    {
        private PrintDispatcher PrintDispatcher;
        private ClassObjectRegistration PrintDispatcherRegistration;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                this.PrintDispatcher = new PrintDispatcher(this.Dispatcher);

                // add to ROT
                this.PrintDispatcherRegistration = new ClassObjectRegistration(
                    typeof(PrintDispatcher).GUID, ComInterop.CreateClassFactoryFor(() => this.PrintDispatcher), 
                    CLSCTX.LOCAL_SERVER, REGCLS.MULTIPLEUSE | REGCLS.SUSPENDED);
                ComInterop.CoResumeClassObjects();

                // register to redirector
                this.PrintDispatcher.Register();
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to start virutal printer dispatcher:\r\n{exception}", 105);
                base.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                PrintDispatcherRegistration?.Dispose();
                PrintDispatcher?.Dispose();
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to stop virutal printer dispatcher:\r\n{exception}", 103);
            }

            base.OnExit(e);
        }
    }
}

