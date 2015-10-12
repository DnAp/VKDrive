using System;
using System.IO;
using System.Text;

namespace VKDrive.Files
{
    class SettingsXls : VFile
    {
        private byte[] text;

        public SettingsXls(string fileName, string name, string actionName, string errorMessage, string skipMessage)
            : base(fileName)
        {
            FileName = "VKDirvePathData.xml";

            HiddenFile = true;
            LastWriteTime = DateTime.Now;
            CreationTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
            //Length = (new FileInfo(SettingsLnk)).Length;
            text = new byte[0];

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<root><type>1</type><name>" + name + "</name>" +
                    "<actionName>" + actionName + "</actionName>" +
                    "<errorMessage>" + errorMessage + "</errorMessage>" +
                    "<skipMessage>" + skipMessage + "</skipMessage></root>";

            text = Encoding.UTF8.GetBytes(xml);
            Length = text.Length;

        }

        public override int ReadFile(byte[] buffer, ref uint readBytes, long offset, Dokan.DokanFileInfo info)
        {
            if (offset >= text.Length)
            {
                readBytes = 0;
                return Dokan.DokanNet.DOKAN_SUCCESS;
            }

            readBytes = Convert.ToUInt32(offset + buffer.Length > text.Length ? text.Length - offset : buffer.Length);
            // Тут ограничение нужно по длинне
            //text.CopyTo(buffer, offset);
            Array.Copy(text, offset, buffer, 0, readBytes);
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }
    }
}
