using Esatto.Win32.Printing;
using Microsoft.Win32;
using System;
using System.IO;
using System.Printing;
using System.Printing.IndexedProperties;

namespace Esatto.VirtualPrinter.PortInstaller;

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
            else if (command == "addprinter")
            {
                RunAddPrinter(args);
            }
            else if (command == "removeprinter")
            {
                RunRemovePrinter(args);
            }
            else
            {
                WriteUsage();
            }
        }
    }

    private static void RunAddPrinter(string[] args)
    {
        var printerName = args[1];
        var regBase = "HKEY_LOCAL_MACHINE\\SOFTWARE\\In Touch Technologies\\Esatto\\Virtual Printer\\Printers\\" + printerName;
        if (args.Length > 2)
        {
            Registry.SetValue(regBase, "TargetExe", args[2]);
        }
        if (args.Length > 3)
        {
            Registry.SetValue(regBase, "TargetArgs", args[3]);
        }

        var printServer = new LocalPrintServer();
        var initialParameters = new PrintPropertyDictionary();
        var printQueue = printServer.InstallPrintQueue(printerName,
            DriverModelName, [PortName], "WinPrint", initialParameters);
    }

    private static void RunRemovePrinter(string[] args)
    {
        var printerName = args[1];
        var printServer = new LocalPrintServer();
        var printQueue = printServer.GetPrintQueue(printerName)
            ?? throw new FileNotFoundException("Queue not found");
        if (!PrintServer.DeletePrintQueue(printQueue))
        {
            throw new InvalidOperationException("Failed to delete queue");
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
        Console.WriteLine("       Esatto.VirtualPrinter.PortInstaller.exe installdriver");
        Console.WriteLine("           Install the Esatto Virtual Printer driver");
        Console.WriteLine("       Esatto.VirtualPrinter.PortInstaller.exe installdriver foo.inf \"driver name\"");
        Console.WriteLine("           Install an arbitrary printer driver");
        Console.WriteLine();
        Console.WriteLine("       Esatto.VirtualPrinter.PortInstaller.exe addprinter \"Printer Name\" [\"c:\\path\\to\\target.exe\"]");
        Console.WriteLine("           Adds an Esatto Virtual Printer queue");
        Console.WriteLine();
        Console.WriteLine("       Esatto.VirtualPrinter.PortInstaller.exe removeprinter \"Printer Name\"");
        Console.WriteLine("           Removes a print queue");
        Environment.Exit(-1);
    }
}