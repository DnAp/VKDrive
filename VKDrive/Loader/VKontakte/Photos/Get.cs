using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using VKDrive.Files;
using VKDrive.Loader.IFace;
using VKDrive.VKAPI;

namespace VKDrive.Loader.VKontakte.Photos
{
    public class Get : ILoader
    {
	    private readonly int _albumId;
	    private readonly int _ownerId;
        public Get(int ownerId, int albumId)
        {
            _ownerId = ownerId;
            _albumId = albumId;
        }

        public VFile[] Load()
        {
            var apiResult = (JObject)Vkapi.Instance.StartTaskSync(new ApiQuery(
                "photos.get", 
                new Dictionary<string, string> {
                    { "owner_id", _ownerId.ToString() },
                    { "album_id", _albumId.ToString() }
                }));

	        var items = (JArray)apiResult.GetValue("items");
            var result = new VFile[items.Count];
            var i = 0;
            foreach (var jToken in items)
            {
	            var item = (JObject) jToken;
	            var photo = new Photo(item.ToObject<SerializationObject.Photo>());
                result[i] = photo;
                i++;
            }
            return result;
        }
    }
}
