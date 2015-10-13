using Dokan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VKDrive.API;
using VKDrive.Files;

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
                string xml = VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("photos.getAlbums"));

                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> aubums = responce.Elements("album");
                Folder curFolder;
                foreach (XElement aubum in aubums)
                {
                    curFolder = new Folder(aubum.Element("title").Value);
                    curFolder.Property.Add("type", "photos.get");
                    curFolder.Property.Add("aid", aubum.Element("aid").Value);
                    DateTime unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    curFolder.CreationTime = unixTimeStamp.AddSeconds(Convert.ToInt32(aubum.Element("created").Value));
                    curFolder.LastWriteTime = unixTimeStamp.AddSeconds(Convert.ToInt32(aubum.Element("updated").Value));
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
                string xml = VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("photos.get", new Dictionary<string,string>(){ {"aid", file.Property["aid"]} }));

                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> photos = responce.Elements("photo");
                Photo photo;
                foreach (XElement curPhoto in photos)
                {
                    photo = new Photo("");
                    photo.LoadByXml(curPhoto);
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
