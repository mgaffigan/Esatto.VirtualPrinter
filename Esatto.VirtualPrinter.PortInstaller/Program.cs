using Esatto.VirtualPrinter.Win32.Printing;
using Esatto.Win32.Printing;
using System;
using System.IO;

namespace Esatto.VirtualPrinter.PortInstaller
{
    using static DriverConstants;

    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 3)
            {
                WriteUsage();
            }
            else
            {
                string command = args[0].ToLower();
                if (command == "install")
                {
                    RunInstall(args);
                }
                else if (command == "installdriver")
                {
                    RunInstallDriver(args);
                }
                else if (command == "uninstall")
                {
                    RunUninstall(args);
                }
                else
                {
                    WriteUsage();
                }
            }
        }

        private static void RunInstall(string[] args)
        {
            string dllName, displayName;
            if (args.Length == 1)
            {
                dllName = PortDllName;
                displayName = PortDisplayName;
            }
            else if (args.Length == 3)
            {
                dllName = args[1];
                displayName = args[2];
            }
            else
            {
                WriteUsage();
                return;
            }

            string path = Path.Combine(Environment.SystemDirectory, dllName);
            if (!File.Exists(path))
            {
                Console.WriteLine($"Port dll does not exist at '{path}', exiting.");
                return;
            }

            PortMonitors.AddPortMonitor(displayName, dllName, null, null);
        }

        private static void RunInstallDriver(string[] args)
        {
            string infName, driverName;
            if (args.Length == 1)
            {
                infName = DriverInfName;
                driverName = DriverModelName;
            }
            else if (args.Length == 2)
            {
                infName = args[1];
                driverName = DriverModelName;
            }
            else if (args.Length == 3)
            {
                infName = args[1];
                driverName = args[2];
            }
            else
            {
                WriteUsage();
                return;
            }

            string path = Path.Combine(Environment.CurrentDirectory, infName);
            if (!File.Exists(path))
            {
                Console.WriteLine($"Driver inf does not exist at '{path}', exiting.");
                return;
            }

            PrinterDriver.InstallInf(infName, driverName);
        }

        private static void RunUninstall(string[] args)
        {
            string displayName;
            if (args.Length == 1)
            {
                displayName = PortDisplayName;
            }
            else if (args.Length == 2)
            {
                displayName = args[1];
            }
            else
            {
                WriteUsage();
                return;
            }

            PortMonitors.RemovePortMonitor(displayName, null, null);
        }

        private static void WriteUsage()
        {
            Console.WriteLine("Usage: Esatto.VirtualPrinter.PortInstaller.exe install|uninstall");
            Console.WriteLine("           Install the Esatto Virtual Printer port");
            Console.WriteLine("       Esatto.VirtualPrinter.PortInstaller.exe install foo.dll \"display name\"");
            Console.WriteLine("           Install an arbitrary port monitor");
            Console.WriteLine("       Esatto.VirtualPrinter.PortInstaller.exe uninstall \"display name\"");
            Console.WriteLine("           Uninstall an arbitrary port monitor");
            Console.WriteLine();
            Console.WriteLine("Usage: Esatto.VirtualPrinter.PortInstaller.exe installdriver");
            Console.WriteLine("           Install the Esatto Virtual Printer driver");
            Console.WriteLine("       Esatto.VirtualPrinter.PortInstaller.exe installdriver foo.inf \"driver name\"");
            Console.WriteLine("           Install an arbitrary printer driver");
            Environment.Exit(-1);
        }
    }
}
