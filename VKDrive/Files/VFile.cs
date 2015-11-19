using Dokan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VKDrive.Files
{
    public abstract class VFile : FileInformation
    {
        protected bool HiddenFile = false;

        public bool IsHiddenFile
        {
            get { return this.HiddenFile; }
        }

        public VFile(string name)
        {
            Attributes = FileAttributes.ReadOnly;
            FileName = clearName(name);

            LastAccessTime = DateTime.Now;
            LastWriteTime = DateTime.Now;
            CreationTime = DateTime.Now;
            
            
        }

        public string toString()
        {
            return base.ToString() + " " + FileName;
        }



        public static string clearName(string name)
        {
            name = Regex.Replace(name, "[\\/?:*\" ><|]+", " ").Trim(" .".ToCharArray());
            name = Regex.Replace(name, @"[!]+", "!");
            if (name.Length > 40)
            {
                int dotPos = name.LastIndexOf('.');
                if (dotPos > -1)
                    name = name.Substring(0, 40) + name.Substring(dotPos);
                else
                    name = name.Substring(0, 40);

            }
            else if (name.Length == 0)
                name = "_";
            
            return name;
        }

        public void CopyTo(FileInformation finfo)
        {
            finfo.Attributes = Attributes;
            finfo.CreationTime = CreationTime;
            finfo.FileName = FileName;
            finfo.LastAccessTime = LastAccessTime;
            finfo.LastWriteTime = LastWriteTime;
            finfo.Length = Length;
        }

        abstract public int ReadFile(
            byte[] buffer,
            ref uint readBytes,
            long offset,
            DokanFileInfo info);

    }
}
