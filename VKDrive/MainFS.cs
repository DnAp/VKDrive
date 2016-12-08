using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using DokanNet;
using VKDrive.Dris;
using VKDrive.Files;
using FileAccess = DokanNet.FileAccess;

namespace VKDrive
{
    public class MainFs : IDokanOperations
    {
        private Dictionary<string, Dir> _rootDirectory;
        private readonly List<VFile> _topFiles;
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MainFs()
        {
            var drive = Properties.Settings.Default.MountPoint;
            _log.Debug("Start MainFS");
            _log.Debug("Db connect");
            try
            {
                Db.Instance.Connect();
            }
            catch (Exception e)
            {
                _log.Error("Db connect fail", e);
                throw;
            }
            _log.Debug("Make root tree");
            MakeRootDirectory();

            _topFiles = new List<VFile>();

            var readme = new PlainText("Прочти меня.txt");

            #region Много текста в readme.txt

            readme.SetText("Привет!\n" +
                           "\n" +
                           "Ты установил приложение VkDrive.\n" +
                           "\n" +
                           "Это полноценный виртуальный диск с музыкой и фотографиями из ВКонтакте.\n" +
                           "Для открытия файлов не нужно ждать долгое время синхронизации. Открытие файла происходит в течение доли секунд.\n" +
                           "\n" +
                           "После установки у тебя в \"Мой компьютер\" появился новый виртуальный диск " + drive +
                           ":\\\n" +
                           "На диске доступны папки:\n" +
                           "- Моя страница\n" +
                           "- Мои друзья\n" +
                           "- Мои группы\n" +
                           "- Поиск\n" +
                           "\n" +
                           "\n" +
                           /*"В бесплатной версии в папках страница, друзья, группы и поиск - тебе доступна музыка для для прослушивания (only read).\n"+
                    "\n"+
                    "В полной версии в папках страница, друзья, группы - доступен просмотр фотографий и загрузка фото в альбомы своей страницы.\n"+
                    "Кроме прослушивания музыки - становится доступна загрузка музыкальных файлов в свои аудиозаписи.\n"+
                    "\n"+
                    "Для получения полной версии - пожертвуйте 1000 руб. на электронный кошелек хххххххххх. После чего (описание манипуляций для получения полной версии программы).\n"+
                    "\n"+
                    */
                           "P.S.: все файлы используются исключительно с серверов ВКонтакте для ознакомления и прослушивания. За сторонние копирования - автор программы ответственности не несёт.\n" +
                           "\n" +
                           "P.P.S.: ваши идеи, предложения, сообщения об ошибках присылайте на e-mail: xxx@yyy.ru");

            #endregion

            _topFiles.Add(readme);

            _topFiles.Add(new PlainText("Официальная группа.url",
                PlainText.InternetShortcut("https://vk.com/vkdriveapp")));
            _topFiles.Add(new Settings("Обновить все.lnk", "--GC"));

            //TopDirectory["Test"] = new DirTest();
            //TopDirectory["Last.fm"] = new DirLastFm();*/

            _log.Debug("Start MainFS OK");

            // clear root directory
            var makeRootTimer = new System.Timers.Timer(5*60*1000) {Enabled = true};
            makeRootTimer.Elapsed += (sender, args) =>
            {
                MakeRootDirectory();
                GC.Collect();
            };
            makeRootTimer.Start();
        }

        public NtStatus Mounted(DokanFileInfo info)
        {
            return DokanResult.Success;
        }

        private void MakeRootDirectory()
        {
            _rootDirectory = new Dictionary<string, Dir>
            {
                ["Моя Страница"] = new DirMy(),
                ["Мои Друзья"] = new DirUsers(),
                ["Мои Группы"] = new DirGroups(),
                ["Поиск"] = new DirSearch()
            };
        }

        public void Cleanup(string filename, DokanFileInfo info)
        {
            //_log.Debug("MainFS Cleanup " + filename);
        }


        public void CloseFile(string filename, DokanFileInfo info)
        {
            //Log.Debug("MainFS Close file " + filename);
        }

        public NtStatus CreateDirectory(string filename, DokanFileInfo info)
        {
            _log.Debug("MainFS CerateDirectory " + filename);
            var dir = GetDirEntry(filename);
            if (dir != null)
                return dir.CreateDirectory(CutEntryPath(filename).Trim('\\'), info);

            if (filename.Length > 9 && filename.Substring(0, 9) == "\\_SYSTEM\\")
            {
                var cmd = filename.Substring(9);
                _log.Debug("MainFS run cmd" + cmd);
                switch (cmd)
                {
                    case "GC":
                        MakeRootDirectory();
                        GC.Collect();
                        break;
                }
                return DokanResult.Success;
            }

            _log.Debug("MainFS CerateDirectory " + filename + " not find");
            return DokanResult.Error;
        }

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode,
            FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            _log.Debug("MainFS CreateFile " + fileName);
            if (mode == FileMode.Open)
            {
                _log.Debug("MainFS CreateFile " + fileName + " GetFileInformation");
                FileInformation fileInfo;
                var res = GetFileInformation(fileName, out fileInfo, info);
                _log.Debug("MainFS CreateFile " + fileName + " ok");
                return res;
            }
            _log.Debug("MainFS CreateFile " + fileName + " mode not open");
            return DokanResult.Error;
        }

