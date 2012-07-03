using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Front {

	public class ShellUtil {
		[DllImport("shell32.dll")]
		private static extern int ShellExecuteW(
			[In] IntPtr hWnd,
			[In][MarshalAs(UnmanagedType.LPWStr)] string operation,
			[In][MarshalAs(UnmanagedType.LPWStr)] string file,
			[In][MarshalAs(UnmanagedType.LPWStr)] string parameters,
			[In][MarshalAs(UnmanagedType.LPWStr)] string directory,
			[In] int showCommand);

		[DllImport("shell32.dll")]
		private static extern int ShellExecuteA(
			[In] IntPtr hWnd,
			[In][MarshalAs(UnmanagedType.LPStr)] string operation,
			[In][MarshalAs(UnmanagedType.LPStr)] string file,
			[In][MarshalAs(UnmanagedType.LPStr)] string parameters,
			[In][MarshalAs(UnmanagedType.LPStr)] string directory,
			[In] int showCommand);

		private static void ExecutePath(string path, string verb) {
			if (System.Environment.OSVersion.Platform == PlatformID.Win32NT) {
				ShellExecuteW(IntPtr.Zero, verb, path, null, null, 1 /* SW_SHOWNORMAL */);
			} else {
				ShellExecuteA(IntPtr.Zero, verb, path, null, null, 1 /* SW_SHOWNORMAL */);
			}
		}

		private static bool ExecuteURL(string protocol, string path, string verb) {
			using (RegistryKey keyProtocol = Registry.ClassesRoot.OpenSubKey(protocol)) {
				if (keyProtocol != null) {
					string valueProtocolDefinition = keyProtocol.GetValue(null) as string;

					if ((valueProtocolDefinition != null) && (valueProtocolDefinition.StartsWith("url", StringComparison.CurrentCultureIgnoreCase))) {
						using (RegistryKey keyCommand = keyProtocol.OpenSubKey(string.Format("shell\\{0}\\command", verb))) {
							if (keyCommand != null) {
								string executablePath = keyCommand.GetValue(null) as string;

								if ((executablePath.IndexOf("%1") == -1) && executablePath.EndsWith("iexplore.exe\" -nohome")) {
									Process.Start(
										executablePath.Substring(1, executablePath.LastIndexOf('\"') - 1),
										path);

									return true;
								}
							}
						}
					}
				}
			}

			return false;
		}

		public static void ExecuteVerb(string uri, string verb) {
			if (verb == "open") {
				Match matchProtocol = (new Regex("([A-Za-z]*)\\://(.+)")).Match(uri);

				if (matchProtocol.Groups.Count == 3) {
					string uriProtocol = matchProtocol.Groups[1].Value.ToLower();
					string uriPath = matchProtocol.Groups[2].Value;

					switch (uriProtocol) {
						case "http":
						case "https":
						case "ftp":
						case "ftps":
							if (!ExecuteURL(uriProtocol, uriPath, verb)) {
								ExecutePath(uri, verb);
							}
							break;
						case "file":
							ExecutePath(uriPath, verb);
							break;
						default:
							ExecutePath(uri, verb);
							break;
					}
				} else {
					ExecutePath(uri, verb);
				}
			} else {
				ExecutePath(uri, verb);
			}
		}

		public static void ExecuteOpen(string url) {
			ExecuteVerb(url, "open");
		}

		public static void ExecutePrint(string url) {
			ExecuteVerb(url, "print");
		}

		public static void ExecuteEdit(string url) {
			ExecuteVerb(url, "edit");
		}
	}
}