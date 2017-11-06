using Esatto.VirtualPrinter;
using System;
using System.Threading;

namespace Esatto.VirtualPrinter.Dispatcher
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Mutex mutex = new Mutex(false, typeof(Program).FullName + "-sessionmutex");
                try
                {
                    if (!mutex.WaitOne(0))
                    {
                        Log.Info("Duplicate dispatcher start", 272);
                        return;
                    }
                }
                catch (AbandonedMutexException)
                {
                    Log.Info("Dispatcher abandoned mutex", 273);
                }

                // we have the mutex now
                try
                {
                    new PrintDispatcherApplication().Run();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to run virutal printer dispatcher:\r\n{exception}", 102);
            }
        }
    }
}

