using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Browser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
	    private void App_OnStartup(object sender, StartupEventArgs e)
	    {
			if (e.Args.Length < 2)
		    {
			    MessageBox.Show("Please start the application using ElectronicObserver.exe",
				    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
			    return;
		    }

		    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		    if (!int.TryParse(e.Args[1], out int port))
		    {
			    MessageBox.Show("Please start the application using ElectronicObserver.exe",
				    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
			    return;
		    }

		    new MainWindow(e.Args[0], port).Show();
		}

	    private static Assembly? CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			if (!(args?.Name ?? "").StartsWith("CefSharp")) return null;

			string asmname = args.Name.Split(",".ToCharArray(), 2)[0] + ".dll";
			string arch = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, Environment.Is64BitProcess ? "x64" : "x86", asmname);

			if (!File.Exists(arch)) return null;

			try
			{
				return System.Reflection.Assembly.LoadFile(arch);
			}
			catch (IOException ex) when (ex is FileNotFoundException || ex is FileLoadException)
			{
				if (MessageBox.Show(
						$@"The browser component could not be loaded.
Microsoft Visual C++ 2015 Redistributable is required.
Open the download page?
(Please install vc_redist.{(Environment.Is64BitProcess ? "x64" : "x86")}.exe)",
						"CefSharp Load Error", MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.Yes)
					== MessageBoxResult.Yes)
				{
					ProcessStartInfo psi = new ProcessStartInfo
					{
						FileName = @"https://www.microsoft.com/en-US/download/details.aspx?id=53587",
						UseShellExecute = true
					};
					Process.Start(psi);
				}

				// なんにせよ今回は起動できないのであきらめる
				throw;
			}
			catch (NotSupportedException)
			{
				// 概ね ZoneID を外し忘れているのが原因

				if (MessageBox.Show(
						@"Browser startup failed.
This may be caused by the fact that the operation required for installation has not been performed.
Do you want to open the installation guide?",
						"Browser Load Failed", MessageBoxButton.YesNo, MessageBoxImage.Error)
					== MessageBoxResult.Yes)
				{
					ProcessStartInfo psi = new ProcessStartInfo
					{
						FileName = @"https://github.com/andanteyk/ElectronicObserver/wiki/Install",
						UseShellExecute = true
					};
					Process.Start(psi);
				}

				// なんにせよ今回は起動できないのであきらめる
				throw;
			}
		}
	}
}
