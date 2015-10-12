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
    class DirUsers : Dir
    {
        const string STORAGE_KEY = "DirUsers";
        public override void _LoadRootNode()
        {
            RootNode = new Folder("");
            RootNode.Property.Add("type", "friends.getLists");
            RootNode.IsLoaded = false;
        }

        public override bool _LoadFile(Files.Folder file)
        {
            if (file.Property["type"] == "friends.getLists")
            {
                string xml = VKAPI.Instance.execute("friends.getLists");
                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> lids = responce.Elements("list");
                Folder curFolder = new Folder("Все");
                curFolder.Property.Add("type", "friends.get");
                curFolder.Property.Add("lid", "0");
                file.ChildsAdd(curFolder);

                foreach (XElement lid in lids)
                {
                    curFolder = new Folder(lid.Element("name").Value);
                    curFolder.Property.Add("type", "friends.get");
                    curFolder.Property.Add("lid", lid.Element("lid").Value);
                    file.ChildsAdd(curFolder);
                }


                curFolder = new Folder("Мои подписки");
                curFolder.Property.Add("type", "subscriptions.get");
                file.ChildsAdd(curFolder);
                curFolder = new Folder("Другие");
                curFolder.Property.Add("type", "storage.get");
                file.ChildsAdd(curFolder);
            }
            else if (file.Property["type"] == "friends.get")
            {
                int key = Convert.ToInt32(file.Property["lid"]);

                Dictionary<string, string> param = new Dictionary<string, string>(){
				    {"fields", "first_name,last_name,domain"}
			    };

                if (key > 0)
                {
                    param.Add("lid", key.ToString());
                }

                string xml = VKAPI.Instance.execute("friends.get", param);

                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> users = responce.Elements("user");
                foreach (XElement user in users)
                {
                    file.ChildsAdd(CreateUserFolder(user));
                }
            }
            else if (file.Property["type"] == "subscriptions.get")
            {
                string xml = VKAPI.Instance.execute("subscriptions.get");

                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> uids = responce.Element("users").Elements("uid");
                List<string> uidsList = new List<string>();

                foreach (XElement uid in uids)
                {
                    uidsList.Add(uid.Value);
                }
                if (uidsList.Count == 0)
                {
                    return true;
                }
                if (uidsList.Count >= 1000)
                {
                    // todo readme
                    return true;
                }

                xml = VKAPI.Instance.execute("users.get", new Dictionary<string, string>() { { "uids", String.Join(",", uidsList) } });

                responce = XElement.Parse(xml);
                IEnumerable<XElement> users = responce.Elements("user");
                foreach (XElement user in users)
                {
                    file.ChildsAdd(CreateUserFolder(user));
                }
            }
            else if (file.Property["type"] == "storage.get")
            {
                string storageUids = API.VKStorage.get(STORAGE_KEY);

                file.ChildsAdd(new Files.Settings("Добавить людей.lnk"));
                file.ChildsAdd(
                    new Files.SettingsXls(
                        "VKDirvePathData.xml", "Добавление людей",
                        "Введите ссылку на человека или номер его страницы. Например http://vk.com/durov",
                        "Никого не удалось найти.",
                        "Такой человек уже есть в этом списке."
                    ));

                if (storageUids.Length > 0)
                {
                    string xml = VKAPI.Instance.execute("users.get", new Dictionary<string, string>() { { "uids", storageUids.Replace('\n', ',') } });

                    XElement responce = XElement.Parse(xml);
                    IEnumerable<XElement> users = responce.Elements("user");
                    foreach (XElement user in users)
                    {
                        file.ChildsAdd(CreateUserFolder(user));
                    }
                }
            }
            else if (file.Property["type"] == "AudioApi.executeGetAlbums")
            {
                System.Collections.ArrayList files = new System.Collections.ArrayList();

                AudioApi.executeGetAlbums(new Dictionary<string, string>(){
				        {"uid", file.Property["uid"]}
			        }, file.Childs);
            }
            else if (file.Property["type"] == "photos.getAlbums")
            {
                string xml = VKAPI.Instance.execute("photos.getAlbums", new Dictionary<string, string>() { { "uid", file.Property["uid"] } });

                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> aubums = responce.Elements("album");
                Folder curFolder;
                foreach (XElement aubum in aubums)
                {
                    curFolder = new Folder(aubum.Element("title").Value);
                    curFolder.Property.Add("type", "photos.get");
                    curFolder.Property.Add("uid", file.Property["uid"]);
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
                string xml = VKAPI.Instance.execute("photos.get",
                    new Dictionary<string, string>() { { "uid", file.Property["uid"] }, { "aid", file.Property["aid"] } });

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
                while (!file.IsLoaded)
                {
                    // todo сделать перехват фатала.
                    System.Threading.Thread.Sleep(100);
                }
            }
            return true;
        }

        /// <summary>
        /// Создает каталог для пользователя
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private Folder CreateUserFolder(XElement user)
        {
            Folder curFolder = new Folder(user.Element("first_name").Value + " " + user.Element("last_name").Value);

            string uid = user.Element("uid").Value;

            Folder subFolder = new Folder("Аудиозаписи");
            subFolder.Property.Add("type", "AudioApi.executeGetAlbums");
            subFolder.Property.Add("uid", uid);
            curFolder.ChildsAdd(subFolder);

            subFolder = new Folder("Фотографии");
            subFolder.Property.Add("type", "photos.getAlbums");
            subFolder.Property.Add("uid", uid);
            curFolder.ChildsAdd(subFolder);

            curFolder.ChildsAdd(
                new PlainText(
                    "Открыть в браузере.url", 
                    PlainText.InternetShortcut("http://vk.com/id" + uid)
                )
            );
            
            curFolder.IsLoaded = true;
            return curFolder;
        }

        public override int CreateDirectory(Folder file, string filename, DokanFileInfo info)
        {
            // http://vk.com/id1
            // http://vk.com/durov
            // http://vkontakte.ru/id1

            filename = System.Text.RegularExpressions.Regex.Replace(filename, @"http(s|)://.*/", "");
            // хавает любое занчение: 47348352, club47348352, vkdriveapp

            if (filename.Length == 0)
            {
                return DokanNet.DOKAN_ERROR;
            }
            string xml;
            try
            {
                xml = VKAPI.Instance.execute("users.get", new Dictionary<string, string>() { { "uids", filename } });
            }
            catch (Exception e)
            {
                if (e.Data.Contains("code") && e.Data["code"].ToString() == "113")
                {
                    Console.WriteLine("Invalid user id");
                }
                return DokanNet.DOKAN_ERROR;
            }

            XElement responce = XElement.Parse(xml);
            IEnumerable<XElement> groups = responce.Elements("user");
            ushort count = 0;
            foreach (XElement user in groups)
            {
                string uid = user.Element("uid").Value;
                bool isDooble = false;
                /*foreach (VFile file in RootNode.Childs)
                {
                    if (file.GetType() == typeof(Folder))
                    {
                        // проверка вхождения ключа нужна при разделении на каталоги. В этом случае сложим группу в корень после перезагрузки все исправится
                        if (((Folder)file).Property.ContainsKey("gid") && ((Folder)file).Property["gid"] == gid)
                        {
                            isDooble = true;
                            break;
                        }
                    }
                }*/
                if (isDooble)
                {
                    count++;
                    continue;
                }

                if (user.Element("first_name").Value == "DELETED")
                {
                    continue;
                }

                API.VKStorage.join(STORAGE_KEY, uid);

                file.ChildsAdd(CreateUserFolder(user));

                count++;
            }

            if (count > 0)
            {
                return DokanNet.DOKAN_SUCCESS;
            }
            return DokanNet.DOKAN_ERROR;

        }
    }
}
