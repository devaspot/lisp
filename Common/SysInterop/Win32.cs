using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Front.SysInterop {
	public static class Win32 {

		[DllImport("user32", EntryPoint = "GetAsyncKeyState")]
		public static extern short GetAsyncKeyState([In] int vKey);
	}

	public enum AsynkKeyState {
		Shift =0x10,
		LShift = 0xA0,
		RShift = 0xA1,
		LControl = 0xA2,
		RControl = 0xA3,
		LMenu = 0xA4,
		RMenu = 0xA5
	}
}
