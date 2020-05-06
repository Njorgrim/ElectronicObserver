using ElectronicObserver.WPFEO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;

namespace ElectronicObserver
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			using (var mutex = new Mutex(false, Application.ResourceAssembly.Location.Replace('\\', '/'), out var created))
			{
				/*
				bool hasHandle = false;

				try
				{
					hasHandle = mutex.WaitOne(0, false);
				}
				catch (AbandonedMutexException)
				{
					hasHandle = true;
				}
				*/

				if (!created)
				{
					// 多重起動禁止
					MessageBox.Show("Electronic Observer already started.", Utility.SoftwareInformation.SoftwareNameEnglish, MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}

				//Application.EnableVisualStyles();
				//Application.SetCompatibleTextRenderingDefault(false);
				new Utility.DynamicTranslator();

				try
				{
					// todo why does this exception happen?
					// observed first after I added the wpf version of KC progress
					//Application.Run(new FormMain());
					WPFMain mainWindow = new WPFMain();
					mainWindow.ShowDialog();
				}
				catch (System.Runtime.InteropServices.SEHException ex)
				{

				}
			}
		}
	}
}
