using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using VKDrive.Files;
using VKDrive.Loader.IFace;
using VKDrive.VKAPI;

namespace VKDrive.Loader.VKontakte.Wall
{
	public class Get : ILoader
	{
		public int OwnerId { get; }
		
		public Get(int ownerId)
		{
			OwnerId = ownerId;
		}

		public VFile[] Load()
		{
			var apiResult = (JObject)Vkapi.Instance.StartTaskSync(new ApiQuery(
				"wall.get",
				new Dictionary<string, string> {
					{ "owner_id", OwnerId.ToString() },
					{ "offset", "0" },
					{ "count", "100" }, // todo paging
					{ "filter", "all" },
				}));

			var items = (JArray)apiResult.GetValue("items");
			var result = new VFile[items.Count];
			var i = 0;
			// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
			foreach (JObject jToken in items)
			{
				result[i] = WallPostToFolder(jToken, true);
				i++;
			}
			
			return result;
		}

		public static VFile WallPostToFolder(JObject jToken, bool isTop = false)
		{
			var wallPost = jToken.ToObject<SerializationObject.WallPost>();
			
			// Я хотел значек музыки или фотки засунуть если они есть в посте
			var folderName = wallPost.Text.Trim();
			
			var folder = new Folder("")
			{
				CreationTime = wallPost.Date,
				LastWriteTime = wallPost.Date
			};

			JToken jArray;
			if (jToken.TryGetValue("copy_history", out jArray))
			{
				// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
				foreach ( JObject historyItem in (JArray)jArray)
				{
					if (isTop && folderName.Length == 0)
						folderName = historyItem.GetValue("text").ToString().Trim();

					var copyFolder = WallPostToFolder(historyItem);
					folder.ChildsAdd(copyFolder);
				}
			}
			folder.FileName = VFile.ClearName(wallPost.Date.ToString("yyyy-MM-dd ") + folderName, false);
			if (wallPost.Text.Trim().Length > 0)
			{
				folder.ChildsAdd(new PlainText("Текст.txt", wallPost.Text.Trim()));
			}
			//folder.ChildsAdd(new PlainText("debug.json", jToken.ToString()));
			
			if (jToken.TryGetValue("attachments", out jArray))
			{
				// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
				foreach (var attachFile in (JArray)jArray)
				{
					folder.ChildsAdd(ConvertAttachToFile((JObject)attachFile));
				}

			}
			folder.IsLoaded = true;
			return folder;

		}

		private static VFile ConvertAttachToFile(JObject attachFile)
		{
			JObject obj;
			switch (attachFile.GetValue("type").ToString())
			{
				case "photo":
					return new Photo(attachFile.GetValue("photo").ToObject<SerializationObject.Photo>());
				case "posted_photo":
					obj = (JObject)attachFile.GetValue("posted_photo");
                    return new SimpleNetFile("Изображение.jpg", obj.GetValue("photo_604").ToString(), int.Parse(obj.GetValue("owner_id").ToString()), int.Parse(obj.GetValue("id").ToString()));
				case "audio":
					return new Mp3(attachFile.GetValue("audio").ToObject<SerializationObject.Audio>());
				case "graffiti":
					obj = (JObject)attachFile.GetValue("graffiti");
					return new SimpleNetFile("Графити.jpg", obj.GetValue("photo_586").ToString(), int.Parse(obj.GetValue("owner_id").ToString()), int.Parse(obj.GetValue("id").ToString()));
				// video
				// album
				default:
					var type = attachFile.GetValue("type").ToString();
					return new PlainText(type+".json", attachFile.GetValue(type).ToString());
			}
		}
	}
}
