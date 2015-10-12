using Dokan;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private char drive;

        public MainFS(char drive)
        {
            this.drive = drive;
            DB.Instance.Connect();
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
        }

        public int Cleanup(string filename, DokanFileInfo info)
        {
            return 0;
        }

        public int CloseFile(string filename, DokanFileInfo info)
        {
            return 0;
        }

        public int CreateDirectory(string filename, DokanFileInfo info)
        {
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
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

            if (mode == System.IO.FileMode.Open)
            {
                FileInformation fileinfo = new FileInformation();
                int res = GetFileInformation(filename, fileinfo, info);
                return res;
            }
            return -1;
        }

        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                return -1;
            }
            return dir.DeleteDirectory(cutEntryPath(filename), info);
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
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
            return -1;
        }

        public int FindFiles(
            string filename,
            System.Collections.ArrayList files,
            DokanFileInfo info)
        {
            //Console.WriteLine("FindFiles " + filename);
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
                return 0;
            }
            if (inBlackList(filename))
            {
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }

            Dir key = GetDirEntry(filename);

            if (key == null)
                return -1;
            
            key.FindFiles(cutEntryPath(filename), files, info);
            return 0;
        }

        public int GetFileInformation(
            string filename,
            FileInformation fileinfo,
            DokanFileInfo info)
        {
            if (filename == "\\")
            {
                fileinfo.Attributes = System.IO.FileAttributes.Directory;
                fileinfo.Attributes |= System.IO.FileAttributes.ReadOnly;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.CreationTime = DateTime.Now;

                fileinfo.FileName = "\\";
                return DokanNet.DOKAN_SUCCESS;
            }

            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                foreach (VFile curFile in TopFiles)
                {
                    if (curFile.FileName == filename.Trim('\\'))
                    {
                        curFile.CopyTo(fileinfo);
                        return DokanNet.DOKAN_SUCCESS;
                    }
                }
                return -DokanNet.ERROR_FILE_NOT_FOUND;
            }

            string dirPath = cutEntryPath(filename);

            if (dirPath == "\\")
            {
                fileinfo.Attributes = System.IO.FileAttributes.Directory;
                fileinfo.Attributes |= System.IO.FileAttributes.ReadOnly;
                fileinfo.LastAccessTime = DateTime.Now;
                fileinfo.LastWriteTime = DateTime.Now;
                fileinfo.CreationTime = DateTime.Now;
                fileinfo.FileName = filename.Trim('\\');

                return DokanNet.DOKAN_SUCCESS;
            }
            if (inBlackList(filename))
            {
                return -DokanNet.ERROR_FILE_NOT_FOUND;
            }
            return dir.GetFileInformation(dirPath, fileinfo, info);
        }

        public int LockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            return 0;
        }

        public int MoveFile(
            string filename,
            string newname,
            bool replace,
            DokanFileInfo info)
        {
            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
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
            return 0;
            if (System.Threading.Thread.CurrentThread.Name == null)
            {
                System.Threading.Thread.CurrentThread.Name = "ReadFile thread";
            }
            

            Dir dir = GetDirEntry(filename);
            if (dir == null)
            {
                foreach (VFile curFile in TopFiles)
                {
                    if (curFile.FileName == filename.Trim('\\'))
                    {
                        curFile.ReadFile(buffer, ref readBytes, offset, info);


                        return DokanNet.DOKAN_SUCCESS;
                    }
                }
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }
            if (inBlackList(filename))
            {
                return DokanNet.ERROR_FILE_NOT_FOUND;
            }

            return dir.ReadFile(cutEntryPath(filename), buffer, ref readBytes, offset, info);
        }

        public bool inBlackList(string filename)
        {
            return filename.Length > 11 && filename.Substring(filename.Length - 11) == "desktop.ini";
        }

        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int SetFileAttributes(
            string filename,
            System.IO.FileAttributes attr,
            DokanFileInfo info)
        {
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
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            return 0;
        }

        public int GetDiskFreeSpace(
           ref ulong freeBytesAvailable,
           ref ulong totalBytes,
           ref ulong totalFreeBytes,
           DokanFileInfo info)
        {
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
            return -1;
        }

        public void clearCache()
        {
            foreach (KeyValuePair<string, Dir> kvp in TopDirectory)
            {
                kvp.Value.clearCache();
            }

            // Натравляем сборщик мусора на последнее поколение
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
        }

        #endregion
    

    }
}
