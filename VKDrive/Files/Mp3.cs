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

        public Mp3(SerializationObject.Audio curAudioWithAlbum) : base("")
        {
            FileName = Mp3.EscapeFileName(curAudioWithAlbum.Artist + " - " + curAudioWithAlbum.Title) + ".mp3";
            Url = curAudioWithAlbum.Url;
            Uid = curAudioWithAlbum.OwnerId;
            Aid = curAudioWithAlbum.Id;
            _log.Debug("Make mp3 " + FileName + " : "+Url);
        }

        public override int ReadFile(
            byte[] buffer,
            ref uint readingBytes,
            long offset,
            Dokan.DokanFileInfo info)
        {
			return DownloadManager.Instance.GetBlock(this, buffer, ref readingBytes, offset);
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
		

    }
}
