﻿using System;
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

		double yMax = 20;   // 20dB
		double yMin = -60;  //-60dB

		double xMin = -3;   // 10^-3 Hz
		double xMax = 6;    // 10^6 Hz

		public Expression Function { get; set; }

		public OutputWindow(Expression function)
		{
			Function = function.ToCommonDenominator();
			Function = Function.ToStandardForm();
			InitializeComponent();
			CvsGraph.Visibility = Visibility.Visible;
			OutputExpression.Visibility = Visibility.Collapsed;
			if(!Function.IsConst() || !double.IsPositiveInfinity((Function as ConstExpression).Value))
			{
				OutputExpression.Formula = Function.EvaluateLaTeX();
			}
			else
			{
				OutputExpression.Formula = @"\infty";
			}
		}

		private void Draw()
		{
			if(!Function.AllValuesSet())
				return;
			CvsGraph.Children.Clear();
			double deltaY = yMax - yMin;
			double deltaX = xMax - xMin;
			double prevY = double.NaN;
			for(int x = 0; x < CvsGraph.ActualWidth; x++)
			{
				double frequency = Math.Pow(10, x / CvsGraph.ActualWidth * deltaX + xMin);
				double magnitude = 0; 
				//magnitude = (Math.Log10(frequency) - xMin) / deltaX * deltaY + yMin;
				magnitude = 10 * Math.Log10(Function.EvaluateImpedance(frequency).Abs2());

				double y = (1 - (magnitude - yMin) / deltaY) * CvsGraph.ActualHeight;
				if(double.IsNaN(prevY))
					prevY = y;
				//Ellipse ellipse = new Ellipse
				//{
				//	Width = 2,
				//	Height = 2,
				//	Fill = Brushes.Black
				//};
				//CvsGraph.Children.Add(ellipse);
				//Canvas.SetLeft(ellipse, x - 1);
				//Canvas.SetTop(ellipse, y - 1);
				Line line = new Line()
				{
					StrokeThickness = 1,
					Stroke = Brushes.Black,
					X1 = x - 1,
					X2 = x,
					Y1 = prevY,
					Y2 = y
				};
				CvsGraph.Children.Add(line);
				prevY = y;
			}
		}

		private void OutputWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Enter)
			{
				if(OutputExpression.Visibility == Visibility.Collapsed)
				{
					OutputExpression.Visibility = Visibility.Visible;
					CvsGraph.Visibility = Visibility.Collapsed;
				}
				else
				{
					OutputExpression.Visibility = Visibility.Collapsed;
					CvsGraph.Visibility = Visibility.Visible;
				}
			}
			else if(e.Key == Key.D)
			{
				Draw();
			}
		}

		private void OutputWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Draw();
		}
	}
}
