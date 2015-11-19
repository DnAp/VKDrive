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
        private int OwnerId;

        public GetAlbums(int OwnerId)
        {
            this.OwnerId = OwnerId;
        }

        public VFile[] Load()
        {
            JObject apiResult;
            try { 
                apiResult = (JObject)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery(
                    "photos.getAlbums", 
                    new Dictionary<string, string>() {
                        { "owner_id", OwnerId.ToString() }
                }));
            }
            catch (Exception e)
            {
                PlainText readme;
                if (e.Data.Contains("code") && e.Data["code"].ToString() == "15")
                {
                    // 15:Access denied: group photos are disabled

                    readme = new PlainText("Фотографии отключены.txt");
                    readme.SetText(PlainText.getSubscript());
                    return new VFile[] { readme };
                }
                if (e.Data.Contains("code"))
                {
                    readme = new PlainText("Ошибка " + e.Data["code"].ToString() + ".txt");
                    readme.SetText(PlainText.getSubscript());
                    return new VFile[] { readme };
                }
                readme = new PlainText("Неизвестная ошибка.txt");
                readme.SetText(e.ToString()+PlainText.getSubscript());
                return new VFile[] { readme };
            }
            JArray items = (JArray)apiResult.GetValue("items");
            Folder curFolder;
            VFile[] result = new VFile[items.Count];
            int i = 0;
            foreach (JObject item in items)
            {
                SerializationObject.Album album = item.ToObject<SerializationObject.Album>();

                curFolder = new Folder(album.Title, new Photos.Get(OwnerId, album.Id));
                DateTime unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                curFolder.CreationTime = unixTimeStamp.AddSeconds(album.Created);
                curFolder.LastWriteTime = unixTimeStamp.AddSeconds(album.Updated);
                result[i] = curFolder;
                i++;
            }
            return result;
        }
    }
}
