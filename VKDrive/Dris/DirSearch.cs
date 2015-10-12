using Dokan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VKDrive.Files;

namespace VKDrive.Dris
{
    class DirSearch : Dir
    {
        const string STORAGE_KEY = "DirSearch";

        public override void _LoadRootNode()
        {
            RootNode = new Folder("");
            RootNode.Property.Add("type", "storage.get:searchDir");
            RootNode.IsLoaded = false;
        }

        public override bool _LoadFile(Files.Folder file)
        {
            file.ChildsAdd(new Files.Settings("Добавить поисковый запрос.lnk"));
            file.ChildsAdd(
                new Files.SettingsXls(
                    "VKDirvePathData.xml", "Добавить поисковый запрос",
                    "Введите ключевые слова для поиска",
                    "Странная ошибка. Программист не смог придумать в каком случае она возникнет.",
                    "Такой запрос уже существует."
                ));

            if (file.Property["type"] == "storage.get:searchDir")
            {
                string stringDir = API.VKStorage.get(STORAGE_KEY);

                if (stringDir.Length > 0)
                {
                    string[] dirs = stringDir.Split('\n');
                    Folder folder;
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        folder = new Folder(dirs[i]);
                        folder.Property.Add("type", "search");
                        RootNode.ChildsAdd(folder);
                    }
                }
            }
            else if (file.Property["type"] == "search")
            {
                string xml = VKAPI.Instance.execute("audio.search", new Dictionary<string, string>(){
				    {"q", file.FileName},
                    {"count", "200"}
			    });

                IEnumerable<XElement> audio = XElement.Parse(xml).Elements("audio");
                foreach (XElement curAudio in audio)
                {
                    file.ChildsAdd(new Mp3("", curAudio));
                }
            }
            else
            {
                return false;
            }
            return true;
        }


        public override int CreateDirectory(string filename, DokanFileInfo info)
        {
            Folder folder = new Folder(filename);
            folder.Property.Add("type", "search");
            RootNode.ChildsAdd(folder);

            saveDirectoriesList();
            return DokanNet.DOKAN_SUCCESS;
        }

        private void saveDirectoriesList()
        {
            string files = "";
            foreach( VFile file in RootNode.Childs ) {
                if(file.GetType() == typeof(Folder)){
                    files += file.FileName+"\n";
                }
            }
            API.VKStorage.set(STORAGE_KEY, files.Trim());
        }

        public override int DeleteDirectory(string filename, DokanFileInfo info)
        {
            VFile node = FindFiles(filename);

            if (node == null)
                return -1;
            RootNode.Childs.Remove(node);
            saveDirectoriesList();
            return DokanNet.DOKAN_SUCCESS;
        }


        public override int MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            this.DeleteDirectory(oldName, info);
            this.CreateDirectory(newName, info);
            return 0;
        }
    }
}
