using Esatto.Win32.Com;
using System;
using System.ServiceProcess;

namespace Esatto.VirtualPrinter.Redirector
{
    public static class Program
    {
        [MTAThread]
        public static void Main(string[] args)
        {
            ComInterop.SetAppId(Guid.Parse(PrintRedirector.OurAppID));
            ServiceBase.Run(new RedirectorService());
        }
    }
}

