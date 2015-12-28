using Dokan;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using VKDrive.Files;
using VKDrive.VKAPI;

namespace VKDrive.Dris
{
    class DirSearch : Dir
    {
	    private const string StorageKey = "DirSearch";

	    public override void _LoadRootNode()
        {
            RootNode = new Folder("");
            RootNode.Property.Add("type", "storage.get:searchDir");
            RootNode.IsLoaded = false;
        }

        public override bool _LoadFile(Folder file)
        {
            file.ChildsAdd(new Settings("Добавить поисковый запрос.lnk"));
            file.ChildsAdd(
                new SettingsXls(
                    "VKDirvePathData.xml", "Добавить поисковый запрос",
                    "Введите ключевые слова для поиска",
                    "Странная ошибка. Программист не смог придумать в каком случае она возникнет.",
                    "Такой запрос уже существует."
                ));

            if (file.Property["type"] == "storage.get:searchDir")
            {
                var stringDir = API.VkStorage.Get(StorageKey);

	            if (stringDir.Length <= 0) return true;

	            var dirs = stringDir.Split('\n');
	            foreach (var folderName in dirs)
	            {
		            RootNode.ChildsAdd(new Folder(folderName, new Loader.VKontakte.Audio.Search(folderName)));
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

            SaveDirectoriesList();
            return DokanNet.DOKAN_SUCCESS;
        }

        private void SaveDirectoriesList()
        {
            string files = "";
            foreach( var file in RootNode.Childs ) {
                if(file.GetType() == typeof(Folder)){
                    files += file.FileName+"\n";
                }
            }
            API.VkStorage.Set(StorageKey, files.Trim());
        }

        public override int DeleteDirectory(string filename, DokanFileInfo info)
        {
            var node = FindFiles(filename);

            if (node == null)
                return -1;
            RootNode.Childs.Remove(node);
            SaveDirectoriesList();
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
