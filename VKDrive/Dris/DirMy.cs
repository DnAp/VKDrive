using Dokan;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VKDrive.API;
using VKDrive.Files;
using VKDrive.VKAPI;

namespace VKDrive.Dris
{
    public class DirMy : Dir
    {


        public override void _LoadRootNode()
        {
            RootNode = new Folder("");
            lock (RootNode)
            {
                Folder folder = new Folder("Аудиозаписи");
                folder.Property.Add("type", "AudioApi.executeGetAlbums");
                
                RootNode.Childs.Add(folder);
                folder = new Folder("Фотографии");
                folder.Property.Add("type", "photos.getAlbums");
                RootNode.Childs.Add(folder);
                
                RootNode.IsLoaded = true;
            }
        }
        
        public override bool _LoadFile(Files.Folder file)
        {
            if (file.Property["type"] == "AudioApi.executeGetAlbums")
            {
                AudioApi.executeGetAlbums(new Dictionary<string, string>(){
				        {"uid", VKAPI.VKAPILibrary.Instance.UserID.ToString()}
			        }, file.Childs);
            }
            else if (file.Property["type"] == "photos.getAlbums")
            {
                JObject apiResult = (JObject)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("photos.getAlbums"));
                JArray items = (JArray)apiResult.GetValue("items");
                Folder curFolder;
                foreach (JObject item in items)
                {
                    SerializationObject.Album album = item.ToObject<SerializationObject.Album>();

                    curFolder = new Folder(album.Title);
                    curFolder.Property.Add("type", "photos.get");
                    curFolder.Property.Add("aid", album.Id.ToString());
                    DateTime unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    curFolder.CreationTime = unixTimeStamp.AddSeconds(album.Created);
                    curFolder.LastWriteTime = unixTimeStamp.AddSeconds(album.Updated);
                    file.ChildsAdd(curFolder);
                }
            }
            else if (file.Property["type"] == "audio.getInAlbum")
            {
                AudioApi.loadMP3(new Dictionary<string, string>(){
				        {"uid", file.Property["uid"]},
                        { "album_id", file.Property["album_id"]}
			        }, file.Childs);
            }
            else if (file.Property["type"] == "photos.get")
            {
                JObject apiResult = (JObject)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("photos.get", new Dictionary<string,string>(){ {"aid", file.Property["aid"]} }));

                Photo photo;
                JArray items = (JArray)apiResult.GetValue("items");
                foreach (JObject item in items)
                {
                    photo = new Photo(item.ToObject<SerializationObject.Photo>());
                    file.ChildsAdd(photo);
                }
            }
            else if (file.Property["type"] == "wait")
            {
                // Он там грузится в паралельном потоке. Подождать нужно
                int i = 0;
                while (!file.IsLoaded && i<10) // 1 сек максимум
                {
                    i++;
                    // todo сделать перехват фатала.
                    System.Threading.Thread.Sleep(100);
                }
                // Подождать не вышло, выводим файл и инфой что нужно ждать
            }
            return true;
        }

        

    }
}
