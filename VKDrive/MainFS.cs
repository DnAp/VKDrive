using Dokan;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VKDrive.Dris;
using VKDrive.Files;

namespace VKDrive
{
    public class MainFs : DokanOperations 
    {

        #region DokanOperations member

        private Dictionary<string, Dir> _topDirectory;
        private List<VFile> _topFiles;
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MainFs()
        {
	        var drive = Properties.Settings.Default.MountPoint;
            _log.Debug("Start MainFS");
            _log.Debug("Db connect");
            try {
                Db.Instance.Connect();
            } catch(Exception e) {
                _log.Error("Db connect fail", e);
                throw e;
            }
            _log.Debug("Make root tree");
            _topDirectory = new Dictionary<string, Dir>();
            _topDirectory["Моя Страница"] = new DirMy();
            _topDirectory["Мои Друзья"] = new DirUsers();
            _topDirectory["Мои Группы"] = new DirGroups();
            _topDirectory["Поиск"] = new DirSearch();

            _topFiles = new List<VFile>();

            PlainText readme = new PlainText("Прочти меня.txt");
            #region Много текста в readme.txt
            readme.SetText("Привет!\n"+
                    "\n"+
                    "Ты установил приложение VkDrive.\n"+
                    "\n"+
                    "Это полноценный виртуальный диск с музыкой и фотографиями из ВКонтакте.\n"+
                    "Для открытия файлов не нужно ждать долгое время синхронизации. Открытие файла происходит в течение доли секунд.\n"+
                    "\n"+
                    "После установки у тебя в \"Мой компьютер\" появился новый виртуальный диск " + drive + ":\\\n" +
                    "На диске доступны папки:\n"+
                    "- Моя страница\n"+
                    "- Мои друзья\n"+
                    "- Мои группы\n"+
                    "- Поиск\n"+
                    "\n"+
                    "\n"+
                    /*"В бесплатной версии в папках страница, друзья, группы и поиск - тебе доступна музыка для для прослушивания (only read).\n"+
                    "\n"+
                    "В полной версии в папках страница, друзья, группы - доступен просмотр фотографий и загрузка фото в альбомы своей страницы.\n"+
                    "Кроме прослушивания музыки - становится доступна загрузка музыкальных файлов в свои аудиозаписи.\n"+
                    "\n"+
                    "Для получения полной версии - пожертвуйте 1000 руб. на электронный кошелек хххххххххх. После чего (описание манипуляций для получения полной версии программы).\n"+
                    "\n"+
                    */"P.S.: все файлы используются исключительно с серверов ВКонтакте для ознакомления и прослушивания. За сторонние копирования - автор программы ответственности не несёт.\n"+
                    "\n"+
                    "P.P.S.: ваши идеи, предложения, сообщения об ошибках присылайте на e-mail: xxx@yyy.ru");
            #endregion
            _topFiles.Add(readme);

            //TopDirectory["Test"] = new DirTest();
            //TopDirectory["Last.fm"] = new DirLastFm();*/

            _log.Debug("Start MainFS OK");
        }

        public int Cleanup(string filename, DokanFileInfo info)
        {
            //Log.Debug("MainFS Cleanup " + filename);
            return 0;
        }

        public int CloseFile(string filename, DokanFileInfo info)
        {
            //Log.Debug("MainFS Close file " + filename);
            return 0;
        }

        public int CreateDirectory(string filename, DokanFileInfo info)
        {
            _log.Debug("MainFS CerateDirectory " + filename);
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                _log.Debug("MainFS CerateDirectory " + filename + " not find");
                return DokanNet.DOKAN_ERROR;
            }
            return dir.CreateDirectory(CutEntryPath(filename).Trim('\\'), info);
        }

