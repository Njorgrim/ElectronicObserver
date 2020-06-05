using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace WPFBrowser
{
	public static class ExtensionMethods
	{
		public static BitmapSource ToBitmapSource(this Bitmap bitmap)
		{
			using MemoryStream memory = new MemoryStream();

			bitmap.Save(memory, ImageFormat.Bmp);
			memory.Position = 0;
			BitmapImage bitmapimage = new BitmapImage();
			bitmapimage.BeginInit();
			bitmapimage.StreamSource = memory;
			bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapimage.EndInit();

			return bitmapimage;
		}
	}
}