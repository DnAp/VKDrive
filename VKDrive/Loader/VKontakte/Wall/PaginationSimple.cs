using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VKDrive.Files;
using VKDrive.VKAPI;

namespace VKDrive.Loader.VKontakte.Wall
{
	internal class PaginationSimple
	{
		/// <summary>
		/// todo нифига не законченый вариант
		/// </summary>
		/// <param name="ownerId"></param>
		/// <returns></returns>
		public VFile[] PaginationSimpleStart(int ownerId)
		{
			var code = string.Format(@"var lastPosts = API.wall.get({{owner_id: {0}, offset: 0, count: 500, filter: ""all""}});
if(lastPosts.count < 500) {{
	var firstPost = API.wall.get({{ owner_id: {0}, offset: lastPosts.count - 1, count: 1, filter: ""all""}});
	result.push({{ count: lastPosts.count, lastPosts: lastPosts.items, firstPost: firstPost.items[0]}});
}}else{{
	result.push(lastPosts);
}}", ownerId);
			var apiResult = (JObject)Vkapi.Instance.StartTaskSync(new ApiExpression(code));
			var count = (int)apiResult.GetValue("count");
			
			JToken lastPpst;
			if (apiResult.TryGetValue("lastPosts", out lastPpst))
			{
				var resultFile = new List<VFile>();

				var firstPost = apiResult.GetValue("firstPost").ToObject<SerializationObject.WallPost>();
				var lastPosts = (JArray)apiResult.GetValue("lastPosts");
				var lastPost = lastPosts.First.ToObject<SerializationObject.WallPost>();


				if (lastPost.Date.Year != firstPost.Date.Year)
				{
					for (var i = firstPost.Date.Year; i <= lastPost.Date.Year; i++)
					{
						resultFile.Add(new Folder(i.ToString()));
                    }
				}
				return resultFile.ToArray();
			}
			var items = (JArray)apiResult.GetValue("items");
			var result = new VFile[items.Count];
			var j = 0;
			// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
			foreach (JObject jToken in items)
			{
				result[j] = Get.WallPostToFolder(jToken, true);
				j++;
			}
			return result;
		}
	}
}