        public NtStatus DeleteDirectory(string filename, DokanFileInfo info)
        {
            _log.Debug("MainFS DeleteDirectory " + filename);
            var dir = GetDirEntry(filename);
            if (dir != null)
                return dir.DeleteDirectory(CutEntryPath(filename), info);
            _log.Debug("MainFS DeleteDirectory " + filename + " not found");
            return DokanResult.Error;
        }

        public NtStatus DeleteFile(string filename, DokanFileInfo info)
        {
            _log.Debug("MainFS DeleteFile " + filename);
            return DokanResult.Error;
        }

        private static string CutEntryPath(string filename)
        {
            var top = filename.IndexOf('\\', 1);
            filename = top == -1 ? "\\" : filename.Substring(top);
            return filename;
        }

        private Dir GetDirEntry(string name)
        {
            //Console.WriteLine("GetDirEntry : {0}", name);
            var top = name.IndexOf('\\', 1) - 1;
            if (top < 0)
                top = name.Length - 1;

            name = name.Substring(1, top);

            return _rootDirectory.ContainsKey(name) ? _rootDirectory[name] : null;
        }

        public NtStatus FlushFileBuffers(
            string filename,
            DokanFileInfo info)
        {
            _log.Debug("FlushFileBuffers " + filename);
            return DokanResult.Error;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = new FileInformation[0];
            return DokanResult.NotImplemented;
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        {
            files = null;
            return DokanResult.NotImplemented;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            _log.Debug("FindFiles " + fileName);
            files = new List<FileInformation>();
            try
            {
                if (fileName == "\\")
                {
                    foreach (var name in _rootDirectory.Keys)
                    {
                        var finfo = new FileInformation
                        {
                            FileName = name,
                            Attributes = FileAttributes.Directory,
                            LastAccessTime = DateTime.Now,
                            LastWriteTime = DateTime.Now,
                            CreationTime = DateTime.Now
                        };
                        files.Add(finfo);
                    }
                    ((List<FileInformation>)files).AddRange(_topFiles.Select(vFile => vFile.Cast()));
                    _log.Debug("FindFiles " + fileName + " OK return root");
                    return 0;
                }
                _log.Debug("FindFiles " + fileName + " inBlackList");
                if (InBlackList(fileName))
                {
                    _log.Debug("FindFiles " + fileName + " black list");
                    return DokanResult.FileNotFound;
                }
                _log.Debug("FindFiles " + fileName + " GetDirEntry");
                var key = GetDirEntry(fileName);

                if (key == null)
                {
                    _log.Debug("FindFiles " + fileName + " not found dir");
                    return DokanResult.Error;
                }
                _log.Debug("FindFiles " + fileName + " find dir " + key);
                key.FindFiles(CutEntryPath(fileName), (List<FileInformation>)files, info);
                _log.Debug("FindFiles " + fileName + " find ok");
                return DokanResult.Success;
            }
            catch (Exception e)
            {
                _log.Error(e);
                return DokanResult.Error;
            }
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            fileInfo = new FileInformation();
            _log.Debug("MainFS GetFileInformation " + fileName);
            if (InBlackList(fileName))
            {
                _log.Debug("MainFS GetFileInformation " + fileName + " black list");
                return DokanResult.FileNotFound;
            }
            if (fileName == "\\")
            {
                fileInfo.Attributes = FileAttributes.Directory;
                fileInfo.Attributes |= FileAttributes.ReadOnly;
                fileInfo.LastAccessTime = DateTime.Now;
                fileInfo.LastWriteTime = DateTime.Now;
                fileInfo.CreationTime = DateTime.Now;

                fileInfo.FileName = "\\";
                _log.Debug("MainFS GetFileInformation " + fileName + " root info");
                return DokanResult.Success;
            }

            var dir = GetDirEntry(fileName);
            if (dir == null)
            {
                _log.Debug("MainFS GetFileInformation " + fileName + " dir find...");
                foreach (var curFile in _topFiles)
                {
                    if (curFile.FileName == fileName.Trim('\\'))
                    {
                        curFile.CopyTo(fileInfo);
                        _log.Debug("MainFS GetFileInformation " + fileName + " ok");
                        return DokanResult.Success;
                    }
                }
                _log.Debug("MainFS GetFileInformation " + fileName + " file not find");
                return DokanResult.FileNotFound;
            }

            var dirPath = CutEntryPath(fileName);

            if (dirPath == "\\") // todo В начале есть такой-же код, сгруппировать
            {
                fileInfo.Attributes = FileAttributes.Directory;
                fileInfo.Attributes |= FileAttributes.ReadOnly;
                fileInfo.LastAccessTime = DateTime.Now;
                fileInfo.LastWriteTime = DateTime.Now;
                fileInfo.CreationTime = DateTime.Now;
                fileInfo.FileName = fileName.Trim('\\');
                _log.Debug("MainFS GetFileInformation " + fileName + " root dir after cut");
                return DokanResult.Success;
            }

            _log.Debug("MainFS GetFileInformation " + fileName + " to subdir");
            return dir.GetFileInformation(dirPath, fileInfo, info);
        }

        public NtStatus LockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            _log.Debug("MainFS LockFile " + filename);
            return DokanResult.Success;
        }

