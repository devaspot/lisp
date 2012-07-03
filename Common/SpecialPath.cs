using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Front {

	/// <summary>
	/// Provides information about predefined paths.
	/// </summary>
	public class SpecialPath {
		#region Fields

		protected static SpecialPath InnerSpecialPath = new SpecialPath();
		protected AssemblyVersionInfo InnerApplicationVersion = null;
		protected string InnerApplicationFile = null;
		protected string InnerApplicationPath = null;
		protected string InnerCurrentUserAllMachinesSettingsPath = null;
		protected string InnerAllUsersCurrentMachineSettingsPath = null;
		protected string InnerCurrentUserCurrentMachineSettingsPath = null;

		#endregion

		#region Methods

		[DllImport("kernel32.dll")]
		protected static extern int GetModuleFileName(IntPtr hModule, StringBuilder path, int size);

		protected SpecialPath() {
			StringBuilder pathApplicationExecutable = new StringBuilder(260);

			GetModuleFileName(IntPtr.Zero, pathApplicationExecutable, pathApplicationExecutable.Capacity);

			this.InnerApplicationFile = pathApplicationExecutable.ToString();
			this.InnerApplicationPath = Path.GetDirectoryName(this.InnerApplicationFile);

			this.InnerApplicationVersion = AssemblyVersionInfo.GetVersionInfo(this.InnerApplicationFile, 3);
			string format;

			if (this.InnerApplicationVersion.CompanyName == null) {
				format = "{1}";
			} else {
				if (this.InnerApplicationVersion.ProductName == null) {
					format = "{1}{0}{2}";
				} else {
					if (this.InnerApplicationVersion.ProductVersion == null) {
						format = "{1}{0}{2}{0}{3}";
					} else {
						format = "{1}{0}{2}{0}{3}{0}{4}";
					}
				}
			}


			this.InnerCurrentUserAllMachinesSettingsPath = string.Format(format,
				Path.DirectorySeparatorChar,
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
				this.InnerApplicationVersion.CompanyName,
				this.InnerApplicationVersion.ProductName,
				this.InnerApplicationVersion.ProductVersion);
			this.InnerAllUsersCurrentMachineSettingsPath = string.Format(format,
				Path.DirectorySeparatorChar,
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
				this.InnerApplicationVersion.CompanyName,
				this.InnerApplicationVersion.ProductName,
				this.InnerApplicationVersion.ProductVersion);
			this.InnerCurrentUserCurrentMachineSettingsPath = string.Format(format,
				Path.DirectorySeparatorChar,
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
				this.InnerApplicationVersion.CompanyName,
				this.InnerApplicationVersion.ProductName,
				this.InnerApplicationVersion.ProductVersion);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Application version of currently executing executable.
		/// </summary>
		public static AssemblyVersionInfo ApplicationVersion {
			[System.Diagnostics.DebuggerStepThrough]
			get { return InnerSpecialPath.InnerApplicationVersion; }
		}

		/// <summary>
		/// Path of file of currently executing executable.
		/// </summary>
		public static string ApplicationFile {
			[System.Diagnostics.DebuggerStepThrough]
			get { return InnerSpecialPath.InnerApplicationFile; }
		}

		/// <summary>
		/// Path of directory of currently executing executable.
		/// </summary>
		public static string ApplicationPath {
			[System.Diagnostics.DebuggerStepThrough]
			get { return InnerSpecialPath.InnerApplicationPath; }
		}

		/// <summary>
		/// Path folder where roaming settings for current user must be placed.
		/// </summary>
		public static string CurrentUserAllMachinesSettingsPath {
			[System.Diagnostics.DebuggerStepThrough]
			get { return InnerSpecialPath.InnerCurrentUserAllMachinesSettingsPath; }
		}

		/// <summary>
		/// Path folder where non-roaming settings for all users must be placed.
		/// </summary>
		public static string AllUsersCurrentMachineSettingsPath {
			[System.Diagnostics.DebuggerStepThrough]
			get { return InnerSpecialPath.InnerAllUsersCurrentMachineSettingsPath; }
		}

		/// <summary>
		/// Path folder where non-roaming settings for current user must be placed.
		/// </summary>
		public static string CurrentUserCurrentMachineSettingsPath {
			[System.Diagnostics.DebuggerStepThrough]
			get { return InnerSpecialPath.InnerCurrentUserCurrentMachineSettingsPath; }
		}

		#endregion
	}
}