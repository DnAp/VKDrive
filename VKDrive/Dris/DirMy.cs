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

                folder = new Folder("Фотографии", new Loader.VKontakte.Photos.GetAlbums(VKAPILibrary.Instance.UserID));
                RootNode.Childs.Add(folder);
                
                RootNode.IsLoaded = true;
            }
        }
        
        public override bool _LoadFile(Files.Folder file)
        {
            
            if (file.Property["type"] == "AudioApi.executeGetAlbums")
            {
                AudioApi.executeGetAlbums(new Dictionary<string, string>(){
				        {"owner_id", VKAPI.VKAPILibrary.Instance.UserID.ToString()}
			        }, file.Childs);
            }
            else if (file.Property["type"] == "audio.getInAlbum")
            {
                AudioApi.loadMP3(new Dictionary<string, string>(){
				        {"owner_id", file.Property["uid"]},
                        { "album_id", file.Property["album_id"]}
			        }, file.Childs);
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
                // Подождать не вышло, выводим файл с инфой что нужно ждать
            }
            return true;
        }

        

    }
}
