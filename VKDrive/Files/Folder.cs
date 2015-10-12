using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive.Files
{
    public class Folder : VFile
    {
        private bool _IsLoader = false;
        public bool IsLoaded
        {
            set
            {
                _IsLoader = value;
                if (value == true)
                {
                    if (Childs.Count > 0 && Childs[0].FileName == "Идет загрузка.txt")
                    {
                        Childs.RemoveAt(0);
                    }
                    GroupFile();
                }
            }
            get
            {
                return _IsLoader;
            }
        }

        public List<VFile> Childs;
        /// <summary>
        /// Дополнительная информация
        /// </summary>
        public Dictionary<string, string> Property = new Dictionary<string,string>();
        public Folder(string name) : base(name)
        {
            Attributes = System.IO.FileAttributes.Directory;
            Childs = new List<VFile>() { new PlainText("Идет загрузка.txt") };
        }

        public void ChildsAdd(VFile file)
        {
            VFile cp;
            string name = file.FileName;
            cp = Childs.Find(delegate(VFile curFileNode)
            {
                return curFileNode.FileName == name;
            });
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

                    cp = Childs.Find(delegate(VFile curFileNode)
                    {
                        return curFileNode.FileName == name;
                    });
                }
                file.FileName = name;
            }
            
            Childs.Add(file);
        }

        private void GroupFile()
        {
            if (Childs.Count > 100)
            {
                var maxFile = 500;
                if (Childs[0].GetType() == typeof(Folder))
                {
                    maxFile = 100;
                }
                else if (Childs.Count < maxFile)
                {
                    return;
                }
                
                List<VFile> replace = new List<VFile>();
                List<VFile> copy = Childs;
                copy.Sort(delegate(VFile a, VFile b) { return a.FileName.CompareTo(b.FileName); });
                Folder fNode;
                for (int i = 0; i < (copy.Count / maxFile); i++)
                {
                    int residue = copy.Count - (i * maxFile); // остаток
                    residue = residue < maxFile ? residue : maxFile;
                    List<VFile> tmp = copy.GetRange(i * maxFile, residue);
                    fNode = new Folder(tmp[0].FileName[0].ToString() + ".." + tmp[residue - 1].FileName[0].ToString());
                    fNode.Childs.AddRange(tmp);
                    fNode.IsLoaded = true;
                    replace.Add(fNode);
                }
                Childs.Clear();
                Childs = replace;
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
