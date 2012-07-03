// $Id: Resources.cs 1615 2006-07-14 11:36:25Z kostya $

using System;
using System.Threading;
using System.Resources;

namespace Front {

	// TODO DF0014: подумать над корректностью работы с ресурсами 
	public sealed class RM {
		static object	syncRoot;
		static RM		loader;

		ResourceManager	rm;
		
		internal RM() {
			rm = new ResourceManager("Front.Messages", typeof(RM).Assembly);
		}

		static RM GetLoader() {
			if (loader == null)	lock(InternalSyncRoot) {
				if (loader == null)
					loader = new RM();
			}
			return loader;
		}

		static object InternalSyncRoot {
			get {
				if (syncRoot == null)
					Interlocked.CompareExchange(ref syncRoot, new object(), (object)null);
				return syncRoot;
			}
		}

		public static string GetString(string name) {
			return "";
			RM ldr = GetLoader();
			return ldr.rm.GetString(name, System.Threading.Thread.CurrentThread.CurrentUICulture);
		}

		public static string GetString(string name, params object[] args) {
			RM ldr = GetLoader();
			string fmt = ldr.rm.GetString(name, System.Threading.Thread.CurrentThread.CurrentUICulture);
			if (fmt == null) return String.Empty;
			if (args == null || args.Length == 0)
				return fmt;
			return String.Format(fmt, args);
		}

		public static object GetObject(string name) {
			RM ldr = GetLoader();
			return ldr.rm.GetObject(name);
		}
	}
}
