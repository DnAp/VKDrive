using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VKDrive.Files
{
    public class Mp3 : Download
    {
        public int UID = 0;
        public int AID = 0;
        public short Duration = 0;

        public Mp3(string name) : base(name) { }

        public Mp3(string name, XElement curAudio) : base(name) {
            LoadByXml(curAudio);
        }
        

        public override int ReadFile(
            byte[] buffer,
            ref uint readBytes,
            long offset,
            Dokan.DokanFileInfo info)
        {
            int res = DownloadManager.Instance.getBlock(this, buffer, ref readBytes, offset);
            return res;
        }

        /*public override string getUniqueId()
        {
            return "aid" + UID +"_"+ AID;
        }*/

        public override int[] getUniqueId()
        {
            return new int[] { UID, AID };
        }

        public override bool update()
        {
            if (AID == 0)
                return false;
            try
            {
                Dictionary<string, string> param = new Dictionary<string, string>() { { "audios", AID.ToString() } };
                string xml = VKAPI.Instance.execute("audio.getById", param);
                XElement responce = XElement.Parse(xml);
                Url = responce.Element("url").Value;
                CreationTime = DateTime.Now;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return true;
        }

        public void LoadByXml(XElement attr)
        {
            FileName = Mp3.EscapeFileName(attr.Element("artist").Value + " - " + attr.Element("title").Value) + ".mp3";
            Url = (string)attr.Element("url");
            UID = Convert.ToInt32(attr.Element("owner_id").Value);
            AID = Convert.ToInt32(attr.Element("aid").Value);
        }

    }
}
