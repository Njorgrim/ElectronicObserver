﻿//using ElectronicObserver.Window;
using ElectronicObserver.WPFEO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using ElectronicObserver.WinFormsEO;

namespace ElectronicObserver
{
	static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{

			bool allowMultiInstance = args.Contains("-m") || args.Contains("--multi-instance");


			using (var mutex = new Mutex(false, Application.ExecutablePath.Replace('\\', '/'), out var created))
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

				if (!created && !allowMultiInstance)
				{
					// 多重起動禁止
					MessageBox.Show("Electronic Observer already started.\r\nIn case of false positive, start using option -m via commandline.", Utility.SoftwareInformation.SoftwareNameEnglish, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				new Utility.DynamicTranslator();
				
				try
				{
					// todo why does this exception happen?
					// observed first after I added the wpf version of KC progress
					//Application.Run(new FormMain());
					WPFMain mainWindow = new WPFMain();
					mainWindow.ShowDialog();
				}
				catch (System.Runtime.InteropServices.SEHException e)
				{

				}
			}
		}
	}
}
