using BrowserLibCore;
using MagicOnion.Server.Hubs;
using System.Threading.Tasks;
using System;
using ElectronicObserver.WPFEO;

namespace BrowserHost
{
	/*public class BrowserHostHub : StreamingHubBase<IBrowserHost, IBrowser>, IBrowserHost
	{
		private IGroup Browsers { get; set; }
		public IBrowser Browser => Broadcast(Browsers);

		public Task<BrowserConfiguration> Configuration()
		{
			return Task.Run(() => WPFBrowserHost.Instance.ConfigurationCore);
		} 

		public async Task ConnectToBrowser(long handle)
		{
			Browsers = await Group.AddAsync("browser");
			await Task.Run(() => WPFBrowserHost.Instance.Connect(this));
			WPFBrowserHost.Instance.ConnectToBrowser((IntPtr)handle);
		}

		public async Task SendErrorReport(string exceptionName, string message)
		{
			await Task.Run(() => WPFBrowserHost.Instance.SendErrorReport(exceptionName, message));
		}

		public async Task AddLog(int priority, string message)
		{
			await Task.Run(() => WPFBrowserHost.Instance.AddLog(priority, message));
		}

		public async Task ConfigurationUpdated(BrowserConfiguration configuration)
		{
			await Task.Run(() => WPFBrowserHost.Instance.ConfigurationUpdated(configuration));
		}

		public Task<string> GetDownstreamProxy()
		{
			return Task.Run(() => WPFBrowserHost.Instance.GetDownstreamProxy());
		}

		public async Task SetProxyCompleted()
		{
			await Task.Run(() => WPFBrowserHost.Instance.SetProxyCompleted());
		}

		public async Task RequestNavigation(string v)
		{
			await Task.Run(() => WPFBrowserHost.Instance.RequestNavigation(v));
		}

		public async Task ClearCache()
		{
			await Task.Run(() => WPFBrowserHost.Instance.ClearCache());
		}

		public Task<bool> IsServerAlive()
		{
			return Task.Run(() => true);
		}
	}*/
}