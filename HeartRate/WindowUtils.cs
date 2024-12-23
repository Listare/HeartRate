using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace HeartRate
{
	static class WindowUtils
	{
		[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
		private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

		private const uint WS_EX_TRANSPARENT = 0x20;
		private const int GWL_EXSTYLE = -20;

		public static void SetWindowTransparency(Window window, bool transparent)
		{
			IntPtr hWnd = new WindowInteropHelper(window).Handle;
			uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
			if (transparent)
			{
				exStyle |= WS_EX_TRANSPARENT;
			}
			else
			{
				exStyle &= ~WS_EX_TRANSPARENT;
			}
			SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);
		}
	}
}