        public int CreateFile(
            string filename,
            System.IO.FileAccess access,
            System.IO.FileShare share,
            System.IO.FileMode mode,
            System.IO.FileOptions options,
            DokanFileInfo info)
        {
            _log.Debug("MainFS CreateFile " + filename);
            if (mode == System.IO.FileMode.Open)
            {
                _log.Debug("MainFS CreateFile " + filename + " GetFileInformation");
                FileInformation fileinfo = new FileInformation();
                int res = GetFileInformation(filename, fileinfo, info);
                _log.Debug("MainFS CreateFile " + filename + " ok");
                return res;
            }
            _log.Debug("MainFS CreateFile " + filename + " mode not open");
            return -1;
        }

        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            _log.Debug("MainFS DeleteDirectory " + filename);
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                _log.Debug("MainFS DeleteDirectory " + filename + " not found");
                return -1;
            }
            return dir.DeleteDirectory(CutEntryPath(filename), info);
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
            _log.Debug("MainFS DeleteFile " + filename);
            return -1;
        }

        private string CutEntryPath(string filename)
        {
            int top = filename.IndexOf('\\', 1);
            if (top == -1)
            {
                filename = "\\";
            }
            else
            {
                filename = filename.Substring(top);
            }
            return filename;
        }

        private Dir GetDirEntry(string name)
        {
            //Console.WriteLine("GetDirEntry : {0}", name);
            int top = name.IndexOf('\\', 1) - 1;
            if (top < 0)
                top = name.Length - 1;

            name = name.Substring(1, top);

            if (_topDirectory.ContainsKey(name))
            {
                return _topDirectory[name];
            }
            return null;
        }

        public int FlushFileBuffers(
            string filename,
            DokanFileInfo info)
        {
            _log.Debug("FlushFileBuffers " + filename);
            return -1;
        }

        public int FindFiles(
            string filename,
            System.Collections.ArrayList files,
            DokanFileInfo info)
        {
            try
            {

                _log.Debug("FindFiles " + filename);
                if (filename == "\\")
                {
                    foreach (string name in _topDirectory.Keys)
                    {
                        FileInformation finfo = new FileInformation();
                        finfo.FileName = name;
                        finfo.Attributes = System.IO.FileAttributes.Directory;
                        finfo.LastAccessTime = DateTime.Now;
                        finfo.LastWriteTime = DateTime.Now;
                        finfo.CreationTime = DateTime.Now;
                        files.Add(finfo);
                    }
                    files.AddRange(_topFiles);
                    _log.Debug("FindFiles " + filename + " OK return root");
                    return 0;
                }
                _log.Debug("FindFiles " + filename + " inBlackList");
                if (InBlackList(filename))
                {
                    _log.Debug("FindFiles " + filename + " black list");
                    return DokanNet.ERROR_FILE_NOT_FOUND;
                }
                _log.Debug("FindFiles " + filename + " GetDirEntry");
                Dir key = GetDirEntry(filename);

                if (key == null)
                {
                    _log.Debug("FindFiles " + filename + " not found dir");
                    return -1;
                }
                _log.Debug("FindFiles " + filename + " find dir " + key);
                key.FindFiles(CutEntryPath(filename), files, info);
                _log.Debug("FindFiles " + filename + " find ok");
                return 0;
            }
            catch (Exception e)
            {
                _log.Error(e);
                return -1;
            }
        }

        public int GetFileInformation(
            string filename,
            FileInformation fileinfo,
            DokanFileInfo info)
        {
            _log.Debug("MainFS GetFileInformation " + filename);
            if (InBlackList(filename))
            {
                _log.Debug("MainFS GetFileInformation " + filename + " black list");
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }
            if (filename == "\\")
            {
                fileinfo.Attributes = System.IO.FileAttributes.Directory;
                fileinfo.Attributes |= System.IO.FileAttributes.ReadOnly;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.CreationTime = DateTime.Now;

                fileinfo.FileName = "\\";
                _log.Debug("MainFS GetFileInformation " + filename + " root info");
                return DokanNet.DOKAN_SUCCESS;
            }

            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                _log.Debug("MainFS GetFileInformation " + filename + " dir find...");
                foreach (VFile curFile in _topFiles)
                {
                    if (curFile.FileName == filename.Trim('\\'))
                    {
                        curFile.CopyTo(fileinfo);
                        _log.Debug("MainFS GetFileInformation " + filename + " ok");
                        return DokanNet.DOKAN_SUCCESS;
                    }
                }
                _log.Debug("MainFS GetFileInformation " + filename + " file not find");
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }

            string dirPath = CutEntryPath(filename);

            if (dirPath == "\\") // todo В начале есть такой-же код, сгруппировать
            {
                fileinfo.Attributes = System.IO.FileAttributes.Directory;
                fileinfo.Attributes |= System.IO.FileAttributes.ReadOnly;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.CreationTime = DateTime.Now;
                fileinfo.FileName = filename.Trim('\\');
                _log.Debug("MainFS GetFileInformation " + filename + " root dir after cut");
                return DokanNet.DOKAN_SUCCESS;
            }
            
            _log.Debug("MainFS GetFileInformation " + filename + " to subdir");
            return dir.GetFileInformation(dirPath, fileinfo, info);
        }

        public int LockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            _log.Debug("MainFS LockFile " + filename);
            return 0;
        }

        public int MoveFile(
            string filename,
            string newname,
            bool replace,
            DokanFileInfo info)
        {
            _log.Debug("MainFS MoveFile " + filename);
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                _log.Debug("MainFS MoveFile " + filename + " directory not find");
                return -1;
            }

            return dir.MoveFile(CutEntryPath(filename), newname, replace, info);
        }

        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            return 0;
        }

        public int ReadFile(
            string filename,
            byte[] buffer,
            ref uint readBytes,
            long offset,
            DokanFileInfo info)
        {
            _log.Debug("MainFS ReadFile " + filename);
            if (System.Threading.Thread.CurrentThread.Name == null)
            {
                _log.Debug("Set thread name ReadFile " + filename);
                System.Threading.Thread.CurrentThread.Name = "ReadFile thread";
            }

            if (InBlackList(filename))
            {
                _log.Debug("MainFS ReadFile " + filename + " black list");
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }

            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                foreach (VFile curFile in _topFiles)
                {
                    if (curFile.FileName == filename.Trim('\\'))
                    {
                        curFile.ReadFile(buffer, ref readBytes, offset, info);
                        _log.Debug("MainFS ReadFile " + filename + " OK - null entry");
                        return DokanNet.DOKAN_SUCCESS;
                    }
                }
                _log.Debug("MainFS ReadFile " + filename + " NOT FIND - null entry");
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }

            int res = dir.ReadFile(CutEntryPath(filename), buffer, ref readBytes, offset, info);
            _log.Debug("MainFS ReadFile " + filename + " OK "+res);
            
            return res;
        }

        public bool InBlackList(string filename)
        {
            //Log.Debug("MainFS inBlackList " + filename + " l:"+ filename.Length);
            if (filename.Length >= 12)
            {
                //Log.Debug("MainFS inBlackList " + filename + " substr");
                string cutFileName = filename.Substring(filename.Length - 12);
                //Log.Debug("MainFS inBlackList " + filename + " : "+cutFileName);
                
                if(cutFileName == "\\desktop.ini" || cutFileName == "\\Desktop.ini" || cutFileName == "\\AutoRun.inf" )
                    return true;
            }
            return false;
        }

        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            _log.Debug("MainFS SetEndOfFile " + filename + "\t" + length);
            return -1;
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            _log.Debug("MainFS SetAllocationSize " + filename + "\t" + length);
            return -1;
        }

        public int SetFileAttributes(
            string filename,
            System.IO.FileAttributes attr,
            DokanFileInfo info)
        {
            _log.Debug("MainFS SetFileAttributes " + filename);
            return -1;
        }

        public int SetFileTime(
            string filename,
            DateTime ctime,
            DateTime atime,
            DateTime mtime,
            DokanFileInfo info)
        {
            return -1;
        }

        public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            _log.Debug("MainFS UnlockFile " + filename);
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            _log.Debug("MainFS Unmount ");
            return 0;
        }

        public int GetDiskFreeSpace(
           ref ulong freeBytesAvailable,
           ref ulong totalBytes,
           ref ulong totalFreeBytes,
           DokanFileInfo info)
        {
            //Log.Debug("MainFS GetDiskFreeSpace " );
            freeBytesAvailable = 512 * 1024 * 1024;
            totalBytes = 1024 * 1024 * 1024;
            totalFreeBytes = 512 * 1024 * 1024;
            /*if (VKMP3FSLogin.self.firtStartup)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("explorer");
                startInfo.Arguments = drive.ToString().ToUpper()+":";
                startInfo.UseShellExecute = true;
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(startInfo);
            }*/

            return 0;
        }

        public int WriteFile(
            string filename,
            byte[] buffer,
            ref uint writtenBytes,
            long offset,
            DokanFileInfo info)
        {
            _log.Debug("MainFS WriteFile " + filename);
            return -1;
        }

        public void ClearCache()
        {
            _log.Debug("MainFS clearCache ");
            foreach (KeyValuePair<string, Dir> kvp in _topDirectory)
            {
                kvp.Value.ClearCache();
            }

            // Натравляем сборщик мусора на последнее поколение
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);

            _log.Debug("MainFS clearCache OK");
        }

        #endregion
    

    }
}
