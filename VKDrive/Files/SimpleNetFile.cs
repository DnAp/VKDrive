using System;
using DokanNet;

namespace VKDrive.Files
{
	class SimpleNetFile : Download
	{
		private readonly int[] _uid;
		public SimpleNetFile(string name, string url, int uid1, int uid2) : base(name)
		{
			_uid = new[] { uid1, uid2 };
			Url = url;
		}

        public override NtStatus ReadFile(byte[] buffer, ref int readBytes, long offset, DokanFileInfo info)
        {
            return DownloadManager.Instance.GetBlock(this, buffer, ref readBytes, offset);
		}

		public override bool Update()
		{
			return false;
		}

		public override int[] GetUniqueId()
		{
			return _uid;
		}
	}
}
