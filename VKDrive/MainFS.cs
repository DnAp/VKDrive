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
    public class MainFS : DokanOperations 
    {

        #region DokanOperations member

        private Dictionary<string, Dir> TopDirectory;
        private List<VFile> TopFiles;
        private readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MainFS(char drive)
        {
            Log.Debug("Start MainFS");
            Log.Debug("Db connect");
            DB.Instance.Connect();
            Log.Debug("Make root tree");
            TopDirectory = new Dictionary<string, Dir>();
            TopDirectory["Моя Страница"] = new DirMy();
            TopDirectory["Мои Друзья"] = new DirUsers();
            TopDirectory["Мои Группы"] = new DirGroups();
            TopDirectory["Поиск"] = new DirSearch();

            TopFiles = new List<VFile>();

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
            TopFiles.Add(readme);

            //TopDirectory["Test"] = new DirTest();
            //TopDirectory["Last.fm"] = new DirLastFm();*/

            Log.Debug("Start MainFS OK");
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
            Log.Debug("MainFS CerateDirectory " + filename);
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                Log.Debug("MainFS CerateDirectory " + filename + " not find");
                return DokanNet.DOKAN_ERROR;
            }
            return dir.CreateDirectory(cutEntryPath(filename).Trim('\\'), info);
        }

        public int CreateFile(
            string filename,
            System.IO.FileAccess access,
            System.IO.FileShare share,
            System.IO.FileMode mode,
            System.IO.FileOptions options,
            DokanFileInfo info)
        {
            Log.Debug("MainFS CreateFile " + filename);
            if (mode == System.IO.FileMode.Open)
            {
                Log.Debug("MainFS CreateFile " + filename + " GetFileInformation");
                FileInformation fileinfo = new FileInformation();
                int res = GetFileInformation(filename, fileinfo, info);
                Log.Debug("MainFS CreateFile " + filename + " ok");
                return res;
            }
            Log.Debug("MainFS CreateFile " + filename + " mode not open");
            return -1;
        }

        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            Log.Debug("MainFS DeleteDirectory " + filename);
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                Log.Debug("MainFS DeleteDirectory " + filename + " not found");
                return -1;
            }
            return dir.DeleteDirectory(cutEntryPath(filename), info);
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
            Log.Debug("MainFS DeleteFile " + filename);
            return -1;
        }

        private string cutEntryPath(string filename)
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

            if (TopDirectory.ContainsKey(name))
            {
                return TopDirectory[name];
            }
            return null;
        }

        public int FlushFileBuffers(
            string filename,
            DokanFileInfo info)
        {
            Log.Debug("FlushFileBuffers " + filename);
            return -1;
        }

        public int FindFiles(
            string filename,
            System.Collections.ArrayList files,
            DokanFileInfo info)
        {
            try
            {

                Log.Debug("FindFiles " + filename);
                if (filename == "\\")
                {
                    foreach (string name in TopDirectory.Keys)
                    {
                        FileInformation finfo = new FileInformation();
                        finfo.FileName = name;
                        finfo.Attributes = System.IO.FileAttributes.Directory;
                        finfo.LastAccessTime = DateTime.Now;
                        finfo.LastWriteTime = DateTime.Now;
                        finfo.CreationTime = DateTime.Now;
                        files.Add(finfo);
                    }
                    files.AddRange(TopFiles);
                    Log.Debug("FindFiles " + filename + " OK return root");
                    return 0;
                }
                Log.Debug("FindFiles " + filename + " inBlackList");
                if (inBlackList(filename))
                {
                    Log.Debug("FindFiles " + filename + " black list");
                    return DokanNet.ERROR_FILE_NOT_FOUND;
                }
                Log.Debug("FindFiles " + filename + " GetDirEntry");
                Dir key = GetDirEntry(filename);

                if (key == null)
                {
                    Log.Debug("FindFiles " + filename + " not found dir");
                    return -1;
                }
                Log.Debug("FindFiles " + filename + " find dir " + key);
                key.FindFiles(cutEntryPath(filename), files, info);
                Log.Debug("FindFiles " + filename + " find ok");
                return 0;
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw e;
            }
        }

        public int GetFileInformation(
            string filename,
            FileInformation fileinfo,
            DokanFileInfo info)
        {
            Log.Debug("MainFS GetFileInformation " + filename);
            if (inBlackList(filename))
            {
                Log.Debug("MainFS GetFileInformation " + filename + " black list");
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
                Log.Debug("MainFS GetFileInformation " + filename + " root info");
                return DokanNet.DOKAN_SUCCESS;
            }

            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                Log.Debug("MainFS GetFileInformation " + filename + " dir find...");
                foreach (VFile curFile in TopFiles)
                {
                    if (curFile.FileName == filename.Trim('\\'))
                    {
                        curFile.CopyTo(fileinfo);
                        Log.Debug("MainFS GetFileInformation " + filename + " ok");
                        return DokanNet.DOKAN_SUCCESS;
                    }
                }
                Log.Debug("MainFS GetFileInformation " + filename + " file not find");
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }

            string dirPath = cutEntryPath(filename);

            if (dirPath == "\\") // todo В начале есть такой-же код, сгруппировать
            {
                fileinfo.Attributes = System.IO.FileAttributes.Directory;
                fileinfo.Attributes |= System.IO.FileAttributes.ReadOnly;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.CreationTime = DateTime.Now;
                fileinfo.FileName = filename.Trim('\\');
                Log.Debug("MainFS GetFileInformation " + filename + " root dir after cut");
                return DokanNet.DOKAN_SUCCESS;
            }
            
            Log.Debug("MainFS GetFileInformation " + filename + " to subdir");
            return dir.GetFileInformation(dirPath, fileinfo, info);
        }

        public int LockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            Log.Debug("MainFS LockFile " + filename);
            return 0;
        }

        public int MoveFile(
            string filename,
            string newname,
            bool replace,
            DokanFileInfo info)
        {
            Log.Debug("MainFS MoveFile " + filename);
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                Log.Debug("MainFS MoveFile " + filename + " directory not find");
                return -1;
            }

            return dir.MoveFile(cutEntryPath(filename), newname, replace, info);
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
            Log.Debug("MainFS ReadFile " + filename);
            if (System.Threading.Thread.CurrentThread.Name == null)
            {
                Log.Debug("Set thread name ReadFile " + filename);
                System.Threading.Thread.CurrentThread.Name = "ReadFile thread";
            }

            if (inBlackList(filename))
            {
                Log.Debug("MainFS ReadFile " + filename + " black list");
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }

            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                foreach (VFile curFile in TopFiles)
                {
                    if (curFile.FileName == filename.Trim('\\'))
                    {
                        curFile.ReadFile(buffer, ref readBytes, offset, info);
                        Log.Debug("MainFS ReadFile " + filename + " OK - null entry");
                        return DokanNet.DOKAN_SUCCESS;
                    }
                }
                Log.Debug("MainFS ReadFile " + filename + " NOT FIND - null entry");
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }

            int res = dir.ReadFile(cutEntryPath(filename), buffer, ref readBytes, offset, info);
            Log.Debug("MainFS ReadFile " + filename + " OK "+res);
            
            return res;
        }

        public bool inBlackList(string filename)
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
            Log.Debug("MainFS SetEndOfFile " + filename + "\t" + length);
            return -1;
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            Log.Debug("MainFS SetAllocationSize " + filename + "\t" + length);
            return -1;
        }

        public int SetFileAttributes(
            string filename,
            System.IO.FileAttributes attr,
            DokanFileInfo info)
        {
            Log.Debug("MainFS SetFileAttributes " + filename);
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
            Log.Debug("MainFS UnlockFile " + filename);
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            Log.Debug("MainFS Unmount ");
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
            Log.Debug("MainFS WriteFile " + filename);
            return -1;
        }

        public void clearCache()
        {
            Log.Debug("MainFS clearCache ");
            foreach (KeyValuePair<string, Dir> kvp in TopDirectory)
            {
                kvp.Value.clearCache();
            }

            // Натравляем сборщик мусора на последнее поколение
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);

            Log.Debug("MainFS clearCache OK");
        }

        #endregion
    

    }
}
