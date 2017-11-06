using Esatto.VirtualPrinter;
using Esatto.Win32.Com;
using System;
using System.ServiceProcess;

namespace Esatto.VirtualPrinter.Redirector
{
    internal sealed class RedirectorService : ServiceBase
    {
        private PrintRedirector Redirector;
        private ClassObjectRegistration RedirectorRegistration;
        public const string SERVICE_NAME = "esVirtualPrinterRedirector";

        public RedirectorService()
        {
            base.ServiceName = SERVICE_NAME;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            try
            {
                this.Redirector = new PrintRedirector();

                // add to system ROT
                this.RedirectorRegistration = new ClassObjectRegistration(
                    typeof(PrintRedirector).GUID, ComInterop.CreateClassFactoryFor(() => this.Redirector), 
                    CLSCTX.LOCAL_SERVER, REGCLS.MULTIPLEUSE | REGCLS.SUSPENDED);
                ComInterop.CoResumeClassObjects();
            }
            catch (Exception exception)
            {
                Log.Error($"Could not start esVirtualPrinterRedirector\r\n{exception}", 1001);
                throw;
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            try
            {
                this.RedirectorRegistration.Dispose();
                this.Redirector.Dispose();
            }
            catch (Exception exception)
            {
                Log.Error($"Could not stop esVirtualPrinterRedirector\r\n{exception}", 1002);
                throw;
            }
        }
    }
}

