using Esatto.VirtualPrinter.IPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Esatto.VirtualPrinter.PrintToFileTarget
{
    class Program
    {
        // we don't pump messages, so we must be MTA
        [MTAThread]
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return;
            }

            string queueName = args[0];
            var filePath = args[1];

            var mreEnd = new ManualResetEventSlim();
            var handler = new Action<PrintJob>((job) => 
            {
                File.Copy(job.SpoolFilePath, filePath);
                mreEnd.Set();
            });

            using (new PrintTargetRegistration(queueName, handler, null))
            {
                mreEnd.Wait();
            }
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine("Usage: PrintToFileTarget.exe [print queue name] [targetfile.xps]");
            Environment.Exit(1);
        }
    }
}
