using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VKDrive
{
    static class Program
    {

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {


            XmlConfigurator.Configure();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);

            try {
                Application.Run(new Browser());
            }catch(Exception e) {
                ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
                Log.Fatal("Main thread", e);
            }
        }

        private static void ThreadException(object sender, ThreadExceptionEventArgs t)
        {
            ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            Log.Fatal(sender.ToString(), t.Exception);
        }
    }
}
