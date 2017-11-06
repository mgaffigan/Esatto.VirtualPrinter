#define DEBUG
// define DEBUG to ensure that calls to System.Diagnostics.Debug are actually made

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SDD = System.Diagnostics.Debug;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.VirtualPrinter
{
    public static class Log
    {
        private static EventLog EventLog;

        static Log()
        {
            EventLog = new EventLog();
            EventLog.Source = "esVirtualPrinter";
        }

        public static void Debug(string message)
        {
            try
            {
                EventLog.WriteEntry(message, EventLogEntryType.SuccessAudit, 0);
            }
            catch (Exception exception)
            {
                SDD.WriteLine("Failed to log message:\r\n" + exception.ToString());
            }
            SDD.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss.ffff}-Debug-0: {message}");
        }

        public static void Error(string message, int eventid)
        {
            LogMessage(EventLogEntryType.Error, message, eventid);
        }

        public static void Info(string message, int eventid)
        {
            LogMessage(EventLogEntryType.Information, message, eventid);
        }

        private static void LogMessage(EventLogEntryType type, string message, int eventid)
        {
            SDD.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss.ffff}-{type}-{eventid}: {message}");
            try
            {
                EventLog.WriteEntry(message, type, eventid);
            }
            catch (Exception exception)
            {
                SDD.WriteLine("Failed to log message:\r\n" + exception.ToString());
            }
        }

        public static void Warn(string message, int eventid)
        {
            LogMessage(EventLogEntryType.Warning, message, eventid);
        }
    }
}

