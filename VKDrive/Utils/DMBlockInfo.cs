using System;
namespace VKDrive.Utils
{
    class DmBlockInfo
    {
        public bool IsComplite = false;
        public bool IsError = false;
        public int IsLock = 1;
        public int Length = 0;
        public int Downloaded = 0;
        public int FileId;

        public DmBlockInfo(int fileId)
        {
            FileId = fileId;
        }
    }
}
