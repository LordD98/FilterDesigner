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
    /// Interaktionslogik für ComponentDialog.xaml
    /// </summary>
    public partial class ComponentDialog : Window
    {
		public bool Modified = false;
		public bool TypeModified = false;

		private ComponentType oldType;
		public ComponentType ResultType
		{
			get
			{
				switch((ComponentType)cmbType.SelectedItem)
				{
					case ComponentType.Resistor:
					case ComponentType.Capacitor:
					case ComponentType.Inductor:
						return (ComponentType)cmbType.SelectedItem;
					default:
						return oldType;
				}
			}
		}

		private string oldName;
		public string ResultName
		{
			get { return tbxName.Text; }
		}

		private double oldValue;
		public double ResultValue
		{
			get
			{
				if(Component.ParseValue(tbxValue.Text, out double resultValue))
				{
					return resultValue;
				}
				else
				{
					return oldValue;
				}
			}
		}

		private bool oldShowName;
		public bool ResultShowName
		{
			get
			{
				return chkShowName.IsChecked ?? false;
			}
		}

		private bool oldShowValue;
		public bool ResultShowValue
		{
			get
			{
				return chkShowValue.IsChecked ?? false;
			}
		}

		public ComponentDialog(string name, double value, ComponentType type, bool showName, bool showValue)
        {
			oldName = name;
			oldValue = value;
			oldType = type;
			oldShowName = showName;
			oldShowValue = showValue;
			InitializeComponent();
		}

		private void BtnReset_Click(object sender = null, RoutedEventArgs e = null)
		{
			tbxName.Text = oldName;
			tbxValue.Text = oldValue.ToString();
			cmbType.SelectedItem = oldType;
			chkShowName.IsChecked = oldShowName;
			chkShowValue.IsChecked = oldShowValue;
		}

		private void BtnCancel_Click(object sender, RoutedEventArgs e)
		{
			Modified = false;
			DialogResult = false;
			Close();
		}

		private void BtnOk_Click(object sender, RoutedEventArgs e)
		{
			Modified = false;
			if(!tbxName.Text.Equals(oldName))
				Modified = true;
			double resVal = ResultValue;
			if(resVal != oldValue)
				Modified = true;
			if((ComponentType)cmbType.SelectedItem != oldType)
			{
				TypeModified = true;
				Modified = true;
			}
			if(chkShowName.IsChecked != oldShowName)
				Modified = true;
			if(chkShowValue.IsChecked != oldShowValue)
				Modified = true;
			if(Modified)
				DialogResult = true;
			else
				DialogResult = false;
			Close();
		}

		private void ComponentDialog_Loaded(object sender, EventArgs e)
		{
			cmbType.Items.Add(ComponentType.Resistor);
			cmbType.Items.Add(ComponentType.Capacitor);
			cmbType.Items.Add(ComponentType.Inductor);
			BtnReset_Click();
		}

		private void ComponentDialog_Closed(object sender, EventArgs e)
		{
			if(DialogResult == null)
			{
				Modified = false;
				DialogResult = false;
			}
		}

		//private void Test(object sender, EventArgs e)
		//{
		//	MinHeight = ActualHeight;                   // Fix height => no vertical resize
		//	MaxHeight = ActualHeight;                   //
		//}

		public void ChangeComponentValue(Component component)	// Modifies the value of the given component
		{
			if(component is Resistor)
			{
				(component as Resistor).Resistance = ResultValue;
			}
			else if(component is Capacitor)
			{
				(component as Capacitor).Capacitance = ResultValue;
			}
			else if(component is Inductor)
			{
				(component as Inductor).Inductance = ResultValue;
			}
		}

		private void Dialog_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Enter)
			{
				BtnOk_Click(null, null);
			}
		}
	}
}
