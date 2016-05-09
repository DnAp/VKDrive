using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace VKDrive.Utils.ShellLink
{
	/// <summary>
	/// Enables extraction of icons for any file type from
	/// the Shell.
	/// </summary>
	public class FileIcon
	{

		#region UnmanagedCode
		private const int MaxPath = 260;
		
		[StructLayout(LayoutKind.Sequential)]
		private struct Shfileinfo
		{
			public IntPtr hIcon;
			public int iIcon;
			public int dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MaxPath)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=80)]
			public string szTypeName;
		}

		[DllImport("shell32")]
		private static extern int SHGetFileInfo (
			string pszPath, 
			int dwFileAttributes,
			ref Shfileinfo psfi, 
			uint cbFileInfo, 
			uint uFlags);

		[DllImport("user32.dll")]
		private static extern int DestroyIcon(IntPtr hIcon);

		private const int FormatMessageAllocateBuffer = 0x100; 
		private const int FormatMessageArgumentArray = 0x2000;
		private const int FormatMessageFromHmodule = 0x800;
		private const int FormatMessageFromString = 0x400;
		private const int FormatMessageFromSystem = 0x1000;
		private const int FormatMessageIgnoreInserts = 0x200;
		private const int FormatMessageMaxWidthMask = 0xFF;
        [DllImport("kernel32")]
		private extern static int FormatMessage (
			int dwFlags, 
			IntPtr lpSource, 
			int dwMessageId, 
			int dwLanguageId, 
			string lpBuffer,
			uint nSize, 
			int argumentsLong);

		[DllImport("kernel32")]
		private extern static int GetLastError();
		#endregion
		
		#region Member Variables
		private string _fileName;
		private string _displayName;
		private string _typeName;
		private ShGetFileInfoConstants _flags;
		private Icon _fileIcon;
		#endregion

		#region Enumerations
		[Flags]		
			public enum ShGetFileInfoConstants : int
		{
			ShgfiIcon = 0x100,                // get icon 
			ShgfiDisplayname = 0x200,         // get display name 
			ShgfiTypename = 0x400,            // get type name 
			ShgfiAttributes = 0x800,          // get attributes 
			ShgfiIconlocation = 0x1000,       // get icon location 
			ShgfiExetype = 0x2000,            // return exe type 
			ShgfiSysiconindex = 0x4000,       // get system icon index 
			ShgfiLinkoverlay = 0x8000,        // put a link overlay on icon 
			ShgfiSelected = 0x10000,          // show icon in selected state 
			ShgfiAttrSpecified = 0x20000,    // get only specified attributes 
			ShgfiLargeicon = 0x0,             // get large icon 
			ShgfiSmallicon = 0x1,             // get small icon 
			ShgfiOpenicon = 0x2,              // get open icon 
			ShgfiShelliconsize = 0x4,         // get shell size icon 
			//SHGFI_PIDL = 0x8,                  // pszPath is a pidl 
			ShgfiUsefileattributes = 0x10,     // use passed dwFileAttribute 
			ShgfiAddoverlays = 0x000000020,     // apply the appropriate overlays
			ShgfiOverlayindex = 0x000000040     // Get the index of the overlay
		}
		#endregion

		#region Implementation
		/// <summary>
		/// Gets/sets the flags used to extract the icon
		/// </summary>
		public FileIcon.ShGetFileInfoConstants Flags
		{
			get
			{
				return _flags;
			}
			set
			{
				_flags = value;
			}
		}

		/// <summary>
		/// Gets/sets the filename to get the icon for
		/// </summary>
		public string FileName
		{
			get
			{
				return _fileName;
			}
			set
			{
				_fileName = value;
			}
		}

		/// <summary>
		/// Gets the icon for the chosen file
		/// </summary>
		public Icon ShellIcon
		{
			get
			{
				return _fileIcon;
			}
		}

		/// <summary>
		/// Gets the display name for the selected file
		/// if the SHGFI_DISPLAYNAME flag was set.
		/// </summary>
		public string DisplayName
		{
			get
			{
				return _displayName;
			}
		}

		/// <summary>
		/// Gets the type name for the selected file
		/// if the SHGFI_TYPENAME flag was set.
		/// </summary>
		public string TypeName
		{
			get
			{
				return _typeName;
			}
		}

		/// <summary>
		///  Gets the information for the specified 
		///  file name and flags.
		/// </summary>
		public void GetInfo()
		{
			_fileIcon = null;
			_typeName = "";
			_displayName = "";

			Shfileinfo shfi = new Shfileinfo();
			uint shfiSize = (uint)Marshal.SizeOf(shfi.GetType());

			int ret = SHGetFileInfo(
				_fileName, 0, ref shfi, shfiSize, (uint)(_flags));
			if (ret != 0)
			{
				if (shfi.hIcon != IntPtr.Zero)
				{
					_fileIcon = System.Drawing.Icon.FromHandle(shfi.hIcon);
					// Now owned by the GDI+ object
					//DestroyIcon(shfi.hIcon);
				}
				_typeName = shfi.szTypeName;
				_displayName = shfi.szDisplayName;
			}
			else
			{
			
				int err = GetLastError();
				Console.WriteLine("Error {0}", err);
				string txtS = new string('\0', 256);
				int len = FormatMessage(
					FormatMessageFromSystem | FormatMessageIgnoreInserts,
					IntPtr.Zero, err, 0, txtS, 256, 0);
				Console.WriteLine("Len {0} text {1}", len, txtS);

				// throw exception

			}
		}

		/// <summary>
		/// Constructs a new, default instance of the FileIcon
		/// class.  Specify the filename and call GetInfo()
		/// to retrieve an icon.
		/// </summary>
		public FileIcon()
		{
			_flags = ShGetFileInfoConstants.ShgfiIcon | 
				ShGetFileInfoConstants.ShgfiDisplayname |
				ShGetFileInfoConstants.ShgfiTypename |
				ShGetFileInfoConstants.ShgfiAttributes |
				ShGetFileInfoConstants.ShgfiExetype;
		}
		/// <summary>
		/// Constructs a new instance of the FileIcon class
		/// and retrieves the icon, display name and type name
		/// for the specified file.		
		/// </summary>
		/// <param name="fileName">The filename to get the icon, 
		/// display name and type name for</param>
		public FileIcon(string fileName) : this()
		{
			this._fileName = fileName;
			GetInfo();
		}
		/// <summary>
		/// Constructs a new instance of the FileIcon class
		/// and retrieves the information specified in the 
		/// flags.
		/// </summary>
		/// <param name="fileName">The filename to get information
		/// for</param>
		/// <param name="flags">The flags to use when extracting the
		/// icon and other shell information.</param>
		public FileIcon(string fileName, FileIcon.ShGetFileInfoConstants flags)
		{
			this._fileName = fileName;
			this._flags = flags;
			GetInfo();
		}

		#endregion	
	}
}