using System;
using System.IO;
using System.Text;
using DokanNet;

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

        public override NtStatus ReadFile(byte[] buffer, ref int readingBytes, long offset, DokanFileInfo info)
        {
            if (readingBytes < 0) throw new ArgumentOutOfRangeException(nameof(readingBytes));
            if (offset >= _text.Length)
            {
                readingBytes = 0;
                return DokanResult.Success;
            }

            readingBytes =
                Convert.ToInt32(offset + buffer.Length > _text.Length ? _text.Length - offset : buffer.Length);
            // Тут ограничение нужно по длинне
            //text.CopyTo(buffer, offset);
            Array.Copy(_text, offset, buffer, 0, readingBytes);
            return DokanResult.Success;
        }
    }
}