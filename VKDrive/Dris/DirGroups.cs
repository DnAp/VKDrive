using Dokan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VKDrive.API;
using VKDrive.Files;
using System.Text.RegularExpressions;

namespace VKDrive.Dris
{
    class DirGroups : Dir
    {
        const string STORAGE_KEY = "DirGroups";

        public override void _LoadRootNode()
        {
            RootNode = new Folder("");
            RootNode.Property.Add("type", "groups.get");
            RootNode.IsLoaded = false;
        }

        public override bool _LoadFile(Files.Folder file)
        {
            if (file.Property["type"] == "groups.get")
            {
                file.ChildsAdd(new Files.Settings("Добавить группу.lnk"));
                file.ChildsAdd(
                    new Files.SettingsXls(
                        "VKDirvePathData.xml", "Добавление группы",
                        "Введите ссылку на группу или ее идентификатор. Например http://vk.com/club47348352",
                        "Не удалось найти такую группу.",
                        "Такая группа уже существует."
                    ));

                string xml = VKAPI.Instance.execute("groups.get", new Dictionary<string, string>() { { "extended", "1" } });
                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> groups = responce.Elements("group");
                
                List<string> gruopIds = new List<string>();

                foreach (XElement group in groups)
                {
                    file.ChildsAdd(CreateGroupFolder(group));
                    gruopIds.Add(group.Element("gid").Value);
                }
                string gids = API.VKStorage.get(STORAGE_KEY);
                if (gids.Length > 0)
                {
                    xml = VKAPI.Instance.execute("groups.getById", new Dictionary<string, string>() { { "gids", gids.Replace('\n', ',') } });
                    responce = XElement.Parse(xml);
                    groups = responce.Elements("group");
                    foreach (XElement group in groups)
                    {
                        string gid = group.Element("gid").Value;

                        if (gruopIds.IndexOf(gid) > -1)
                            continue;

                        file.ChildsAdd(CreateGroupFolder(group));
                    }
                }
                
            }
            else if (file.Property["type"] == "AudioApi.executeGetAlbums")
            {
                //// 15:Access denied: group audio is disabled
                try
                {
                    AudioApi.executeGetAlbums(new Dictionary<string, string>(){
				        {"gid", file.Property["gid"]}
			        }, file.Childs);
                }
                catch (Exception e)
                {
                    if(e.Data.Contains("code") && e.Data["code"].ToString() == "15" ){
                        // 15:Access denied: group photos are disabled

                        PlainText readme = new PlainText("Аудиозаписи отключены.txt");
                        readme.SetText(PlainText.getSubscript());
                        file.ChildsAdd(readme);
                        return true;
                    }
                    return false;
                
                }
                
            }
            else if (file.Property["type"] == "audio.getInAlbum")
            {
                AudioApi.loadMP3(new Dictionary<string, string>(){
				        {"gid", file.Property["gid"]},
                        { "album_id", file.Property["album_id"]}
			        }, file.Childs);
            }
            else if (file.Property["type"] == "photos.getAlbums")
            {
                string xml;
                try
                {
                    xml = VKAPI.Instance.execute("photos.getAlbums", new Dictionary<string, string>() { { "gid", file.Property["gid"] } });
                }
                catch (Exception e)
                {
                    if(e.Data.Contains("code") && e.Data["code"].ToString() == "15" ){
                        // 15:Access denied: group photos are disabled

                        PlainText readme = new PlainText("Фотографии отключены.txt");
                        readme.SetText(PlainText.getSubscript());
                        file.ChildsAdd(readme);
                        return true;
                    }
                    return false;
                }
                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> aubums = responce.Elements("album");
                Folder curFolder;
                foreach (XElement aubum in aubums)
                {
                    curFolder = new Folder(aubum.Element("title").Value);
                    curFolder.Property.Add("type", "photos.get");
                    curFolder.Property.Add("gid", file.Property["gid"]);
                    curFolder.Property.Add("aid", aubum.Element("aid").Value);
                    DateTime unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    curFolder.CreationTime = unixTimeStamp.AddSeconds(Convert.ToInt32(aubum.Element("created").Value));
                    curFolder.LastWriteTime = unixTimeStamp.AddSeconds(Convert.ToInt32(aubum.Element("updated").Value));
                    file.ChildsAdd(curFolder);
                }
            }
            else if (file.Property["type"] == "photos.get")
            {
                string xml = VKAPI.Instance.execute("photos.get",
                    new Dictionary<string, string>() { { "gid", file.Property["gid"] }, { "aid", file.Property["aid"] } });

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
                while (!file.IsLoaded && i < 20) // 2 сек максимум
                {
                    i++;
                    // todo сделать перехват фатала.
                    System.Threading.Thread.Sleep(100);
                }
                // Подождать не вышло, выводим что есть
            }

            return true;
        }

