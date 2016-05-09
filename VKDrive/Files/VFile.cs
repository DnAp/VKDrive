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

	    protected VFile(string name)
        {
            Attributes = FileAttributes.ReadOnly;
			
            FileName = ClearName(name, GetType() == typeof(Folder));

            LastAccessTime = DateTime.Now;
            LastWriteTime = DateTime.Now;
            CreationTime = DateTime.Now;
            
            
        }

        public override string ToString()
        {
            return base.ToString() + " " + FileName;
        }



        public static string ClearName(string name, bool isFolder)
        {
			name = Regex.Replace(name, "[^a-zA-Z0-9а-яА-Я .,+()!@#$%^&*_№;?=[\\]~'-]", " ").Trim(" .".ToCharArray());
			/* disallow symwols
			name = Regex.Replace(name, "[\\/?:*\" ><|\n]+", " ").Trim(" .".ToCharArray());
            */
			name = Regex.Replace(name, @"[!]+", "!");

			if (name.Length > 40)
            {
	            if (!isFolder)
	            {
		            var dotPos = name.LastIndexOf('.');
		            if (dotPos > -1 && dotPos > name.Length - 4)
			            name = name.Substring(0, 40) + name.Substring(dotPos);
		            else
			            name = name.Substring(0, 40);
	            }
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
