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

		public ComponentType ResultType
		{
			get
			{
				switch((ComponentType)cbxType.SelectedItem)
				{
					case ComponentType.Resistor:
					case ComponentType.Capacitor:
					case ComponentType.Inductor:
						return (ComponentType)cbxType.SelectedItem;
					default:
						return oldType;
				}
			}
		}
		private ComponentType oldType;
		public string ResultName
		{
			get { return tbxName.Text; }
		}
		private string oldName;
		public double ResultValue
		{
			get
			{
				if(double.TryParse(tbxValue.Text, out double resultValue))
				{
					return resultValue;
				}
				else
				{
					return oldValue;
				}
			}
		}
		private double oldValue;

		public ComponentDialog(string name, double value, ComponentType type)
        {
			oldName = name;
			oldValue = value;
			oldType = type;
            InitializeComponent();
		}

		private void BtnReset_Click(object sender = null, RoutedEventArgs e = null)
		{
			tbxName.Text = oldName;
			tbxValue.Text = oldValue.ToString();
			cbxType.SelectedItem = oldType;
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
			if((ComponentType)cbxType.SelectedItem != oldType)
			{
				TypeModified = true;
				Modified = true;
			}
			if(Modified)
				DialogResult = true;
			else
				DialogResult = false;
			Close();
		}

		private void ComponentDialog_Loaded(object sender, EventArgs e)
		{
			cbxType.Items.Add(ComponentType.Resistor);
			cbxType.Items.Add(ComponentType.Capacitor);
			cbxType.Items.Add(ComponentType.Inductor);
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

		private void Test(object sender, EventArgs e)
		{
			MinHeight = ActualHeight;                   // Fix height => no vertical resize
			MaxHeight = ActualHeight;                   //
		}

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
