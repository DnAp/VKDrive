
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using VKDrive.VKAPI;

namespace VKDrive.Files
{
    /*
    public class SMp3 : Download, IDisposable
    {
        public int UID = 0;
        public int AID = 0;
        public short Duration = 0;

        private Stream stream;

        public object Work { get; private set; }

        int download(byte[] buffer, int offset, int count){
            HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(Url);
            WebReq.Timeout = Properties.Settings.Default.Timeout * 1000;
            WebReq.AddRange(offset, offset+count);

            int downloaded = 0;

            using (WebResponse result = WebReq.GetResponse())
            {
                using (Stream stream = result.GetResponseStream())
                {
                    byte[] bufferWritter = new byte[1024];
                    int read;
                    while (true)
                    {
                        read = stream.Read(bufferWritter, 0, bufferWritter.Length);
                        if (read == 0)
                            break;

                        Buffer.BlockCopy(bufferWritter, 0, buffer, downloaded, read);
                        downloaded += read;
                    }
                }
            }

            return downloaded;
        }
        //public SMp3(string name) : base(name) { }

        public static void getFileSize(object osmp3)
        {
            SMp3 smp3 = (SMp3)osmp3;
            try
            {
                lock (smp3)
                {
                    if (smp3.stream == null)
                    {
                        HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(smp3.Url);
                        WebReq.Timeout = Properties.Settings.Default.Timeout * 1000;
                        WebReq.Method = "HEAD";
                        using (WebResponse result = WebReq.GetResponse()) {
                            smp3.Length = result.ContentLength;
                            smp3.stream = new SwapStream(new SwapLib(1, 1), Convert.ToInt32(smp3.Length), smp3.download);
                        }
                    }
                }
            }
            catch (Exception)
            {
                
            }
            
        }

        public SMp3(SerializationObject.AudioWithAlbum curAudio) : base("")
        {
            FileName = SMp3.EscapeFileName(curAudio.Artist + " - " + curAudio.Title) + ".mp3";
            Url = curAudio.Url;
            UID = curAudio.OwnerId;
            AID = curAudio.AId;

            Thread newThread = new Thread(SMp3.getFileSize);
            newThread.Start(this);

        }

        public override int ReadFile(
            byte[] buffer,
            ref uint readBytes,
            long offset,
            Dokan.DokanFileInfo info)
        {
            while(stream == null)
            {
                Thread.Sleep(100);
            }
            int res = stream.Read(buffer, Convert.ToInt32(offset), buffer.Length);
            readBytes += Convert.ToUInt32(res);
            return DokanNet.DOKAN_SUCCESS;
        }

        /*public override string getUniqueId()
        {
            return "aid" + UID +"_"+ AID;
        }* /

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
                JArray apiResult = (JArray)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("audio.getById", param));
                Url = apiResult[0].ToObject<SerializationObject.AudioWithAlbum>().Url;
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
            FileName = SMp3.EscapeFileName(attr.Element("artist").Value + " - " + attr.Element("title").Value) + ".mp3";
            Url = (string)attr.Element("url");
            UID = Convert.ToInt32(attr.Element("owner_id").Value);
            AID = Convert.ToInt32(attr.Element("aid").Value);
        }

        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing && stream!=null)
            {
                stream.Dispose();
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        ~SMp3()
        {
            Dispose(false);
        }
    }*/
}
