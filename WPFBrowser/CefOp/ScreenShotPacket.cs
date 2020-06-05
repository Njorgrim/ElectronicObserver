﻿using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace WPFBrowser.CefOp
{
	public class ScreenShotPacket
	{
		public string ID { get; }
		public string DataUrl;
		public TaskCompletionSource<ScreenShotPacket> TaskSource { get; }

		public ScreenShotPacket() : this("ss_" + Guid.NewGuid().ToString("N")) { }
		public ScreenShotPacket(string id)
		{
			ID = id;
			TaskSource = new TaskCompletionSource<ScreenShotPacket>();
		}

		public void Complete(string dataurl)
		{
			DataUrl = dataurl;
			TaskSource.SetResult(this);
		}

		public Bitmap GetImage() => ConvertToImage(DataUrl);


		public static Bitmap ConvertToImage(string dataurl)
		{
			if (dataurl == null || !dataurl.StartsWith("data:image/png"))
				return null;

			var s = dataurl.Substring(dataurl.IndexOf(',') + 1);
			var bytes = Convert.FromBase64String(s);

			Bitmap bitmap;
			using (var ms = new MemoryStream(bytes))
				bitmap = new Bitmap(ms);

			return bitmap;
		}
	}
}