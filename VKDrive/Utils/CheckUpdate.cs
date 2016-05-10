using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace VKDrive.Utils
{
	internal static class CheckUpdate
	{
		private class MyWebClient : WebClient
		{
			protected override WebRequest GetWebRequest(Uri uri)
			{
				var w = base.GetWebRequest(uri);
				w.Timeout = 5000; // 5 sec
				return w;
			}
		}

		public static bool HasNewVersion()
		{
			try
			{
				var client = new MyWebClient();
                using (client)
				{
					var version = new Version(client.DownloadString("http://dnap.su/vkdrive/version.txt"));
					return version > Assembly.GetExecutingAssembly().GetName().Version;
				}
			}
			catch (Exception)
			{
				// ignored
			}
			return false;
		}

		public static bool Upgrade()
		{
			try
			{
				/*if (!HasNewVersion())
				{
					return false;
				}*/
				var client = new WebClient();
				using (client)
				{
						var fileName = System.IO.Path.GetTempPath() + Guid.NewGuid() + ".exe";
						client.DownloadFile("http://dnap.su/vkdrive/setup.exe", fileName);
						Process.Start(fileName, "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS");
						return true;
				}
			}
			catch (Exception)
			{
				// ignored
			}
			return false;
		}
		


	}
}
