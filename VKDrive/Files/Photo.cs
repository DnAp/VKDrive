using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VKDrive.VKAPI;

namespace VKDrive.Files
{
    class Photo : Download
    {
        public int PID = 0;
        public short Duration = 0;

        public Photo(string name) : base(name) { }

        public Photo(SerializationObject.Photo photo) : base("")
        {
            loadByObject(photo);
        }

        private void loadByObject(SerializationObject.Photo photo)
        {
            string name = photo.Text.Replace("<br>", " ");
            if (name.Length == 0)
            {
                name = photo.Id.ToString();
            }

            this.FileName = clearName(name.Trim() + ".jpg");
            PID = photo.Id;
            Url = photo.GetSrc();
            DateTime unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            CreationTime = unixTimeStamp.AddSeconds(photo.Created);
            LastWriteTime = CreationTime;
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
                SerializationObject.Photo photo = VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("photos.getById", param)).ToObject<SerializationObject.Photo>();
                loadByObject(photo);
                LastWriteTime = DateTime.Now;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return true;
        }
        
    }
}
