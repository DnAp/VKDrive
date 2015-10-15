﻿using Dokan;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VKDrive.Files;

namespace VKDrive.Dris
{
    public abstract class Dir
    {
        public Dictionary<string, object> Directory = new Dictionary<string, object>();
        public Folder RootNode;

        public Dir()
        {
            _LoadRootNode();
        }

        public void clearCache()
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
                if (!file.IsLoaded && _LoadFile(file))
                    file.IsLoaded = true;
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
                    currentNode = ((Folder)currentNode).Childs.Find(
                        delegate(VFile curFileNode)
                        {
                            return curFileNode.FileName == dirName;
                        }
                    );
                    if (currentNode == null)
                    {
                        Console.WriteLine("Не нашел: " + pathString);
                        return null;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Не нашел(exception): " + pathString);
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
            VFile file = FindFiles(filename);
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

            foreach (VFile currentNode in ((Folder)file).Childs)
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
            string[] pathList = filename.Split('\\');
            string newDirName = pathList[pathList.Length - 1];
            filename = string.Join("\\", pathList, 0, pathList.Length - 1);
            VFile file = FindFiles(filename);
            
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
            VFile file = FindFiles(filename);
            if (file == null)
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            if (file.Length == 0 && file is Download)
            {
                // Это нужно чтоб TC и другие наивные приложения читали корректное кол-во 
                HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(((Download)file).Url);
                WebReq.Timeout = Properties.Settings.Default.Timeout * 1000;
                WebReq.Method = "HEAD";
                System.Net.WebResponse result = WebReq.GetResponse();
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
            VFile file = FindFiles(filename);
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