using Dokan;

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using log4net;
using VKDrive.Files;

namespace VKDrive.Dris
{
    public abstract class Dir
    {
        public Dictionary<string, object> Directory;
        public Folder RootNode;
		private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Dir()
        {
            Directory = new Dictionary<string, object>();
            _LoadRootNode();
        }

        public void ClearCache()
        {
            Directory = new Dictionary<string, object>();
            RootNode = new Folder("");
            _LoadRootNode();
        }

        abstract public void _LoadRootNode();

        abstract public bool _LoadFile(Folder file);

        protected void LoadFile(Folder file)
        {
            lock (file)
            {
                if (!file.IsLoaded)
                {
                    if (file.Loader != null)
                    {
                        foreach(VFile f in file.Loader.Load() )
                        {
                            file.ChildsAdd(f);
                        }
                        
                        file.IsLoaded = true;
                    }
                    else if (_LoadFile(file))
                    {
                        file.IsLoaded = true;
                    }
                    
                }
            }
        }

        protected VFile FindFiles(string pathString)
        {
            pathString = pathString.Trim('\\').Replace("\\\\", "\\");
            if (pathString == "")
            {
                if (RootNode == null || !RootNode.IsLoaded)
                {
                    LoadFile(RootNode);
                }
                return RootNode;
            }
            string[] path = pathString.Split('\\');

            VFile currentNode = RootNode;

            foreach (string dirName in path)
            {
                try
                {
                    if (currentNode.GetType() != typeof(Folder))
                    {
                        return null;
                    }

                    if (!((Folder)currentNode).IsLoaded)
                    {
                        //И так работает все медленно, не будем ставить lock если это можно
                        lock (currentNode)
                        {
                            if (!((Folder)currentNode).IsLoaded)
                            {
                                LoadFile((Folder)currentNode);
                            }
                        }
                    }


                    //currentNode = currentNode.ChildNodes.First(value => value.Name == dirName);
                    currentNode = ((Folder)currentNode).FindInChilds(dirName);
                    if (currentNode == null)
                    {
						_log.Debug("Не нашел: " + pathString);
                        return null;
                    }
                }
                catch (Exception)
                {
					_log.Debug("Не нашел(exception): " + pathString);
                    return null;
                }
            }
            if (currentNode.GetType() == typeof(Folder) && !((Folder)currentNode).IsLoaded)
            {
                LoadFile((Folder)currentNode);
            }
            return currentNode;
        }

        public int FindFiles(
            string filename,
            System.Collections.ArrayList files,
            DokanFileInfo info)
        {
            var file = FindFiles(filename);
            if (file == null)
                return DokanNet.DOKAN_SUCCESS;

            if (file.GetType() != typeof(Folder))
            {
                files.Add(file);
                return DokanNet.DOKAN_SUCCESS;
            }

            if (!((Folder)file).IsLoaded)
            {
                LoadFile((Folder)file);
            }

            foreach (var currentNode in ((Folder)file).Childs)
            {
                if(!currentNode.IsHiddenFile)
                    files.Add(currentNode);
            }

            return DokanNet.DOKAN_SUCCESS;
        }

        /// <summary>
        /// Более крутой метод, давайте будем использовать его
        /// </summary>
        /// <param name="file"></param>
        /// <param name="name"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public virtual int CreateDirectory(Folder file, string name, DokanFileInfo info)
        {
            return DokanNet.DOKAN_ERROR;
        }

        public virtual int CreateDirectory(string filename, DokanFileInfo info)
        {
            var pathList = filename.Split('\\');
            var newDirName = pathList[pathList.Length - 1];
            filename = string.Join("\\", pathList, 0, pathList.Length - 1);
            var file = FindFiles(filename);
            
            if (file != null && file.GetType() == typeof(Folder)) // хз у null есть GetType или нет
            {
                return CreateDirectory((Folder)file, newDirName, info);
            }

            return DokanNet.DOKAN_ERROR;
        }

        public virtual int DeleteDirectory(string filename, DokanFileInfo info)
        {
            return DokanNet.DOKAN_ERROR;
        }

        public virtual int GetFileInformation(
           string filename,
           FileInformation fileinfo,
           DokanFileInfo info)
        {
            var file = FindFiles(filename);
            if (file == null)
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            if (file.Length == 0 && file is Download)
            {
                // Это нужно чтоб TC и другие наивные приложения читали корректное кол-во 
                var webReq = (HttpWebRequest)WebRequest.Create(((Download)file).Url);
                webReq.Timeout = Properties.Settings.Default.Timeout * 1000;
                webReq.Method = "HEAD";
                var result = webReq.GetResponse();
                file.Length = result.ContentLength;
                result.Close();
            }

            file.CopyTo(fileinfo);
            return DokanNet.DOKAN_SUCCESS;
        }
        
        public virtual int ReadFile(
            string filename,
            byte[] buffer,
            ref uint readBytes,
            long offset,
            DokanFileInfo info)
        {
            var file = FindFiles(filename);
            if (file == null)
            {
                return -DokanNet.ERROR_FILE_NOT_FOUND;
            }
            return file.ReadFile(buffer, ref readBytes, offset, info);
        }

        public virtual int MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            return DokanNet.ERROR_ACCESS_DENIED;
        }


    }

}
