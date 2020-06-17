using System;
using System.IO;

namespace BrowserLib
{
	public static class BrowserConstants
	{
#if DEBUG
		public static string CachePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserCache");
#else
		public static string CachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"ElectronicObserver\CEF");
#endif
	}
}