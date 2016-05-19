using System;
using System.IO;
using VKDrive.Utils.ShellLink;

namespace VKDrive.Files
{
    /// <summary>
    /// Этот класс нужно юзать вместе с SettingsXls
    /// </summary>
    internal class Settings : VFile
    {
        static string _settingsLnk = null;
        protected static object SettingsLnkLock = new object();
        static ushort _settingsCount = 0;

		public Settings(string name) : this(name, "") { }

	    public Settings(string name, string arg)
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
					    shortcut.Arguments = arg;
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
	        if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(readBytes));
	        if (offset >= Length)
            {
                readBytes = 0;
                return Dokan.DokanNet.DOKAN_SUCCESS;
            }
            lock (SettingsLnkLock)
            {
                var stream = new FileStream(_settingsLnk, FileMode.Open);

                readBytes = Convert.ToUInt32(stream.Read(buffer, Convert.ToInt32(offset), buffer.Length));
                stream.Close();
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }
    }
}
