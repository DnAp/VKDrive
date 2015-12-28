using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using VKDrive.VKAPI;

namespace VKDrive.Files
{
    public class Mp3 : Download
    {
        public int Uid = 0;
        public int Aid = 0;
        public short Duration = 0;

        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Mp3(SerializationObject.Audio curAudio) : base("")
        {
            FileName = Mp3.EscapeFileName(curAudio.Artist + " - " + curAudio.Title) + ".mp3";
            Url = curAudio.Url;
            Uid = curAudio.OwnerId;
            Aid = curAudio.Id;
            _log.Debug("Make mp3 " + FileName + " : "+Url);
        }

        public override int ReadFile(
            byte[] buffer,
            ref uint readBytes,
            long offset,
            Dokan.DokanFileInfo info)
        {
            int res = DownloadManager.Instance.GetBlock(this, buffer, ref readBytes, offset);
            return res;
        }

        /*public override string getUniqueId()
        {
            return "aid" + UID +"_"+ AID;
        }*/

        public override int[] GetUniqueId()
        {
            return new int[] { Uid, Aid };
        }

        public override bool Update()
        {
            if (Aid == 0)
                return false;
            try
            {
                Dictionary<string, string> param = new Dictionary<string, string>() { { "audios", Aid.ToString() } };
                JArray apiResult = (JArray)VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("audio.getById", param));
                Url = apiResult[0].ToObject<SerializationObject.Audio>().Url;
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
            Uid = Convert.ToInt32(attr.Element("owner_id").Value);
            Aid = Convert.ToInt32(attr.Element("aid").Value);
        }

    }
}
