using System.Collections.Generic;
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
                var folder = new Folder("Аудиозаписи");
                folder.Property.Add("type", "AudioApi.ExecuteGetAlbums");
                RootNode.Childs.Add(folder);

                folder = new Folder("Фотографии", new Loader.VKontakte.Photos.GetAlbums(VkapiLibrary.Instance.UserId));
                RootNode.Childs.Add(folder);

				folder = new Folder("Стена(beta)", new Loader.VKontakte.Wall.Get(VkapiLibrary.Instance.UserId));
				RootNode.Childs.Add(folder);

				RootNode.IsLoaded = true;
            }
        }
        
        public override bool _LoadFile(Files.Folder file)
        {
            
            if (file.Property["type"] == "AudioApi.ExecuteGetAlbums")
            {
                AudioApi.ExecuteGetAlbums(new Dictionary<string, string>(){
				        {"owner_id", VkapiLibrary.Instance.UserId.ToString()}
			        }, file.Childs);
            }
            else if (file.Property["type"] == "audio.getInAlbum")
            {
                AudioApi.LoadMp3(new Dictionary<string, string>(){
				        {"owner_id", file.Property["uid"]},
                        { "album_id", file.Property["album_id"]}
			        }, file.Childs);
            }
            else if (file.Property["type"] == "wait")
            {
                // Он там грузится в паралельном потоке. Подождать нужно
                var i = 0;
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
