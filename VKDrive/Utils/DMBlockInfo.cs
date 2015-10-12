using System;
namespace VKDrive.Utils
{
    class DMBlockInfo
    {
        public bool IsComplite = false;
        public bool IsError = false;
        public int IsLock = 1;
        public int Length = 0;
        public int Downloaded = 0;
        public int FileId;

        public DMBlockInfo(int fileId)
        {
            FileId = fileId;
        }
    }
}
