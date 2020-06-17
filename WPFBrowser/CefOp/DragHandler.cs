using System;
using System.Collections.Generic;
using CefSharp;
using CefSharp.Enums;

namespace Browser.CefOp
{
	/// <summary>
	/// (たぶん)ドラッグ&ドロップを無効化します。
	/// </summary>
	public class DragHandler : IDragHandler
	{

		bool IDragHandler.OnDragEnter(IWebBrowser chromiumWebBrowser, IBrowser browser, IDragData dragData, DragOperationsMask mask)
		{
			return true;
		}

		void IDragHandler.OnDraggableRegionsChanged(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IList<DraggableRegion> regions)
		{
			throw new NotImplementedException();
		}
	}
}
