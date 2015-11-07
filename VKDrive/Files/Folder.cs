using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VKDrive.Utils;

namespace VKDrive.Files
{
    public class Folder : VFile
    {
        public BlockingList<VFile> Childs;

        private bool _IsLoader = false;
        public bool IsLoaded
        {
            set
            {
                if (_IsLoader == false && value == true)
                {
                    _IsLoader = value;
                    if (Childs.Count > 0)
                    {
                        VFile loaderFile = Childs.First();
                        if (loaderFile.FileName == "Идет загрузка.txt")
                        {
                            Childs.Remove(loaderFile);
                        }
                    }
                    GroupFile();
                }
            }
            get
            {
                return _IsLoader;
            }
        }

        /// <summary>
        /// Дополнительная информация
        /// </summary>
        public Dictionary<string, string> Property = new Dictionary<string,string>();
        public Folder(string name) : base(name)
        {
            Attributes = System.IO.FileAttributes.Directory;
            Childs = new BlockingList<VFile>() { new PlainText("Идет загрузка.txt") };
        }

        public new string toString()
        {
            return base.ToString() + " " + Property.Select(s => s.ToString());
        }

        public VFile findInChilds(string name)
        {
            foreach(VFile curFile in Childs) {
                if (curFile.FileName == name)
                    return curFile;
            }
            return null;
        }

        public void ChildsAdd(VFile file)
        {
            string name = file.FileName;
            VFile cp = this.findInChilds(name);
            
            if (cp != null)
            {
                int i = 0;
                while (cp != null)
                {
                    i++;
                    // Думал вот сейчас напишешь одну строчку, а фот фиг тебе, експешен кидает на кривые пути(
                    // name = Path.GetFileNameWithoutExtension(file.FileName) + "(" + i.ToString() + ")" + Path.GetExtension(file.FileName);
                    int dotPos = file.FileName.LastIndexOf('.');
                    if (dotPos > -1)
                    {
                        name = file.FileName.Substring(0, dotPos) + "(" + i.ToString() + ")" + file.FileName.Substring(dotPos);
                    }
                    else
                    {
                        name = file.FileName + "(" + i.ToString() + ")";
                    }

                    cp = this.findInChilds(name);
                }
                file.FileName = name;
            }
            
            Childs.Add(file);
        }

        private void GroupFile()
        {
            if (Childs.Count > 500)
            {
                var maxFile = 500;
                /*if (Childs.First().GetType() == typeof(Folder))
                {
                    maxFile = 100;
                }
                else if (Childs.Count < maxFile)
                {
                    return;
                }*/

                BlockingList<VFile> replaceChilds = new BlockingList<VFile>();
                List<VFile> copy = Childs.Select(item => item).ToList(); // clone list
                copy.Sort(delegate(VFile a, VFile b) { return a.FileName.CompareTo(b.FileName); });
                Folder fNode;
                for (int i = 0; i <= (copy.Count / maxFile); i++)
                {
                    int residue = copy.Count - (i * maxFile); // остаток
                    residue = residue < maxFile ? residue : maxFile;
                    IList<VFile> tmp = copy.GetRange(i * maxFile, residue);
                    fNode = new Folder(tmp[0].FileName.Substring(0, 1) + ".." + tmp[residue - 1].FileName.Substring(0, 1));

                    foreach (VFile curFile in tmp)
                    {
                        fNode.Childs.Add(curFile);
                    }
                    fNode.IsLoaded = true;
                    replaceChilds.Add(fNode);
                }
                Childs = replaceChilds;
            }
        }

        public override int ReadFile(
            byte[] buffer,
            ref uint readBytes,
            long offset,
            Dokan.DokanFileInfo info)
        {
            // wat?
            return Dokan.DokanNet.ERROR_ACCESS_DENIED;
        }
    }
}