        public NtStatus MoveFile(
            string filename,
            string newname,
            bool replace,
            DokanFileInfo info)
        {
            _log.Debug("MainFS MoveFile " + filename);
            var dir = GetDirEntry(filename);
            if (dir == null)
            {
                _log.Debug("MainFS MoveFile " + filename + " directory not find");
                return DokanResult.Error;
            }

            return dir.MoveFile(CutEntryPath(filename), newname, replace, info);
        }

        /*
        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            return 0;
        }*/

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            _log.Debug("MainFS ReadFile " + fileName);
            bytesRead = 0;
            if (System.Threading.Thread.CurrentThread.Name == null)
            {
                _log.Debug("Set thread name ReadFile " + fileName);
                System.Threading.Thread.CurrentThread.Name = "ReadFile thread";
            }

            if (InBlackList(fileName))
            {
                _log.Debug("MainFS ReadFile " + fileName + " black list");
                return DokanResult.FileNotFound;
            }

            var dir = GetDirEntry(fileName);
            if (dir == null)
            {
                foreach (var curFile in _topFiles)
                {
                    if (curFile.FileName == fileName.Trim('\\'))
                    {
                        curFile.ReadFile(buffer, ref bytesRead, offset, info);
                        _log.Debug("MainFS ReadFile " + fileName + " OK - null entry");
                        return DokanResult.Success;
                    }
                }
                _log.Debug("MainFS ReadFile " + fileName + " NOT FIND - null entry");
                return DokanResult.FileNotFound;
            }

            var res = dir.ReadFile(CutEntryPath(fileName), buffer, ref bytesRead, offset, info);
            _log.Debug("MainFS ReadFile " + fileName + " OK " + res);

            return res;
        }

        public bool InBlackList(string filename)
        {
            //Log.Debug("MainFS inBlackList " + filename + " l:"+ filename.Length);
            if (filename.Length < 12)
                return false;
            //Log.Debug("MainFS inBlackList " + filename + " substr");
            var cutFileName = filename.Substring(filename.Length - 12);
            //Log.Debug("MainFS inBlackList " + filename + " : "+cutFileName);

            if (cutFileName == "\\desktop.ini" || cutFileName == "\\Desktop.ini" || cutFileName == "\\AutoRun.inf")
                return true;
            return false;
        }

        public NtStatus SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            _log.Debug("MainFS SetEndOfFile " + filename + "\t" + length);
            return DokanResult.Error;
        }

        public NtStatus SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            _log.Debug("MainFS SetAllocationSize " + filename + "\t" + length);
            return DokanResult.Error;
        }

        public NtStatus SetFileAttributes(
            string filename,
            FileAttributes attr,
            DokanFileInfo info)
        {
            _log.Debug("MainFS SetFileAttributes " + filename);
            return DokanResult.Error;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
            DateTime? lastWriteTime, DokanFileInfo info)
        {
            return DokanResult.Error;
        }

        public NtStatus UnlockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            _log.Debug("MainFS UnlockFile " + filename);
            return DokanResult.Success;
        }

        public NtStatus Unmounted(DokanFileInfo info)
        {
            _log.Debug("MainFS Unmount ");
            return DokanResult.Success;
        }

        // ReSharper disable once RedundantAssignment
        public NtStatus GetDiskFreeSpace(out long free, out long total, out long used, DokanFileInfo info)
        {
            //Log.Debug("MainFS GetDiskFreeSpace " );
            free = 512*1024*1024;
            total = 1024*1024*1024;
            used = 512*1024*1024;
            /*if (VKMP3FSLogin.self.firtStartup)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("explorer");
                startInfo.Arguments = drive.ToString().ToUpper()+":";
                startInfo.UseShellExecute = true;
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(startInfo);
            }*/

            return DokanResult.Success;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName,
            DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections,
            DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections,
            DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            _log.Debug("MainFS WriteFile " + fileName);
            bytesWritten = 0;
            return DokanResult.Error;
        }

        public void ClearCache()
        {
            _log.Debug("MainFS clearCache ");
            foreach (var kvp in _rootDirectory)
            {
                kvp.Value.ClearCache();
            }

            // Натравляем сборщик мусора на последнее поколение
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);

            _log.Debug("MainFS clearCache OK");
        }
    }
}