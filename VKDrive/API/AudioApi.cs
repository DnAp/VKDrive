using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using VKDrive.Files;

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
                string xml;
                xml = VKAPI.Instance.execute("audio.getAlbums", paramSend);
                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> albums = responce.Elements("album");
                System.Collections.ArrayList files2 = new System.Collections.ArrayList();

                string name;


                Folder fileNode;

                // 2 уровня
                if (albums.Count() > 0)
                {
                    max = Convert.ToInt32(responce.Element("count").Value);

                    foreach (XElement album in albums)
                    {
                        int key = Convert.ToInt32(album.Element("album_id").Value);
                        name = album.Element("title").Value;
                        fileNode = new Folder(name);
                        if (waitParam)
                        {
                            fileNode.Property.Add("type", "wait");
                        }
                        else
                        {
                            fileNode.Property.Add("type", "audio.getInAlbum");
                            fileNode.Property.Add("album_id", key.ToString());
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
                        albumsKeyValue.Add(key, fileNode);
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
            string countResult;
            if (param.ContainsKey("uid"))
            {
                countResult = VKAPI.Instance.execute("audio.getCount", new Dictionary<string, string>() { { "oid", param["uid"] } }, VKAPI.JSON);
            }
            else
            { // gid, а если упадет, так тебе и нужно
                countResult = VKAPI.Instance.execute("audio.getCount", new Dictionary<string, string>() { { "oid", "-" + param["gid"] } }, VKAPI.JSON);
            }
            countResult = countResult.Split(':')[1];
            countResult = countResult.Trim('}', '"');

            return Convert.ToInt32(countResult);
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
                string xml = VKAPI.Instance.execute("audio.get", paramSend);

                XElement responce = XElement.Parse(xml);
                IEnumerable<XElement> mp3List = responce.Elements("audio");
                foreach (XElement attr in mp3List)
                {

                    Mp3 finfo = new Mp3("", attr);

                    if (albumsKeyValue.Count == 0)
                    {
                        files.Add(finfo);
                    }
                    else
                    {
                        ((Folder)albumsKeyValue[0]).ChildsAdd(finfo);
                        XElement albumX = attr.Element("album");
                        if (albumX != null)
                        {
                            albimId = Convert.ToInt32(albumX.Value);
                            // Здесь ньюанс, в ChildsAdd есть уникальность которая изменяет имя. 
                            // Но так как в общий список мы добавляем первым, то имя файла будет уникальное всегда
                            ((Folder)albumsKeyValue[Convert.ToInt32(albumX.Value)]).ChildsAdd(finfo);
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
