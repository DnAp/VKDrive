using Dokan;
using System;
using System.Collections.Generic;
using VKDrive.API;
using VKDrive.Files;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using VKDrive.VKAPI;

namespace VKDrive.Dris
{
	internal class DirGroups : Dir
    {
        const string StorageKey = "DirGroups";

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
                        "Введите ссылку на группу или ее идентификатор. Например https://vk.com/club47348352",
                        "Не удалось найти такую группу.",
                        "Такая группа уже существует."
                    ));

				JObject apiResult = (JObject)Vkapi.Instance.StartTaskSync(new ApiQuery("groups.get", new Dictionary<string, string> { { "extended", "1" } }));
				JArray items = (JArray)apiResult.GetValue("items");

                List<int> gruopIds = new List<int>();

                foreach (JObject item in items)
                {
                    SerializationObject.Group group = item.ToObject<SerializationObject.Group>();
                    file.ChildsAdd(CreateGroupFolder(group));
                    gruopIds.Add(group.Id);
                }
                
                string gids = VkStorage.Get(StorageKey);
                if (gids.Length > 0)
                {
                    JArray values = (JArray)Vkapi.Instance.StartTaskSync(new ApiQuery("groups.getById", new Dictionary<string, string> { { "group_ids", gids.Replace('\n', ',') } }));

                    foreach (JObject item in items)
                    {
                        SerializationObject.Group group = item.ToObject<SerializationObject.Group>();
                        if (gruopIds.IndexOf(group.Id) > -1)
                            continue;
                        file.ChildsAdd(CreateGroupFolder(group));
                    }
                }
                
            }
            else if (file.Property["type"] == "AudioApi.ExecuteGetAlbums")
            {
                //// 15:Access denied: group audio is disabled
                try
                {
                    AudioApi.ExecuteGetAlbums(new Dictionary<string, string>(){
				        {"owner_id", "-"+file.Property["gid"]}
			        }, file.Childs);
                }
                catch (Exception e)
                {
                    if(e.Data.Contains("code") && e.Data["code"].ToString() == "15" ){
                        // 15:Access denied: group photos are disabled

                        var readmePlainText = new PlainText("Аудиозаписи отключены.txt");
                        readmePlainText.SetText(PlainText.GetSubscript());
                        file.ChildsAdd(readmePlainText);
                        return true;
                    }
                    return false;
                
                }
                
            }
            else if (file.Property["type"] == "audio.getInAlbum")
            {
                AudioApi.LoadMp3(new Dictionary<string, string>(){
				        {"gid", file.Property["gid"]},
                        { "album_id", file.Property["album_id"]}
			        }, file.Childs);
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

        private Folder CreateGroupFolder(SerializationObject.Group group)
        {
            string gid = group.Id.ToString();
            var curFolder = new Folder(group.Name);
            curFolder.Property.Add("gid", gid);

            Folder subFolder = new Folder("Аудиозаписи");
            subFolder.Property.Add("type", "AudioApi.ExecuteGetAlbums");
            subFolder.Property.Add("inStorage", "1");
            subFolder.Property.Add("gid", gid);
            curFolder.ChildsAdd(subFolder);

            subFolder = new Folder("Фотографии", new Loader.VKontakte.Photos.GetAlbums(-group.Id));
            curFolder.ChildsAdd(subFolder);

			subFolder = new Folder("Стена(beta)", new Loader.VKontakte.Wall.Get(-group.Id));
			curFolder.ChildsAdd(subFolder);

			curFolder.ChildsAdd(
                new PlainText(
                    "Открыть в браузере.url",
                    PlainText.InternetShortcut("https://vk.com/club" + gid)
                )
            );

            curFolder.IsLoaded = true;
            return curFolder;
        }

        public override int CreateDirectory(Folder file, string filename, DokanFileInfo info)
        {
            // https://vk.com/club47348352
            // https://vk.com/vkdriveapp
            // https://vkontakte.ru/club47348352
            
            filename = Regex.Replace(filename, @"http(s|)://.*/", "");
            // хавает любое занчение: 47348352, club47348352, vkdriveapp

            if (filename.Length == 0)
            {
                return DokanNet.DOKAN_ERROR;
            }
            JArray items;
            try
            {
                items = (JArray)VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("groups.getById", new Dictionary<string, string>() { { "group_id", filename } }));
            }
            catch (Exception e)
            {
                if (e.Data.Contains("code") && e.Data["code"].ToString() == "125")
                {
                    Console.WriteLine("Invalid group id");
                }
                return DokanNet.DOKAN_ERROR;
            }

            ushort count = 0;
            foreach (JObject item in items)
            {
                SerializationObject.Group group = item.ToObject<SerializationObject.Group>();

                string gid = group.Id.ToString();
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

                if (group.Deactivated != null)
                {
                    continue;
                }

                API.VkStorage.Join(StorageKey, gid);

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
                API.VkStorage.Remove(StorageKey, ((Folder)node).Property["gid"]);
                return DokanNet.DOKAN_SUCCESS;
            }

            return DokanNet.DOKAN_ERROR;

            
        }



    }
}
