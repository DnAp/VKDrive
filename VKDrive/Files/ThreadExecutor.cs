using System;
using System.Threading;

namespace VKDrive.Files
{
    class ThreadExecutor
    {
        public void Execute(Action action)
        {
            new Thread(() => action()).Start();
        }
    }
}