        private Folder CreateGroupFolder(XElement group)
        {
            string gid = group.Element("gid").Value;
            Folder curFolder = new Folder(group.Element("name").Value);
            curFolder.Property.Add("gid", gid);

            Folder subFolder = new Folder("Аудиозаписи");
            subFolder.Property.Add("type", "AudioApi.executeGetAlbums");
            subFolder.Property.Add("inStorage", "1");
            subFolder.Property.Add("gid", gid);
            curFolder.ChildsAdd(subFolder);

            subFolder = new Folder("Фотографии");
            subFolder.Property.Add("type", "photos.getAlbums");
            subFolder.Property.Add("gid", gid);
            curFolder.ChildsAdd(subFolder);

            curFolder.ChildsAdd(
                new PlainText(
                    "Открыть в браузере.url",
                    PlainText.InternetShortcut("http://vk.com/club" + gid)
                )
            );

            curFolder.IsLoaded = true;
            return curFolder;
        }

        public override int CreateDirectory(Folder file, string filename, DokanFileInfo info)
        {
            // http://vk.com/club47348352
            // http://vk.com/vkdriveapp
            // http://vkontakte.ru/club47348352
            
            filename = Regex.Replace(filename, @"http(s|)://.*/", "");
            // хавает любое занчение: 47348352, club47348352, vkdriveapp

            if (filename.Length == 0)
            {
                return DokanNet.DOKAN_ERROR;
            }
            string xml;
            try
            {
                xml = VKAPI.Instance.execute("groups.getById", new Dictionary<string, string>() { { "gids", filename } });
            }
            catch (Exception e)
            {
                if (e.Data.Contains("code") && e.Data["code"].ToString() == "125")
                {
                    Console.WriteLine("Invalid group id");
                }
                return DokanNet.DOKAN_ERROR;
            }

            XElement responce = XElement.Parse(xml);
            IEnumerable<XElement> groups = responce.Elements("group");
            
            ushort count = 0;
            foreach (XElement group in groups)
            {
                string gid = group.Element("gid").Value;
                bool isDooble = false;
                foreach (VFile curFile in file.Childs)
                {
                    if (curFile.GetType() == typeof(Folder))
                    {
                        // проверка вхождения ключа нужна при разделении на каталоги. В этом случае сложим группу в корень после перезагрузки все исправится
                        if (((Folder)curFile).Property.ContainsKey("gid") && ((Folder)curFile).Property["gid"] == gid)
                        {
                            isDooble = true;
                            break;
                        }
                    }
                }
                if (isDooble)
                {
                    count++;
                    continue;
                }

                if (group.Element("name").Value == "DELETED")
                {
                    continue;
                }

                API.VKStorage.join(STORAGE_KEY, gid);

                file.ChildsAdd(CreateGroupFolder(group));
                count++;
            }

            if (count > 0)
            {
                return DokanNet.DOKAN_SUCCESS;
            }
            return DokanNet.DOKAN_ERROR;
            
        }


        public override int DeleteDirectory(string filename, DokanFileInfo info)
        {
            VFile node = FindFiles(filename);
            if (node == null && node.GetType() == typeof(Folder) && ((Folder)node).Property.ContainsKey("inStorage"))
            {
                RootNode.Childs.Remove(node);
                API.VKStorage.remove(STORAGE_KEY, ((Folder)node).Property["gid"]);
                return DokanNet.DOKAN_SUCCESS;
            }

            return DokanNet.DOKAN_ERROR;

            
        }



    }
}
