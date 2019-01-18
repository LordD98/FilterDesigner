using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FilterDesigner
{
	/// <summary>
	/// Interaktionslogik für ConnectionPort.xaml
	/// </summary>
	public partial class ConnectionPort : UserControl
	{
		public static Brush SelectionColorBrush = new SolidColorBrush(Color.FromArgb(0xFF,0xFF,0x20,0x20));
		public const double Radius = 5;

		public ConnectionPort()
		{
			InitializeComponent();
		}

		private void Ellipse_Enter(object sender, MouseEventArgs e)
		{
			(sender as Ellipse).Stroke = SelectionColorBrush;
		}

		private void Ellipse_Leave(object sender, MouseEventArgs e)
		{
			(sender as Ellipse).Stroke = Brushes.Transparent;
		}
	}
}
