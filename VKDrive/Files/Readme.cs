using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive.Files
{
    class PlainText : VFile
    {
        private byte[] text;

        public static string InternetShortcut(string url)
        {
            return "[InternetShortcut]\nURL=" + url;
        }

        public PlainText(string name) : base(name) {
            LastWriteTime = DateTime.Now;
            CreationTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
            text = new byte[0];
        }

        public PlainText(string name, string textString)
            : base(name)
        {
            LastWriteTime = DateTime.Now;
            CreationTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
            SetText(textString);
        }

        public void SetText(string textString)
        {
            textString = textString.Replace("\n", "\r\n");
            text = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(1251), Encoding.UTF8.GetBytes(textString));
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

        /// <summary>
        /// Подпись
        /// </summary>
        /// <returns></returns>
        public static string getSubscript(){
            return "\n\nСоздано при помощи VKDrive\nhttp://vkdrive.dnap.su/\nmailto: vkdrive@dnap.su";
        }
    }
}
