using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using log4net;

namespace VKDrive.VKAPI
{
    class VkapiLibrary
    {
        public const int Json = 1;
        public const int Xml = 2;

        public int Expire = 0;
        public int UserId;
        /// <summary>
        /// Идентификатор сессии
        /// </summary>
        public string Sid = string.Empty;
        public string AccessTokien = string.Empty;
        public int AppId;

        private sealed class SingletonCreator
        {
            private static readonly VkapiLibrary instance = new VkapiLibrary();
            public static VkapiLibrary Instance { get { return instance; } }
        }

        public static VkapiLibrary Instance => SingletonCreator.Instance;
    }

}
