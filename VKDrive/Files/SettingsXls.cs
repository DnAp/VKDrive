using System;
using System.IO;
using System.Text;

namespace VKDrive.Files
{
    class SettingsXls : VFile
    {
        private byte[] _text;

        public SettingsXls(string fileName, string name, string actionName, string errorMessage, string skipMessage)
            : base(fileName)
        {
            FileName = "VKDirvePathData.xml";

            HiddenFile = true;
            LastWriteTime = DateTime.Now;
            CreationTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
            //Length = (new FileInfo(SettingsLnk)).Length;
            _text = new byte[0];

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<root><type>1</type><name>" + name + "</name>" +
                    "<actionName>" + actionName + "</actionName>" +
                    "<errorMessage>" + errorMessage + "</errorMessage>" +
                    "<skipMessage>" + skipMessage + "</skipMessage></root>";

            _text = Encoding.UTF8.GetBytes(xml);
            Length = _text.Length;

        }

        public override int ReadFile(byte[] buffer, ref uint readBytes, long offset, Dokan.DokanFileInfo info)
        {
            if (offset >= _text.Length)
            {
                readBytes = 0;
                return Dokan.DokanNet.DOKAN_SUCCESS;
            }

            readBytes = Convert.ToUInt32(offset + buffer.Length > _text.Length ? _text.Length - offset : buffer.Length);
            // Тут ограничение нужно по длинне
            //text.CopyTo(buffer, offset);
            Array.Copy(_text, offset, buffer, 0, readBytes);
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }
    }
}
