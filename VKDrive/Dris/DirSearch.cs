using DokanNet;
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
				return true;
			}
	        return false;
        }


        public override NtStatus CreateDirectory(string filename, DokanFileInfo info)
        {
            RootNode.ChildsAdd(new Folder(filename, new Loader.VKontakte.Audio.Search(filename)));

            SaveDirectoriesList();
            return DokanResult.Success;
        }

        private void SaveDirectoriesList()
        {
            var files = "";
	        foreach (var file in RootNode.Childs)
	        {
		        if (file.GetType() == typeof (Folder))
		        {
			        files += file.FileName + "\n";
		        }
	        }
	        API.VkStorage.Set(StorageKey, files.Trim());
        }

        public override NtStatus DeleteDirectory(string filename, DokanFileInfo info)
        {
            var node = FindFiles(filename);

            if (node == null)
                return DokanResult.FileNotFound;
            RootNode.Childs.Remove(node);
            SaveDirectoriesList();
            return DokanResult.Success;
        }


        public override NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            this.DeleteDirectory(oldName, info);
            this.CreateDirectory(newName, info);
            return DokanResult.Success;
        }
    }
}
