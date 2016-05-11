using log4net;
using log4net.Config;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using VKDrive.Utils;

namespace VKDrive
{
	internal static class Program
    {
		private static System.Threading.Mutex _mutex;

		/// <summary>
		/// Главная точка входа для приложения.
		/// </summary>
		[STAThread]
	    private static void Main()
	    {
#if DEBUG
			XmlConfigurator.Configure();
#endif
		    Application.EnableVisualStyles();
		    Application.SetCompatibleTextRenderingDefault(false);
			Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);

			var log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			log.Info("Start vkdrive #"+ Process.GetCurrentProcess().Id);

			bool mutexWasCreated;
			_mutex = new Mutex(true, "VKDrive", out mutexWasCreated);
            if (!mutexWasCreated)
            {
	            log.Warn("Double start! Exit.");
                Environment.Exit(0);
            }
			
			try
			{
                Application.Run(new Browser());
		    }
		    catch (Exception e)
		    {
			    //var log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			    log.Fatal("Main thread", e);
		    }
	    }

	    private static void ThreadException(object sender, ThreadExceptionEventArgs t)
        {
            var log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            log.Fatal(sender.ToString(), t.Exception);
        }
		
	}
}
