using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using VKDrive.Files;
using VKDrive.VKAPI;

namespace VKDrive.API
{
    class AudioApi
    {
        public const int LOADER_AUDIO = 1;
        public const int WAIT = 2;

        /// <summary>
        /// Грузим рекурсивно альбомы по 100 штук
        /// </summary>
        /// <param name="albumsKeyValue"></param>
        /// <param name="param"></param>
        /// <param name="files"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static bool executeGetAlbumsRecursive(Dictionary<int, Folder> albumsKeyValue, Dictionary<string, string> param,
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

                JObject apiRequest = (JObject)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("audio.getAlbums", paramSend));
                
                JArray items = (JArray)apiRequest.GetValue("items");
                if (items.Count > 0)
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
                            if (param.ContainsKey("uid"))
                            {
                                fileNode.Property.Add("uid", param["uid"]);
                            }
                            else
                            {
                                fileNode.Property.Add("gid", param["gid"]);
                            }
                        }
                        fileNode.IsLoaded = false;
                        files.Add(fileNode);
                        albumsKeyValue.Add(album.Id, fileNode);
                    }
                }
                
                if (max > -1)
                {
                    executeGetAlbumsRecursive(albumsKeyValue, param, files, max, waitParam);
                }
            }

            return true;
        }

        public static int executeGetAlbums(Dictionary<string, string> param, IList<VFile> files)
        {
            Dictionary<int, Folder> albumsKeyValue = new Dictionary<int, Folder>();
            Folder fileNode;

            Dictionary<string, string> paramSend = new Dictionary<string, string>(param);
            paramSend.Add("count", "100");

            int audioCount = getCount(param);

            executeGetAlbumsRecursive(albumsKeyValue, paramSend, files, -1, audioCount <= 5000);
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
                    new ThreadExecutor().Execute(() => loadMP3(param, files, albumsKeyValue, audioCount));
                }
            }
            else
            {
                loadMP3(param, files, albumsKeyValue, audioCount);
            }

            return 0;
        }

        protected static int getCount(Dictionary<string, string> param)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            // gid, а если упадет, так тебе и нужно
            p.Add("oid", param.ContainsKey("uid") ? param["uid"] : "-" + param["gid"]);
            return VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("audio.getCount", p)).ToObject<int>();
        }

        public static void loadMP3(Dictionary<string, string> param, IList<VFile> files)
        {
            loadMP3(param, files, new Dictionary<int, Folder>(), 500);
        }

        protected static void loadMP3(Dictionary<string, string> param, IList<VFile> files, Dictionary<int, Folder> albumsKeyValue, int audioCount)
        {
            
            // второй уровень
            string mp3Name;
            int albimId;
            int countOnStep = 500; // Сколько забирать за шаг
            int pageCount = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(audioCount) / countOnStep));

            Dictionary<string, string> paramSend = new Dictionary<string, string>(param);

            paramSend.Add("count", countOnStep.ToString());
            paramSend.Add("offset", "0");

            for (int page = 0; page < pageCount; page++)
            {
                paramSend["offset"] = (page * countOnStep).ToString();
                JObject apiRequest = (JObject)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("audio.get", paramSend));
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
