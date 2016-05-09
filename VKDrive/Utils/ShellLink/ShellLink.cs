using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace VKDrive.Utils.ShellLink
{
	#region ShellLink Object
	/// <summary>
	/// Summary description for ShellLink.
	/// </summary>
	public sealed class ShellLink : IDisposable
	{
		#region ComInterop for IShellLink

		#region IPersist Interface
		[ComImport()]
		[Guid("0000010C-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IPersist
		{
			[PreserveSig]
				//[helpstring("Returns the class identifier for the component object")]
			void GetClassID(out Guid pClassId);
		}
		#endregion

		#region IPersistFile Interface
		[ComImport()]
		[Guid("0000010B-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IPersistFile
		{
			// can't get this to go if I extend IPersist, so put it here:
			[PreserveSig]
			void GetClassID(out Guid pClassId);

			//[helpstring("Checks for changes since last file write")]		
			void IsDirty();

			//[helpstring("Opens the specified file and initializes the object from its contents")]		
			void Load(
				[MarshalAs(UnmanagedType.LPWStr)] string pszFileName, 
				uint dwMode);

			//[helpstring("Saves the object into the specified file")]		
			void Save(
				[MarshalAs(UnmanagedType.LPWStr)] string pszFileName, 
				[MarshalAs(UnmanagedType.Bool)] bool fRemember);

			//[helpstring("Notifies the object that save is completed")]		
			void SaveCompleted(
				[MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

			//[helpstring("Gets the current name of the file associated with the object")]		
			void GetCurFile(
				[MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
		}
		#endregion

		#region IShellLink Interface
		[ComImport()]
		[Guid("000214EE-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IShellLinkA
		{
			//[helpstring("Retrieves the path and filename of a shell link object")]
			void GetPath(
				[Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszFile, 
				int cchMaxPath, 
				ref Win32FindDataa pfd, 
				uint fFlags);

			//[helpstring("Retrieves the list of shell link item identifiers")]
			void GetIDList(out IntPtr ppidl);

			//[helpstring("Sets the list of shell link item identifiers")]
			void SetIDList(IntPtr pidl);

			//[helpstring("Retrieves the shell link description string")]
			void GetDescription(
				[Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszFile,
				int cchMaxName);
		
			//[helpstring("Sets the shell link description string")]
			void SetDescription(
				[MarshalAs(UnmanagedType.LPStr)] string pszName);

			//[helpstring("Retrieves the name of the shell link working directory")]
			void GetWorkingDirectory(
				[Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszDir,
				int cchMaxPath);

			//[helpstring("Sets the name of the shell link working directory")]
			void SetWorkingDirectory(
				[MarshalAs(UnmanagedType.LPStr)] string pszDir);

			//[helpstring("Retrieves the shell link command-line arguments")]
			void GetArguments(
				[Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszArgs, 
				int cchMaxPath);

			//[helpstring("Sets the shell link command-line arguments")]
			void SetArguments(
				[MarshalAs(UnmanagedType.LPStr)] string pszArgs);

			//[propget, helpstring("Retrieves or sets the shell link hot key")]
			void GetHotkey(out short pwHotkey);
			//[propput, helpstring("Retrieves or sets the shell link hot key")]
			void SetHotkey(short pwHotkey);

			//[propget, helpstring("Retrieves or sets the shell link show command")]
			void GetShowCmd(out uint piShowCmd);
			//[propput, helpstring("Retrieves or sets the shell link show command")]
			void SetShowCmd(uint piShowCmd);

			//[helpstring("Retrieves the location (path and index) of the shell link icon")]
			void GetIconLocation(
				[Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszIconPath, 
				int cchIconPath, 
				out int piIcon);
		
			//[helpstring("Sets the location (path and index) of the shell link icon")]
			void SetIconLocation(
				[MarshalAs(UnmanagedType.LPStr)] string pszIconPath, 
				int iIcon);

			//[helpstring("Sets the shell link relative path")]
			void SetRelativePath(
				[MarshalAs(UnmanagedType.LPStr)] string pszPathRel, 
				uint dwReserved);

			//[helpstring("Resolves a shell link. The system searches for the shell link object and updates the shell link path and its list of identifiers (if necessary)")]
			void Resolve(
				IntPtr hWnd, 
				uint fFlags);

			//[helpstring("Sets the shell link path and filename")]
			void SetPath(
				[MarshalAs(UnmanagedType.LPStr)] string pszFile);
		}


		[ComImport()]
		[Guid("000214F9-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IShellLinkW
		{
			//[helpstring("Retrieves the path and filename of a shell link object")]
			void GetPath(
				[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, 
				int cchMaxPath, 
				ref Win32FindDataw pfd, 
				uint fFlags);

			//[helpstring("Retrieves the list of shell link item identifiers")]
			void GetIDList(out IntPtr ppidl);

			//[helpstring("Sets the list of shell link item identifiers")]
			void SetIDList(IntPtr pidl);

			//[helpstring("Retrieves the shell link description string")]
			void GetDescription(
				[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
				int cchMaxName);
		
			//[helpstring("Sets the shell link description string")]
			void SetDescription(
				[MarshalAs(UnmanagedType.LPWStr)] string pszName);

			//[helpstring("Retrieves the name of the shell link working directory")]
			void GetWorkingDirectory(
				[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
				int cchMaxPath);

			//[helpstring("Sets the name of the shell link working directory")]
			void SetWorkingDirectory(
				[MarshalAs(UnmanagedType.LPWStr)] string pszDir);

			//[helpstring("Retrieves the shell link command-line arguments")]
			void GetArguments(
				[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, 
				int cchMaxPath);

			//[helpstring("Sets the shell link command-line arguments")]
			void SetArguments(
				[MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

			//[propget, helpstring("Retrieves or sets the shell link hot key")]
			void GetHotkey(out short pwHotkey);
			//[propput, helpstring("Retrieves or sets the shell link hot key")]
			void SetHotkey(short pwHotkey);

			//[propget, helpstring("Retrieves or sets the shell link show command")]
			void GetShowCmd(out uint piShowCmd);
			//[propput, helpstring("Retrieves or sets the shell link show command")]
			void SetShowCmd(uint piShowCmd);

			//[helpstring("Retrieves the location (path and index) of the shell link icon")]
			void GetIconLocation(
				[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, 
				int cchIconPath, 
				out int piIcon);
		
			//[helpstring("Sets the location (path and index) of the shell link icon")]
			void SetIconLocation(
				[MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, 
				int iIcon);

			//[helpstring("Sets the shell link relative path")]
			void SetRelativePath(
				[MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, 
				uint dwReserved);

			//[helpstring("Resolves a shell link. The system searches for the shell link object and updates the shell link path and its list of identifiers (if necessary)")]
			void Resolve(
				IntPtr hWnd, 
				uint fFlags);

			//[helpstring("Sets the shell link path and filename")]
			void SetPath(
				[MarshalAs(UnmanagedType.LPWStr)] string pszFile);
		}
		#endregion

		#region ShellLinkCoClass
		[Guid("00021401-0000-0000-C000-000000000046")]
		[ClassInterface(ClassInterfaceType.None)]
		[ComImport()]
		private class CShellLink{}

		#endregion
	
		#region Private IShellLink enumerations
		private enum EShellLinkGp : uint
		{
			SlgpShortpath = 1,
			SlgpUncpriority = 2
		}

		[Flags]
		private enum EShowWindowFlags : uint
		{
			SwHide = 0,
			SwShownormal = 1,
			SwNormal = 1,
			SwShowminimized = 2,
			SwShowmaximized = 3,
			SwMaximize = 3,
			SwShownoactivate = 4,
			SwShow = 5,
			SwMinimize = 6,
			SwShowminnoactive = 7,
			SwShowna = 8,
			SwRestore = 9,
			SwShowdefault = 10,
			SwMax = 10
		}
		#endregion

		#region IShellLink Private structs

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=0, CharSet=CharSet.Unicode)]
		private struct Win32FindDataw
		{
			public uint dwFileAttributes;
			public Filetime ftCreationTime;
			public Filetime ftLastAccessTime;
			public Filetime ftLastWriteTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
			public uint dwReserved0;
			public uint dwReserved1;
			[MarshalAs(UnmanagedType.ByValTStr , SizeConst = 260)] // MAX_PATH
			public string cFileName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string cAlternateFileName;
		}

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=0, CharSet=CharSet.Ansi)]
		private struct Win32FindDataa
		{
			public uint dwFileAttributes;
			public Filetime ftCreationTime;
			public Filetime ftLastAccessTime;
			public Filetime ftLastWriteTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
			public uint dwReserved0;
			public uint dwReserved1;
			[MarshalAs(UnmanagedType.ByValTStr , SizeConst = 260)] // MAX_PATH
			public string cFileName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string cAlternateFileName;
		}

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=0)]
		private struct Filetime 
		{
			public uint dwLowDateTime;
			public uint dwHighDateTime;
		}  
		#endregion	

		#region UnManaged Methods
		private class UnManagedMethods
		{
			[DllImport("Shell32", CharSet=CharSet.Auto)]
			internal extern static int ExtractIconEx (
				[MarshalAs(UnmanagedType.LPTStr)] 
				string lpszFile,
				int nIconIndex,
				IntPtr[] phIconLarge, 
				IntPtr[] phIconSmall,
				int nIcons);

			[DllImport("user32")]
			internal static extern int DestroyIcon(IntPtr hIcon);
		}
		#endregion

		#endregion

		#region Enumerations
		/// <summary>
		/// Flags determining how the links with missing
		/// targets are resolved.
		/// </summary>
		[Flags]
		public enum EShellLinkResolveFlags : uint
		{
			/// <summary>
			/// Allow any match during resolution.  Has no effect
			/// on ME/2000 or above, use the other flags instead.
			/// </summary>
			SlrAnyMatch = 0x2,
			/// <summary>
			/// Call the Microsoft Windows Installer. 
			/// </summary>
			SlrInvokeMsi = 0x80,
			/// <summary>
			/// Disable distributed link tracking. By default, 
			/// distributed link tracking tracks removable media 
			/// across multiple devices based on the volume name. 
			/// It also uses the UNC path to track remote file 
			/// systems whose drive letter has changed. Setting 
			/// SLR_NOLINKINFO disables both types of tracking.
			/// </summary>
			SlrNolinkinfo = 0x40,
			/// <summary>
			/// Do not display a dialog box if the link cannot be resolved. 
			/// When SLR_NO_UI is set, a time-out value that specifies the 
			/// maximum amount of time to be spent resolving the link can 
			/// be specified in milliseconds. The function returns if the 
			/// link cannot be resolved within the time-out duration. 
			/// If the timeout is not set, the time-out duration will be 
			/// set to the default value of 3,000 milliseconds (3 seconds). 
			/// </summary>										    
			SlrNoUi = 0x1,
			/// <summary>
			/// Not documented in SDK.  Assume same as SLR_NO_UI but 
			/// intended for applications without a hWnd.
			/// </summary>
			SlrNoUiWithMsgPump = 0x101,
			/// <summary>
			/// Do not update the link information. 
			/// </summary>
			SlrNoupdate = 0x8,
			/// <summary>
			/// Do not execute the search heuristics. 
			/// </summary>																																																																																																																																																																																																														
			SlrNosearch = 0x10,
			/// <summary>
			/// Do not use distributed link tracking. 
			/// </summary>
			SlrNotrack = 0x20,
			/// <summary>
			/// If the link object has changed, update its path and list 
			/// of identifiers. If SLR_UPDATE is set, you do not need to 
			/// call IPersistFile::IsDirty to determine whether or not 
			/// the link object has changed. 
			/// </summary>
			SlrUpdate  = 0x4
		}

		public enum LinkDisplayMode : uint
		{
			EdmNormal = EShowWindowFlags.SwNormal,
			EdmMinimized = EShowWindowFlags.SwShowminnoactive,
			EdmMaximized = EShowWindowFlags.SwMaximize
		}
		#endregion

		#region Member Variables
		// Use Unicode (W) under NT, otherwise use ANSI		
		private IShellLinkW _linkW;
		private IShellLinkA _linkA;
		private string _shortcutFile = "";
		#endregion

		#region Constructor
		/// <summary>
		/// Creates an instance of the Shell Link object.
		/// </summary>
		public ShellLink()
		{
			if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				_linkW = (IShellLinkW)new CShellLink();
			}
			else
			{
				_linkA = (IShellLinkA)new CShellLink();
			}
		}

		/// <summary>
		/// Creates an instance of a Shell Link object
		/// from the specified link file
		/// </summary>
		/// <param name="linkFile">The Shortcut file to open</param>
		public ShellLink(string linkFile) : this()
		{
			Open(linkFile);
		}
		#endregion

		#region Destructor and Dispose
		/// <summary>
		/// Call dispose just in case it hasn't happened yet
		/// </summary>
		~ShellLink()
		{
			Dispose();
		}

		/// <summary>
		/// Dispose the object, releasing the COM ShellLink object
		/// </summary>
		public void Dispose()
		{
			if (_linkW != null ) 
			{
				Marshal.ReleaseComObject(_linkW);
				_linkW = null;
			}
			if (_linkA != null)
			{
				Marshal.ReleaseComObject(_linkA);
				_linkA = null;
			}
		}
		#endregion

		#region Implementation
		public string ShortCutFile
		{
			get
			{
				return this._shortcutFile;
			}
			set
			{
				this._shortcutFile = value;
			}
		}

		/// <summary>
		/// Gets a System.Drawing.Icon containing the icon for this
		/// ShellLink object.
		/// </summary>
		public Icon LargeIcon
		{
			get
			{
				return GetIcon(true);
			}
		}

		public Icon SmallIcon
		{
			get
			{
				return GetIcon(false);
			}
		}

		private Icon GetIcon(bool large)
		{
			// Get icon index and path:
			int iconIndex = 0;
			StringBuilder iconPath = new StringBuilder(260, 260);
			if (_linkA == null)
			{
				_linkW.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
			}
			else
			{
				_linkA.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
			}
			string iconFile = iconPath.ToString();

			// If there are no details set for the icon, then we must use
			// the shell to get the icon for the target:
			if (iconFile.Length == 0)
			{
				// Use the FileIcon object to get the icon:
				FileIcon.ShGetFileInfoConstants flags = FileIcon.ShGetFileInfoConstants.ShgfiIcon |
					FileIcon.ShGetFileInfoConstants.ShgfiAttributes;
				if (large)
				{
					flags = flags | FileIcon.ShGetFileInfoConstants.ShgfiLargeicon;
				}
				else
				{
					flags = flags | FileIcon.ShGetFileInfoConstants.ShgfiSmallicon;
				}
				FileIcon fileIcon = new FileIcon(Target, flags);
				return fileIcon.ShellIcon;
			}
			else
			{
				// Use ExtractIconEx to get the icon:
				IntPtr[] hIconEx = new IntPtr[1] {IntPtr.Zero};			
				int iconCount = 0;
				if (large)
				{
					iconCount = UnManagedMethods.ExtractIconEx(
						iconFile,
						iconIndex,
						hIconEx,
						null,
						1);
				}
				else
				{
					iconCount = UnManagedMethods.ExtractIconEx(
						iconFile,
						iconIndex,
						null,
						hIconEx,
						1);
				}
				// If success then return as a GDI+ object
				Icon icon = null;
				if (hIconEx[0] != IntPtr.Zero)
				{
					icon = Icon.FromHandle(hIconEx[0]);
					//UnManagedMethods.DestroyIcon(hIconEx[0]);
				}
				return icon;
			}				
		}

		/// <summary>
		/// Gets the path to the file containing the icon for this shortcut.
		/// </summary>
		public string IconPath
		{
			get
			{
				StringBuilder iconPath = new StringBuilder(260, 260);
				int iconIndex = 0;
				if (_linkA == null)
				{
					_linkW.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
				}
				else
				{
					_linkA.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
				}
				return iconPath.ToString();
			}
			set
			{
				StringBuilder iconPath = new StringBuilder(260, 260);
				int iconIndex = 0;
				if (_linkA == null)
				{
					_linkW.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
				}
				else
				{
					_linkA.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
				}
				if (_linkA == null)
				{
					_linkW.SetIconLocation(value, iconIndex);
				}
				else
				{
					_linkA.SetIconLocation(value, iconIndex);
				}
			}
		}

		/// <summary>
		/// Gets the index of this icon within the icon path's resources
		/// </summary>
		public int IconIndex
		{
			get
			{
				StringBuilder iconPath = new StringBuilder(260, 260);
				int iconIndex = 0;
				if (_linkA == null)
				{
					_linkW.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
				}
				else
				{
					_linkA.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
				}
				return iconIndex;
			}
			set
			{
				StringBuilder iconPath = new StringBuilder(260, 260);
				int iconIndex = 0;
				if (_linkA == null)
				{
					_linkW.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
				}
				else
				{
					_linkA.GetIconLocation(iconPath, iconPath.Capacity, out iconIndex);
				}
				if (_linkA == null)
				{
					_linkW.SetIconLocation(iconPath.ToString(), value);
				}
				else
				{
					_linkA.SetIconLocation(iconPath.ToString(), value);
				}
			}
		}

		/// <summary>
		/// Gets/sets the fully qualified path to the link's target
		/// </summary>
		public string Target
		{
			get
			{		
				StringBuilder target = new StringBuilder(260, 260);
				if (_linkA == null)
				{
					Win32FindDataw fd = new Win32FindDataw();
					_linkW.GetPath(target, target.Capacity, ref fd, (uint)EShellLinkGp.SlgpUncpriority);
				}
				else
				{
					Win32FindDataa fd = new Win32FindDataa();
					_linkA.GetPath(target, target.Capacity, ref fd, (uint)EShellLinkGp.SlgpUncpriority);
				}
				return target.ToString();
			}
			set
			{
				if (_linkA == null)
				{
					_linkW.SetPath(value);
				}
				else
				{
					_linkA.SetPath(value);
				}
			}
		}

		/// <summary>
		/// Gets/sets the Working Directory for the Link
		/// </summary>
		public string WorkingDirectory
		{
			get
			{
				StringBuilder path = new StringBuilder(260, 260);
				if (_linkA == null)
				{
					_linkW.GetWorkingDirectory(path, path.Capacity);
				}
				else
				{
					_linkA.GetWorkingDirectory(path, path.Capacity);
				}
				return path.ToString();
			}
			set
			{
				if (_linkA == null)
				{
					_linkW.SetWorkingDirectory(value);	
				}
				else
				{
					_linkA.SetWorkingDirectory(value);
				}
			}
		}

		/// <summary>
		/// Gets/sets the description of the link
		/// </summary>
		public string Description
		{
			get
			{
				StringBuilder description = new StringBuilder(1024, 1024);
				if (_linkA == null)
				{
					_linkW.GetDescription(description, description.Capacity);
				}
				else
				{
					_linkA.GetDescription(description, description.Capacity);
				}
				return description.ToString();
			}
			set
			{
				if (_linkA == null)
				{
					_linkW.SetDescription(value);
				}
				else
				{
					_linkA.SetDescription(value);
				}
			}
		}

		/// <summary>
		/// Gets/sets any command line arguments associated with the link
		/// </summary>
		public string Arguments
		{
			get
			{				
				StringBuilder arguments = new StringBuilder(260, 260);
				if (_linkA == null)
				{
					_linkW.GetArguments(arguments, arguments.Capacity);
				}
				else
				{
					_linkA.GetArguments(arguments, arguments.Capacity);
				}
				return arguments.ToString();
			}
			set
			{
				if (_linkA == null)
				{
					_linkW.SetArguments(value);
				}
				else
				{
					_linkA.SetArguments(value);
				}
			}
		}

		/// <summary>
		/// Gets/sets the initial display mode when the shortcut is
		/// run
		/// </summary>
		public LinkDisplayMode DisplayMode
		{
			get
			{
				uint cmd = 0;
				if (_linkA == null)
				{
					_linkW.GetShowCmd(out cmd);
				}
				else
				{
					_linkA.GetShowCmd(out cmd);
				}
				return (LinkDisplayMode)cmd;
			}
			set
			{
				if (_linkA == null)
				{
					_linkW.SetShowCmd((uint)value);
				}
				else
				{
					_linkA.SetShowCmd((uint)value);
				}
			}
		}

		/// <summary>
		/// Gets/sets the HotKey to start the shortcut (if any)
		/// </summary>
		public Keys HotKey
		{
			get
			{
				short key = 0;
				if (_linkA == null)
				{
					_linkW.GetHotkey(out key);
				}
				else
				{
					_linkA.GetHotkey(out key);
				}
				return (Keys)key;
			}
			set
			{
				if (_linkA == null)
				{
					_linkW.SetHotkey((short)value);
				}
				else
				{
					_linkA.SetHotkey((short)value);
				}
			}
		}

		/// <summary>
		/// Saves the shortcut to ShortCutFile.
		/// </summary>
		public void Save()
		{
			Save(_shortcutFile);
		}

		/// <summary>
		/// Saves the shortcut to the specified file
		/// </summary>
		/// <param name="linkFile">The shortcut file (.lnk)</param>
		public void Save(
			string linkFile
			)
		{   
			// Save the object to disk
			if (_linkA == null)
			{
				((IPersistFile)_linkW).Save(linkFile, true);
				_shortcutFile = linkFile;
			}
			else
			{
				((IPersistFile)_linkA).Save(linkFile, true);
                _shortcutFile = linkFile;
			}
		}

		/// <summary>
		/// Loads a shortcut from the specified file
		/// </summary>
		/// <param name="linkFile">The shortcut file (.lnk) to load</param>
		public void Open(
			string linkFile			
			)
		{
			Open(linkFile, 
				IntPtr.Zero, 
				(EShellLinkResolveFlags.SlrAnyMatch | EShellLinkResolveFlags.SlrNoUi),
				1);
		}
		
		/// <summary>
		/// Loads a shortcut from the specified file, and allows flags controlling
		/// the UI behaviour if the shortcut's target isn't found to be set.
		/// </summary>
		/// <param name="linkFile">The shortcut file (.lnk) to load</param>
		/// <param name="hWnd">The window handle of the application's UI, if any</param>
		/// <param name="resolveFlags">Flags controlling resolution behaviour</param>
		public void Open(
			string linkFile, 
			IntPtr hWnd, 
			EShellLinkResolveFlags resolveFlags
			)
		{
			Open(linkFile, 
				hWnd, 
				resolveFlags, 
				1);
		}

		/// <summary>
		/// Loads a shortcut from the specified file, and allows flags controlling
		/// the UI behaviour if the shortcut's target isn't found to be set.  If
		/// no SLR_NO_UI is specified, you can also specify a timeout.
		/// </summary>
		/// <param name="linkFile">The shortcut file (.lnk) to load</param>
		/// <param name="hWnd">The window handle of the application's UI, if any</param>
		/// <param name="resolveFlags">Flags controlling resolution behaviour</param>
		/// <param name="timeOut">Timeout if SLR_NO_UI is specified, in ms.</param>
		public void Open(
			string linkFile,
			IntPtr hWnd, 
			EShellLinkResolveFlags resolveFlags,
			ushort timeOut
			)
		{
			uint flags;

			if ((resolveFlags & EShellLinkResolveFlags.SlrNoUi) 
				== EShellLinkResolveFlags.SlrNoUi)
			{
				flags = (uint)((int)resolveFlags | (timeOut << 16));
			}
			else
			{
				flags = (uint)resolveFlags;
			}

			if (_linkA == null)
			{
				((IPersistFile)_linkW).Load(linkFile, 0); //STGM_DIRECT)
				_linkW.Resolve(hWnd, flags);
				this._shortcutFile = linkFile;
			}
			else
			{
				((IPersistFile)_linkA).Load(linkFile, 0); //STGM_DIRECT)
				_linkA.Resolve(hWnd, flags);
				this._shortcutFile = linkFile;
			}
		}
		#endregion
	}
	#endregion

}
