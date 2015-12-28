using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vbAccelerator.Components.Shell;

namespace VKDrive.Files
{
    /// <summary>
    /// Этот класс нужно юзать вместе с SettingsXls
    /// </summary>
    class Settings : VFile
    {
        static string _settingsLnk = null;
        static protected Object SettingsLnkLock = new Object();
        static ushort _settingsCount = 0;
        
        public Settings(string name)
            : base(name)
        {
            _settingsCount++;
            if (_settingsLnk == null)
            {
                _settingsLnk = Path.GetTempFileName();
                lock (SettingsLnkLock)
                {
                    using (ShellLink shortcut = new ShellLink())
                    {
                        shortcut.Target = Directory.GetCurrentDirectory() + "\\Resurces\\VKDriveSettings.exe";
                        shortcut.WorkingDirectory = "";
                        shortcut.Description = "Настройки VKDrive";
                        shortcut.DisplayMode = ShellLink.LinkDisplayMode.EdmNormal;
                        shortcut.Save(_settingsLnk);
                    }
                }
            }

            LastWriteTime = DateTime.Now;
            CreationTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
            Length = (new FileInfo(_settingsLnk)).Length;
        }
        ~Settings()
        {
            _settingsCount--;
            if (_settingsCount == 0)
            {
                File.Delete(_settingsLnk);
                _settingsLnk = null;
            }
        }

        public override int ReadFile(byte[] buffer, ref uint readBytes, long offset, Dokan.DokanFileInfo info)
        {
            if (offset >= Length)
            {
                readBytes = 0;
                return Dokan.DokanNet.DOKAN_SUCCESS;
            }
            lock (SettingsLnkLock)
            {
                FileStream stream = new FileStream(_settingsLnk, FileMode.Open);

                readBytes = Convert.ToUInt32(stream.Read(buffer, Convert.ToInt32(offset), buffer.Length));
                stream.Close();
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }
    }
}
