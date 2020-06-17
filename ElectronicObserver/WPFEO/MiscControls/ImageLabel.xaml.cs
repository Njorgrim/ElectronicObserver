using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElectronicObserver.WPFEO.MiscControls
{
	/// <summary>
	/// Interaction logic for ImageLabel.xaml
	/// </summary>
	public partial class ImageLabel : UserControl
	{
		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
			"Source", typeof(ImageSource), typeof(ImageLabel), new PropertyMetadata(default(ImageSource)));

		public ImageSource Source
		{
			get { return (ImageSource) GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
			"Text", typeof(string), typeof(ImageLabel), new PropertyMetadata(default(string)));

		public string Text
		{
			get { return (string) GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public ImageLabel()
		{
			InitializeComponent();
		}
	}
}
