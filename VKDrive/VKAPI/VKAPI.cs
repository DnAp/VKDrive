using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VKDrive.VKAPI
{
    class VKAPI
    {
        const int TIMEOUT = 1000 / 3;
        ConcurrentQueue<int> concurrentQueue = new ConcurrentQueue<int>();
        bool isAlive = true;
        private static VKAPI instance;

        private AutoResetEvent resetEvent = new AutoResetEvent(false);

        private VKAPI()
        {
            Thread workerThread = new Thread(this.DoWork);
            workerThread.Start();
        }

        public static VKAPI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VKAPI();
                }
                return instance;
            }
        }

        public string StartTask(int i)
        {
            concurrentQueue.Enqueue(i);
            resetEvent.WaitOne();

            return i.ToString();
        }

        public void DoWork()
        {
            while (isAlive)
            {
                int i;
                while (concurrentQueue.TryDequeue(out i))
                {
                    Console.WriteLine(i.ToString() + "t");
                }
                resetEvent.Set();
                resetEvent.Reset();
                Thread.Sleep(TIMEOUT);
            }
        }

        public void Stop()
        {
            isAlive = false;
        }
    }
}
