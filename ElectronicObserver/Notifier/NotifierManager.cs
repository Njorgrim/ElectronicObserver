using ElectronicObserver.WinFormsEO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ElectronicObserver.WPFEO;

namespace ElectronicObserver.Notifier
{

	public sealed class NotifierManager
	{

		#region Singleton

		private static readonly NotifierManager instance = new NotifierManager();

		public static NotifierManager Instance => instance;

		#endregion

		private object ParentForm { get; set; }


		public NotifierExpedition Expedition { get; private set; }
		public NotifierConstruction Construction { get; private set; }
		public NotifierRepair Repair { get; private set; }
		public NotifierCondition Condition { get; private set; }
		public NotifierDamage Damage { get; private set; }
		public NotifierAnchorageRepair AnchorageRepair { get; private set; }
		public NotifierBaseAirCorps BaseAirCorps { get; private set; }

		private NotifierManager()
		{
		}


		public void Initialize(object parent)
		{

			ParentForm = parent;

			var c = Utility.Configuration.Config;

			Expedition = new NotifierExpedition(c.NotifierExpedition);
			Construction = new NotifierConstruction(c.NotifierConstruction);
			Repair = new NotifierRepair(c.NotifierRepair);
			Condition = new NotifierCondition(c.NotifierCondition);
			Damage = new NotifierDamage(c.NotifierDamage);
			AnchorageRepair = new NotifierAnchorageRepair(c.NotifierAnchorageRepair);
			BaseAirCorps = new NotifierBaseAirCorps(c.NotifierBaseAirCorps);
		}

		public void ApplyToConfiguration()
		{

			var c = Utility.Configuration.Config;

			Expedition.ApplyToConfiguration(c.NotifierExpedition);
			Construction.ApplyToConfiguration(c.NotifierConstruction);
			Repair.ApplyToConfiguration(c.NotifierRepair);
			Condition.ApplyToConfiguration(c.NotifierCondition);
			Damage.ApplyToConfiguration(c.NotifierDamage);
			AnchorageRepair.ApplyToConfiguration(c.NotifierAnchorageRepair);
			BaseAirCorps.ApplyToConfiguration(c.NotifierBaseAirCorps);
		}

		public void ShowNotifier(ElectronicObserver.WinFormsEO.Dialog.DialogNotifier form)
		{

			if (form.DialogData.Alignment == NotifierDialogAlignment.CustomRelative)
			{       //cloneしているから書き換えても問題ないはず
				static Point ToWinformsPoint(System.Windows.Point p) =>
					new Point((int)p.X, (int)p.Y);

				Point p = ParentForm switch
				{
					FormMain parentForm => parentForm.fBrowser.PointToScreen(
						new Point(parentForm.fBrowser.ClientSize.Width / 2, parentForm.fBrowser.ClientSize.Height / 2)),

					WPFMain parentForm => ToWinformsPoint(parentForm.PointToScreen(
						new System.Windows.Point(parentForm.ucBrowser.RenderSize.Width / 2,
							parentForm.ucBrowser.RenderSize.Height / 2))),

					_ => new Point()
				}; p.Offset(new Point(-form.Width / 2, -form.Height / 2));
				p.Offset(form.DialogData.Location);

				form.DialogData.Location = p;
			}
			form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
			form.Show();
		}

		public IEnumerable<NotifierBase> GetNotifiers()
		{
			yield return Expedition;
			yield return Construction;
			yield return Repair;
			yield return Condition;
			yield return Damage;
			yield return AnchorageRepair;
			yield return BaseAirCorps;
		}

	}

}
