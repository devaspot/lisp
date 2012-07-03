using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Front.SysInterop.Printing
{
	public class PrintingNativeMethods
	{
		#region Error Codes

		public const int HPR_OK = 0;
		public const int HPR_INVALID_HANDLE = 1;
		public const int HPR_INVALID_FORMAT = 2;
		public const int HPR_FILE_NOT_FOUND = 3;
		public const int HPR_INVALID_PARAMETER = 4;
		public const int HPR_INVALID_STATE = 5;

		#endregion

		#region Types

		public delegate bool HtmlPrintLoadDataHandler(
			[In] IntPtr hPrint,
			[In] IntPtr tag,
			[In][MarshalAs(UnmanagedType.LPStr)] string resourceUri);

		public delegate void HtmlPrintHyperlinkAreaHandler(
			[In] IntPtr hPrint,
			[In] IntPtr tag,
			[In][Out] ref Rectangle area,
			[In][MarshalAs(UnmanagedType.LPStr)] string linkUri);

		#endregion

		#region Printing

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintCreateInstance")]
		public static extern IntPtr Print_CreateInstance();

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintDestroyInstance")]
		public static extern void Print_DestroyInstance(
			[In] IntPtr hPrint);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintSetTag")]
		public static extern int Print_SetTag(
			[In] IntPtr hPrint,
			[In] IntPtr tag);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintGetTag")]
		public static extern int Print_GetTag(
			[In] IntPtr hPrint,
			[Out] out IntPtr tag);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintLoadHtmlFromFile")]
		public static extern int Print_LoadHtml(
			[In] IntPtr hPrint,
			[In][MarshalAs(UnmanagedType.LPStr)] string uri);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintLoadHtmlFromMemory")]
		public static extern int Print_LoadHtml(
			[In] IntPtr hPrint,
			[In][MarshalAs(UnmanagedType.LPStr)] string baseUri,
			[In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] data,
			[In] int dataSize);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintMeasure")]
		public static extern int Print_Measure(
			[In] IntPtr hPrint,
			[In] IntPtr hDC,
			[In] int widthInPixels,
			[In] int widthInUnits,
			[In] int heightInUnits,
			[Out] out int pageNumber);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintRender")]
		public static extern int Print_Draw(
			[In] IntPtr hPrint,
			[In] IntPtr hDC,
			[In] int offsetLeft,
			[In] int offsetTop,
			[In] int pageIndex);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintSetDataReady")]
		public static extern int Print_ResourceReady(
			[In] IntPtr hPrint,
			[In][Out][MarshalAs(UnmanagedType.LPStr)] string resourceUri,
			[In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] data,
			[In] int dataSize);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintGetDocumentMinWidth")]
		public static extern int Print_GetMinWidth(
			[In] IntPtr hPrint,
			[Out] out int width);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintGetDocumentHeight")]
		public static extern int Print_GetMinHeight(
			[In] IntPtr hPrint,
			[Out] out int height);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintSetMediaType")]
		public static extern int Print_SetMediaType(
			[In] IntPtr hPrint,
			[In][MarshalAs(UnmanagedType.LPStr)] string mediatype);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintSetLoadDataCallback")]
		public static extern int Print_SetResourceLoadCallback(
			[In] IntPtr hPrint,
			[In] HtmlPrintLoadDataHandler callback);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintSetHyperlinkAreaCallback")]
		public static extern int Print_SetHyperlinkAreaCallback(
			[In] IntPtr hPrint,
			[In] HtmlPrintHyperlinkAreaHandler callback);

		[DllImport("HTMLayout.dll", EntryPoint = "HTMPrintGetRootElement")]
		public static extern int Print_GetRootElement(
			[In] IntPtr hPrint,
			[Out] out IntPtr hElement);

		#endregion
	}
}
