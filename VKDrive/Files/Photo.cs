using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DokanNet;
using VKDrive.VKAPI;

namespace VKDrive.Files
{
    class Photo : Download
    {
        public int Pid = 0;
        public short Duration = 0;

        public Photo(string name) : base(name) { }

        public Photo(SerializationObject.Photo photo) : base("")
        {
            LoadByObject(photo);
        }

        private void LoadByObject(SerializationObject.Photo photo)
        {
            string name = photo.Text.Replace("<br>", " ");
            if (name.Length == 0)
            {
                name = photo.Id.ToString();
            }

            this.FileName = ClearName(name.Trim() + ".jpg", false);
            Pid = photo.Id;
            Url = photo.GetSrc();
            DateTime unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            CreationTime = unixTimeStamp.AddSeconds(photo.Created);
            LastWriteTime = CreationTime;
        }

        public override NtStatus ReadFile(byte[] buffer, ref int readBytes, long offset, DokanFileInfo info)
        {
            return DownloadManager.Instance.GetBlock(this, buffer, ref readBytes, offset);
        }

        /*public override string getUniqueId()
        {
            return "pid"+PID;
        }*/

        public override int[] GetUniqueId()
        {
            return new int[] { 0, Pid };
        }

        public override bool Update()
        {
            if (Pid == 0)
                return false;
            try
            {
                Dictionary<string, string> param = new Dictionary<string, string>() { { "photos", Pid.ToString() } };
                SerializationObject.Photo photo = VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("photos.getById", param)).ToObject<SerializationObject.Photo>();
                LoadByObject(photo);
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
