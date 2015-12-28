using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKDrive.Files;
using VKDrive.VKAPI;

namespace VKDrive.Loader.VKontakte.Photos
{
    public class GetAlbums : ILoader
    {
        private readonly int _ownerId;

        public GetAlbums(int ownerId)
        {
            this._ownerId = ownerId;
        }

        public VFile[] Load()
        {
	        try { 
                var apiResult = (JObject)Vkapi.Instance.StartTaskSync(new ApiQuery(
	                "photos.getAlbums", 
	                new Dictionary<string, string>() {
		                { "owner_id", _ownerId.ToString() }
	                }));

				var items = (JArray)apiResult.GetValue("items");
				
				var result = new List<VFile>();
				foreach (var jToken in items)
				{
					var album = jToken.ToObject<SerializationObject.Album>();

					var curFolder = new Folder(album.Title, new Get(_ownerId, album.Id));
					var unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
					curFolder.CreationTime = unixTimeStamp.AddSeconds(album.Created);
					curFolder.LastWriteTime = unixTimeStamp.AddSeconds(album.Updated);
					result.Add(curFolder);
				}
				return result.ToArray();
			}
            catch (Exception exception)
            {
	            return new VFile[] { SerializationObject.ExceptionToFile(exception) };
            }
        }
    }
}
