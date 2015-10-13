using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VKDrive.Files
{
    class Photo : Download
    {
        public int PID = 0;
        public short Duration = 0;

        public Photo(string name) : base(name) { }

        public void LoadByXml(XElement curPhoto)
        {
            string name = curPhoto.Element("text").Value.Replace("<br>", " ");
            if (name.Length == 0)
            {
                name = curPhoto.Element("pid").Value;
            }
            
            this.FileName = clearName(name.Trim() + ".jpg");
            PID = Convert.ToInt32(curPhoto.Element("pid").Value);
            XElement tmp = curPhoto.Element("src_xxbig");
            if (tmp == null)
            {
                Url = curPhoto.Element("src_big").Value;
            }
            else
            {
                Url = tmp.Value;
            }
            DateTime unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            CreationTime = unixTimeStamp.AddSeconds(Convert.ToInt32(curPhoto.Element("created").Value));
            tmp = curPhoto.Element("updated");
            if (tmp == null)
            {
                LastWriteTime = DateTime.Now;
            }
            else
            {
                LastWriteTime = unixTimeStamp.AddSeconds(Convert.ToInt32(tmp.Value));
            }
        }

        public override int ReadFile(
            byte[] buffer,
            ref uint readBytes,
            long offset,
            Dokan.DokanFileInfo info)
        {
            return DownloadManager.Instance.getBlock(this, buffer, ref readBytes, offset);
        }

        /*public override string getUniqueId()
        {
            return "pid"+PID;
        }*/

        public override int[] getUniqueId()
        {
            return new int[] { 0, PID };
        }

        public override bool update()
        {
            if (PID == 0)
                return false;
            try
            {
                Dictionary<string, string> param = new Dictionary<string, string>() { { "photos", PID.ToString() } };
                string xml = VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("photos.getById", param));
                XElement responce = XElement.Parse(xml).Element("photo");
                this.LoadByXml(responce);
                CreationTime = DateTime.Now;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return true;
        }
        
    }
}
