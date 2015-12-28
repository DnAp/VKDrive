using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using VKDrive.Files;
using VKDrive.VKAPI;

namespace VKDrive.API
{
    class AudioApi
    {
        public const int LoaderAudio = 1;
        public const int Wait = 2;

        /// <summary>
        /// Грузим рекурсивно альбомы по 100 штук
        /// </summary>
        /// <param name="albumsKeyValue"></param>
        /// <param name="param"></param>
        /// <param name="files"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static bool ExecuteGetAlbumsRecursive(Dictionary<int, Folder> albumsKeyValue, Dictionary<string, string> param,
                                            IList<VFile> files, int max, bool waitParam)
        {
            if (max > albumsKeyValue.Count || max == -1)
            {
                Dictionary<string, string> paramSend = new Dictionary<string, string>(param);
                if (max > -1)
                {
                    paramSend.Add("offset", albumsKeyValue.Count.ToString());
                }

                Folder fileNode;

                JObject apiRequest = (JObject)VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("audio.getAlbums", paramSend));
                
                JArray items = (JArray)apiRequest.GetValue("items");
                if (items!=null && items.Count > 0)
                {
                    max = apiRequest.GetValue("count").ToObject<int>();
                    foreach (JToken item in items)
                    {
                        SerializationObject.Album album = item.ToObject<SerializationObject.Album>();

                        fileNode = new Folder(album.Title);
                        if (waitParam)
                        {
                            fileNode.Property.Add("type", "wait");
                        }
                        else
                        {
                            fileNode.Property.Add("type", "audio.getInAlbum");
                            fileNode.Property.Add("album_id", album.Id.ToString());
                            fileNode.Property.Add("owner_id", param.ContainsKey("uid") ? param["uid"] : "-"+param["gid"]);
                        }
                        fileNode.IsLoaded = false;
                        files.Add(fileNode);
                        albumsKeyValue.Add(album.Id, fileNode);
                    }
                }
                
                if (max > -1)
                {
                    ExecuteGetAlbumsRecursive(albumsKeyValue, param, files, max, waitParam);
                }
            }

            return true;
        }

        public static int ExecuteGetAlbums(Dictionary<string, string> param, IList<VFile> files)
        {
            Dictionary<int, Folder> albumsKeyValue = new Dictionary<int, Folder>();
            Folder fileNode;

            Dictionary<string, string> paramSend = new Dictionary<string, string>(param);
            paramSend.Add("count", "100");

            int audioCount = GetCount(param);

            ExecuteGetAlbumsRecursive(albumsKeyValue, paramSend, files, -1, audioCount <= 5000);
            if (albumsKeyValue.Count > 0)
            {
                if (audioCount <= 5000) // это максимум вк, если больше то нужно получать альбомами
                {
                    // Мы получили список альбомов по сути это все что нам сейчас нужно.
                    fileNode = new Folder("Все аудиозаписи");
                    fileNode.Property.Add("type", "wait");
                    files.Add(fileNode);
                    albumsKeyValue.Add(0, fileNode);
                    // Теперь мы свободны и запускаем в отдельном потоке загрузку музыки
                    new ThreadExecutor().Execute(() => LoadMp3(param, files, albumsKeyValue, audioCount));
                }
            }
            else
            {
                LoadMp3(param, files, albumsKeyValue, audioCount);
            }

            return 0;
        }

        protected static int GetCount(Dictionary<string, string> param)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("owner_id", param["owner_id"]);
            return VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("audio.GetCount", p)).ToObject<int>();
        }

        public static void LoadMp3(Dictionary<string, string> param, IList<VFile> files)
        {
            LoadMp3(param, files, new Dictionary<int, Folder>(), 500);
        }

        protected static void LoadMp3(Dictionary<string, string> param, IList<VFile> files, Dictionary<int, Folder> albumsKeyValue, int audioCount)
        {
            // второй уровень
            int countOnStep = 500; // Сколько забирать за шаг
            int pageCount = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(audioCount) / countOnStep));

            Dictionary<string, string> paramSend = new Dictionary<string, string>(param);

            paramSend.Add("count", countOnStep.ToString());
            paramSend.Add("offset", "0");

            for (int page = 0; page < pageCount; page++)
            {
                paramSend["offset"] = (page * countOnStep).ToString();
                JObject apiRequest = (JObject)VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("audio.get", paramSend));
                JArray items = (JArray)apiRequest.GetValue("items");
                foreach (JToken item in items)
                {
                    SerializationObject.Audio audio = item.ToObject<SerializationObject.Audio>();
                    Mp3 finfo = new Mp3(audio);
                    if (albumsKeyValue.Count == 0)
                    {
                        files.Add(finfo);
                    }
                    else
                    {
                        ((Folder)albumsKeyValue[0]).ChildsAdd(finfo);
                        if (audio.AlbumId != 0)
                        {
                            // Здесь ньюанс, в ChildsAdd есть уникальность которая изменяет имя. 
                            // Но так как в общий список мы добавляем первым, то имя файла будет уникальное всегда
                            ((Folder)albumsKeyValue[audio.AlbumId]).ChildsAdd(finfo);
                        }
                    }
                }
                
            }

            foreach (Folder folder in albumsKeyValue.Values)
            {
                folder.IsLoaded = true;
            }

        }
    }
}
