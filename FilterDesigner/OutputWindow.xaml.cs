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
using WpfMath;

namespace FilterDesigner
{
	/// <summary>
	/// Interaktionslogik für OutputWindow.xaml
	/// </summary>
	public partial class OutputWindow : Window
	{
		public Expression Function { get; set; }

		public OutputWindow(Expression function)
		{
			Function = function.ToCommonDenominator();
			Function = Function.ToStandardForm();
			InitializeComponent();
			if(!Function.IsConst() || !double.IsPositiveInfinity((Function as ConstExpression).Value))
			{
				OutputExpression.Formula = Function.EvaluateLaTeX();
			}
			else
			{
				OutputExpression.Formula = @"\infty";
			}
		}
	}
}
