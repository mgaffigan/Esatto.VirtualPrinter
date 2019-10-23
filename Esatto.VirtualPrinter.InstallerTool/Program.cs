using Esatto.VirtualPrinter;
using System;
using System.Linq;

namespace Esatto.VirtualPrinter.InstallerTool
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                WriteUsage();
            }
            else
            {
                string command = args[0].ToLower();
                if (command == "list")
                {
                    RunList();
                }
                else if (command == "add")
                {
                    RunAdd(args);
                }
                else if (command == "remove")
                {
                    RunRemove(args);
                }
                else
                {
                    WriteUsage();
                }
            }
        }

        private static void RunAdd(string[] args)
        {
            if ((args.Length != 3) && (args.Length != 4))
            {
                WriteUsage();
                return;
            }

            string queueName = args[1];
            string handlerType = args[2];
            string handlerCodebase = args.Length > 3 ? args[3] : null;
            if (string.IsNullOrWhiteSpace(queueName) || string.IsNullOrWhiteSpace(handlerType))
            {
                WriteUsage();
                return;
            }

            using (var config = new VirtualPrinterSystemConfiguration(VirtualPrinterConfigurationAccessLevel.ReadWrite))
            {
                config.Printers.Add(queueName, handlerType, handlerCodebase, null, null);
            }
        }

        private static void RunList()
        {
            using (var config = new VirtualPrinterSystemConfiguration(VirtualPrinterConfigurationAccessLevel.ReadOnly))
            {
                foreach (var queue in config.Printers)
                {
                    Console.WriteLine("Name:    \"{0}\"", queue.Name);
                    Console.WriteLine("Type:    \"{0}\"", queue.HandlerTypeName);
                    if (queue.HandlerCodebase != null)
                    {
                        Console.WriteLine("         \"{0}\"", queue.HandlerCodebase);
                    }
                    Console.WriteLine();
                }
            }
        }

        private static void RunRemove(string[] args)
        {
            if (args.Length != 2)
            {
                WriteUsage();
                return;
            }

            var queueName = args[1];
            if (string.IsNullOrWhiteSpace(queueName))
            {
                WriteUsage();
                return;
            }

            using (var config = new VirtualPrinterSystemConfiguration(VirtualPrinterConfigurationAccessLevel.ReadWrite))
            {
                var queue = config.Printers[queueName];
                config.Printers.Remove(queue);
            }
        }

        private static void WriteUsage()
        {
            Console.Error.WriteLine("Usage: InstallerTool {Command} {Options}");
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Commands:");
            Console.Error.WriteLine("    list                      List all configured printers");
            Console.Error.WriteLine("    add [name] [type]         Adds a printer named [name] handled by [type]");
            Console.Error.WriteLine("    add [name] [type] [path]  Adds a printer named [name] handled by [type]");
            Console.Error.WriteLine("                                which has been loaded from [path]");
            Console.Error.WriteLine("    remove [name]             Removes a printer named [name]");
            Environment.Exit(1);
        }
    }
}

