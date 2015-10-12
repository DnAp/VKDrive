using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive.Files
{
    class Log
    {
        public StreamWriter file;
        public static Log log;
        public static void init()
        {
            Log.log = new Log();
        }
        public static void l(string str)
        {
            
            lock (Log.log)
            {
                Log.log.file = new StreamWriter(new FileStream("log.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                Log.log.file.WriteLine(str);
                Log.log.file.Close();
            }/*
            Console.WriteLine(str);*/
        }
    }
}
