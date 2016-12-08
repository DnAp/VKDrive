using DokanNet;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using VKDrive.API;
using VKDrive.Files;
using VKDrive.VKAPI;

namespace VKDrive.Dris
{
    class DirUsers : Dir
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        const string StorageKey = "DirUsers";

        public override void _LoadRootNode()
        {
            RootNode = new Folder("");
            RootNode.Property.Add("type", "friends.getLists");
            RootNode.IsLoaded = false;
        }

        public override bool _LoadFile(Folder file)
        {
            _log.Debug("_LoadFile " + file);
            if (file.Property["type"] == "friends.getLists")
            {
                JObject apiResult = (JObject) Vkapi.Instance.StartTaskSync(new ApiQuery("friends.getLists"));
                JArray items = (JArray) apiResult.GetValue("items");

                Folder curFolder = new Folder("Все");
                curFolder.Property.Add("type", "friends.get");
                curFolder.Property.Add("lid", "0");
                file.ChildsAdd(curFolder);

                foreach (JObject item in items)
                {
                    curFolder = new Folder(item.GetValue("name").ToString()); // посмотреть типы данных
                    curFolder.Property.Add("type", "friends.get");
                    curFolder.Property.Add("lid", item.GetValue("id").ToString());
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

                Dictionary<string, string> param = new Dictionary<string, string>()
                {
                    {"fields", "first_name,last_name,domain"}
                };

                if (key > 0)
                {
                    param.Add("list_id", key.ToString());
                }

                JObject apiResult = (JObject) Vkapi.Instance.StartTaskSync(new ApiQuery("friends.get", param));
                JArray items = (JArray) apiResult.GetValue("items");

                foreach (JObject item in items)
                {
                    file.ChildsAdd(CreateUserFolder(item.ToObject<SerializationObject.User>()));
                }
            }
            else if (file.Property["type"] == "subscriptions.get")
            {
                /*
                JObject apiResult = (JObject)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("users.getSubscriptions"));

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

                xml = VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("users.get", new Dictionary<string, string>() { { "uids", String.Join(",", uidsList) } }));

                responce = XElement.Parse(xml);
                IEnumerable<XElement> users = responce.Elements("user");
                foreach (XElement user in users)
                {
                    file.ChildsAdd(CreateUserFolder(user));
                }*/
            }
            else if (file.Property["type"] == "storage.get")
            {
                string storageUids = VkStorage.Get(StorageKey);

                file.ChildsAdd(new Settings("Добавить людей.lnk"));
                file.ChildsAdd(
                    new Files.SettingsXls(
                        "VKDirvePathData.xml", "Добавление людей",
                        "Введите ссылку на человека или номер его страницы. Например https://vk.com/durov",
                        "Никого не удалось найти.",
                        "Такой человек уже есть в этом списке."
                    ));

                if (storageUids.Length > 0)
                {
                    JArray items =
                        (JArray)
                        Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("users.get",
                            new Dictionary<string, string> {{"user_ids", storageUids.Replace('\n', ',')}}));
                    foreach (JObject item in items)
                    {
                        file.ChildsAdd(CreateUserFolder(item.ToObject<SerializationObject.User>()));
                    }
                }
            }
            else if (file.Property["type"] == "AudioApi.ExecuteGetAlbums")
            {
                System.Collections.ArrayList files = new System.Collections.ArrayList();

                AudioApi.ExecuteGetAlbums(new Dictionary<string, string>()
                {
                    {"owner_id", file.Property["uid"]}
                }, file.Childs);
            }
            else if (file.Property["type"] == "audio.getInAlbum")
            {
                AudioApi.LoadMp3(new Dictionary<string, string>()
                {
                    {"owner_id", file.Property["uid"]},
                    {"album_id", file.Property["album_id"]}
                }, file.Childs);
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
        private Folder CreateUserFolder(SerializationObject.User user)
        {
            Folder curFolder = new Folder(user.FirstName + " " + user.LastName);

            string uid = user.Id.ToString();

            Folder subFolder = new Folder("Аудиозаписи");
            subFolder.Property.Add("type", "AudioApi.ExecuteGetAlbums");
            subFolder.Property.Add("uid", uid);
            curFolder.ChildsAdd(subFolder);

            subFolder = new Folder("Фотографии", new Loader.VKontakte.Photos.GetAlbums(user.Id));
            curFolder.ChildsAdd(subFolder);

            curFolder.ChildsAdd(
                new PlainText(
                    "Открыть в браузере.url",
                    PlainText.InternetShortcut("https://vk.com/id" + uid)
                )
            );

            curFolder.IsLoaded = true;
            return curFolder;
        }

        public override NtStatus CreateDirectory(Folder file, string filename, DokanFileInfo info)
        {
            // https://vk.com/id1
            // https://vk.com/durov
            // https://vkontakte.ru/id1

            filename = System.Text.RegularExpressions.Regex.Replace(filename, @"http(s|)://.*/", "");
            // хавает любое занчение: 47348352, club47348352, vkdriveapp

            if (filename.Length == 0)
            {
                return DokanResult.Error;
            }
            JObject apiResult;
            try
            {
                apiResult =
                    (JObject)
                    VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("users.get",
                        new Dictionary<string, string>() {{"uids", filename}}));
            }
            catch (Exception e)
            {
                if (e.Data.Contains("code") && e.Data["code"].ToString() == "113")
                {
                    Console.WriteLine(@"Invalid user id");
                }
                return DokanResult.Error;
            }

            ushort count = 0;
            JArray items = (JArray) apiResult.GetValue("items");
            foreach (JObject item in items)
            {
                SerializationObject.User user = item.ToObject<SerializationObject.User>();
                /*
                bool isDooble = false;
                foreach (VFile file in RootNode.Childs)
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
                }
                if (isDooble)
                {
                    count++;
                    continue;
                }*/

                if (user.FirstName == "DELETED" || user.Deactivated != null)
                {
                    continue;
                }

                VkStorage.Join(StorageKey, user.Id.ToString());

                file.ChildsAdd(CreateUserFolder(user));

                count++;
            }

            if (count > 0)
            {
                return DokanResult.Success;
            }
            return DokanResult.Error;
        }
    }
}