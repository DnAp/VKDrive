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
    public class Get : ILoader
    {
        int AlbumId;
        int OwnerId;
        public Get(int ownerId, int albumId)
        {
            OwnerId = ownerId;
            AlbumId = albumId;
        }

        public VFile[] Load()
        {
            JObject apiResult = (JObject)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery(
                "photos.get", 
                new Dictionary<string, string>() {
                    { "owner_id", OwnerId.ToString() },
                    { "album_id", AlbumId.ToString() }
                }));

            Photo photo;
            JArray items = (JArray)apiResult.GetValue("items");
            VFile[] result = new VFile[items.Count];
            int i = 0;
            foreach (JObject item in items)
            {
                photo = new Photo(item.ToObject<SerializationObject.Photo>());
                result[i] = photo;
                i++;
            }
            return result;
        }
    }
}
