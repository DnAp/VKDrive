using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using VKDrive.Files;
using VKDrive.Loader.IFace;
using VKDrive.VKAPI;

namespace VKDrive.Loader.VKontakte.Audio
{
	public class Search : ILoader
	{
		private readonly string _query;
		private readonly int _count = 200;

		public Search(string query)
		{
			_query = query;
		}

		public VFile[] Load()
		{
			var result = new HashSet<VFile>();

			try
			{
				var apiResult = (JObject) Vkapi.Instance.StartTaskSync(new ApiQuery(
					"audio.search",
					new Dictionary<string, string>
					{
						{"q", _query},
						{"count", _count.ToString()}
					}
					));

				foreach (var jToken in apiResult.GetValue("items"))
				{
					result.Add(new Mp3(
						jToken.ToObject<SerializationObject.Audio>()
						));
				}

				return result.ToArray();
			}
			catch (Exception e)
			{
				return new VFile[] {SerializationObject.ExceptionToFile(e)};
			}
		}
	}
}
