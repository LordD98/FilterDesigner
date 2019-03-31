using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static FilterDesigner.MainWindow;

namespace FilterDesigner
{
	/// <summary>
	/// Interaktionslogik für ComponentValues.xaml
	/// </summary>
	public partial class LiveComponentValueDialog : Window
	{
		public LiveComponentValueDialog(List<Component> components)
		{
			InitializeComponent();
			foreach(Component component in components)
			{
				AddComponent(component);
			}
		}

		public void AddComponent(Component component)
		{
			while(GridComponents.Children.Count / 4 >= GridComponents.RowDefinitions.Count)
			{
				GridComponents.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
			}
			TextBlock tbk = new TextBlock { Width = double.NaN, Text = component.Name, VerticalAlignment = VerticalAlignment.Center };
			Rectangle rect = new Rectangle { Width = double.NaN, Fill = Brushes.DarkGray, VerticalAlignment = VerticalAlignment.Center };
			Slider sldValue = new Slider { Width = double.NaN };
			sldValue.Resources.Add(0, GridComponents.RowDefinitions.Count - 1);
			sldValue.Resources.Add(1, component);
			TextBox tbxValue = new TextBox { Width = double.NaN };
			tbxValue.Resources.Add(0, GridComponents.RowDefinitions.Count - 1);
			tbxValue.Resources.Add(1, component);
			if(component is Resistor)
			{
				// 1µOhm-100TOhm
				sldValue.Minimum = -6;
				sldValue.Maximum = 11;
			}
			else if(component is Capacitor)
			{
				// 1pF-100F
				sldValue.Minimum = -12;
				sldValue.Maximum = 2;
			}
			else // Inductor
			{
				// 1nH-100H
				sldValue.Minimum = -9;
				sldValue.Maximum = 2;
			}
			sldValue.ValueChanged += SldValue_ValueChanged;
			tbxValue.KeyDown += TbxValue_KeyDown;
			tbxValue.TextChanged += TbxValue_TextChanged;
			GridComponents.Children.Add(rect);
			GridComponents.Children.Add(tbk);
			GridComponents.Children.Add(sldValue);
			GridComponents.Children.Add(tbxValue);
			Grid.SetColumn(rect, 0);
			Grid.SetColumn(tbk, 1);
			Grid.SetColumn(sldValue, 2);
			Grid.SetColumn(tbxValue, 3);
			Grid.SetRow(rect, GridComponents.RowDefinitions.Count - 1);
			Grid.SetRow(tbk, GridComponents.RowDefinitions.Count - 1);
			Grid.SetRow(sldValue, GridComponents.RowDefinitions.Count - 1);
			Grid.SetRow(tbxValue, GridComponents.RowDefinitions.Count - 1);
		}

		private void TbxValue_TextChanged(object sender, EventArgs e)
		{
			((sender as TextBox).Resources[1] as Component).SetValueStr((sender as TextBox).Text);
		}

		private void TbxValue_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Enter)
			{
				// Lose Focus
				Keyboard.Focus(GridComponents.Children[4*(int)(sender as TextBox).Resources[0]+2]);
				e.Handled = true;
			}
		}

		private void SldValue_ValueChanged(object sender, EventArgs e)
		{
			Slider slider = sender as Slider;
			int row = (int)slider.Resources[0];
			TextBox tbxValue = GridComponents.Children[row * 4 + 3] as TextBox;
			tbxValue.Text = ValueToText(Math.Pow(10, slider.Value), (slider.Resources[1] as Component).GetComponentType());
		}

		public string ValueToText(double value, ComponentType type)
		{
			int exponent = ((int)Math.Floor(Math.Log10(value) / 3)) * 3;
			double mantisse = Math.Round(value / Math.Pow(10, exponent), 2, MidpointRounding.AwayFromZero);
			string result = "";
			if(mantisse != 1.0)
			{
				result += string.Format("{0:0.##}", mantisse);
			}
			else
			{
				result += "1";
			}
			switch(exponent)
			{
				case -12:
					result += "p";
					break;
				case -9:
					result += "n";
					break;
				case -6:
					result += "µ";
					break;
				case -3:
					result += "m";
					break;
				case 3:
					result += "k";
					break;
				case 6:
					result += "M";
					break;
				case 9:
					result += "G";
					break;
				case 12:
					result += "T";
					break;
				default:
					break;
			}
			switch(type)
			{
				case ComponentType.Resistor:
					result += "\x03A9";
					break;
				case ComponentType.Inductor:
					result += "H";
					break;
				case ComponentType.Capacitor:
					result += "F";
					break;
			}
			return result;
		}
	}
}
