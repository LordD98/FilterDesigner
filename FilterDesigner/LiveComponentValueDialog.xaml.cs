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
		private OutputWindow Host;

		public LiveComponentValueDialog(List<Component> components, OutputWindow host)
		{
			InitializeComponent();
			Host = host;
			foreach(Component component in components)
			{
				AddComponent(component);
			}
		}

		public void AddComponent(Component component)
		{
			GridComponents.RowDefinitions.Add(new RowDefinition { Height = new GridLength(27) });
			Border border = new Border
			{
				BorderBrush =  Brushes.LightGreen,
				BorderThickness = new Thickness(1),
				Height = 25,
				VerticalAlignment = VerticalAlignment.Top
			};
			DockPanel dockpanel = new DockPanel();
			Label label = new Label
			{
				Width = 50,
				Content = component.Name,
				Padding = new Thickness(3, 0, 0, 0),
				VerticalContentAlignment = VerticalAlignment.Center, 
			};
			Rectangle rect = new Rectangle
			{
				Width = 15,
				Height = 23,
				Fill = Brushes.DarkGray,
				//Fill = 0xFF949494,
				//VerticalAlignment = VerticalAlignment.Center
			};
			Slider sldValue = new Slider()
			{
				VerticalAlignment = VerticalAlignment.Center
			};
			TextBox tbxValue = new TextBox
			{
				Width = 60,
				Height = 17,
				Margin = new Thickness(0,0,3,0),
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				VerticalAlignment = VerticalAlignment.Center
			};
			sldValue.Resources.Add(0, GridComponents.RowDefinitions.Count - 1);
			sldValue.Resources.Add(1, component);
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
			GridComponents.Children.Add(border);
			border.Child = dockpanel;
			dockpanel.Children.Add(rect);
			dockpanel.Children.Add(label);
			dockpanel.Children.Add(tbxValue);
			dockpanel.Children.Add(sldValue);
			DockPanel.SetDock(rect, Dock.Left);
			DockPanel.SetDock(label, Dock.Left);
			DockPanel.SetDock(tbxValue, Dock.Right);
			Grid.SetRow(border, GridComponents.RowDefinitions.Count - 1);
			Slider_SetValue(sldValue, component.GetValue());
			SldValue_ValueChanged(sldValue, null);
		}

		private void Slider_SetValue(Slider slider, double value)
		{
			if(value == 0.0)
			{
				slider.Value = slider.Minimum;
			}
			else
			{
				slider.Value = Math.Log10(Math.Abs(value));
			}
		}

		private void TbxValue_TextChanged(object sender, EventArgs e)
		{
			Component component = (sender as TextBox).Resources[1] as Component;
			Slider slider = ((GridComponents.Children[(int)(sender as TextBox).Resources[0]] as Border).Child as DockPanel).Children[3] as Slider;
			if(component.SetValueStr((sender as TextBox).Text))
			{
				//Green
				(GridComponents.Children[(int)(sender as TextBox).Resources[0]] as Border).BorderBrush = Brushes.Green;
				Host.DrawFunction();
			}
			else
			{
				//Red
				(GridComponents.Children[(int)(sender as TextBox).Resources[0]] as Border).BorderBrush = Brushes.Red;
			}
			Slider_SetValue(slider, component.GetValue());
		}

		private void TbxValue_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Enter)
			{
				// Lose Focus
				Keyboard.Focus(((GridComponents.Children[(int)(sender as TextBox).Resources[0]] as Border).Child as DockPanel).Children[3] as Slider);
				Component component = (sender as TextBox).Resources[1] as Component;
				(sender as TextBox).Text = component.GetValueStr();
				e.Handled = true;
			}
		}

		private void SldValue_ValueChanged(object sender, EventArgs e)
		{
			Slider slider = sender as Slider;
			int row = (int)slider.Resources[0];
			TextBox tbxValue = ((GridComponents.Children[row] as Border).Child as DockPanel).Children[2] as TextBox;
			tbxValue.Text = ValueToText(Math.Pow(10, slider.Value), (slider.Resources[1] as Component).GetComponentType());
		}

		public string ValueToText(double value, ComponentType type)
		{
			string result = "";
			result += Component.PrintValue(value);
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
