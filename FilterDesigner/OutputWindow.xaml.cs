using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfMath;

namespace FilterDesigner
{
	/// <summary>
	/// Interaktionslogik für OutputWindow.xaml
	/// </summary>
	public partial class OutputWindow : Window
	{

		double yMax = 20;   // 20dB	for real Test
		double yMin = -120; //-60dB

		double xMin = 0;    // 10^-0.99 Hz
		double xMax = 7;    // 10^-0.01 Hz

		private int outputCycle = 0;
		LiveComponentValueDialog lcvd;
		public Expression Function { get; set; }

		private Point mouseClick = new Point(-1, -1);
		private bool selectionHorizontal = false;

		private double lastDrawWidth = 0;
		private double lastDrawHeight = 0;
		public bool ComponentChangeDialogDocked = true;
		Thread FunctionThread;

		public OutputWindow(Expression function)
		{
			Function = (function as Sum)?.ToCommonDenominator() ?? function;
			Function = Function.ToStandardForm(true);
			
			InitializeComponent();
			CvsGraph.Visibility = Visibility.Visible;
			OutputExpression.Visibility = Visibility.Collapsed;
			ComputeFormula();

			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += Timer_Tick;
			timer.Interval = TimeSpan.FromMilliseconds(500);
			timer.Start();
		}

		public void Update()
		{
			if(outputCycle % 3 == 0)
			{
				DrawFunction();
			}
			else
			{
				ComputeFormula();
			}
		}

		public void ComputeFormula()
		{
			Expression result; 
			if(outputCycle % 3 == 0)
			{
				result = Function;
			}
			else
			{
				result = Function?.EvaluateToConst()?.ToStandardForm(true);
			}
			if(!result.IsConst() || !double.IsPositiveInfinity((Function as ConstExpression)?.Value ?? 0))
			{
				OutputExpression.Formula = result.EvaluateLaTeX();
			}
			else
			{
				OutputExpression.Formula = @"\infty";
			}
		}

		public void Draw()
		{
			lastDrawHeight = CvsGraph.ActualHeight;
			lastDrawWidth = CvsGraph.ActualWidth;
			CvsBase.Children.Clear();
			DrawGrid();
			DrawFunction();
			//if(!Function.AllValuesSet())    // Unknown variables?
			//{
			//	return;
			//}
			//else
			//{
			//	//if(FunctionThread != null && FunctionThread.ThreadState == ThreadState.Running)
			//	//{
			//	//	FunctionThread.Abort();
			//	//}
			//	//FunctionThread = new Thread(DrawFunction);
			//	//FunctionThread.Start();
			//}
		}

		public void DrawFunction()
		{
			CvsGraph.Children.Clear();
			double deltaY = yMax - yMin;
			double deltaX = xMax - xMin;
			double prevY = double.NaN;
			for(int x = 0; x < CvsGraph.ActualWidth; x++)
			{
				double frequency = XCoordinateToFrequency(x);
				double magnitude = 0;
				//magnitude = (Math.Log10(frequency) - xMin) / deltaX * deltaY + yMin;
				magnitude = 10 * Math.Log10(Function.EvaluateImpedance(frequency).Abs2());

				double y = MagnitudeToYCoordinate(magnitude);
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

				//Dispatcher.Invoke(() =>
				//{
				if(!double.IsInfinity(y) && !double.IsNaN(y) && !double.IsInfinity(prevY) && !double.IsNaN(prevY))
				{
					Line line = new Line()
					{
						StrokeThickness = 1,
						Stroke = Brushes.Red,
						X1 = x - 1,
						X2 = x,
						Y1 = prevY,
						Y2 = y
					};
					CvsGraph.Children.Add(line);
				}
				//}, DispatcherPriority.Input);
				prevY = y;
			}
		}

		public void DrawGrid()
		{
			CvsGrid.Children.Clear();
			double deltaY = yMax - yMin;
			double deltaX = xMax - xMin;
			double minFreq = Math.Pow(10, xMin);
			double maxFreq = Math.Pow(10, xMax);

			List<double> magValues = GetNOrders(1, yMin / 20, yMax / 20, out List<double> specialMagValues)[0];
			List<double> freqValues = GetNOrdersLog(Math.Pow(10, xMin), Math.Pow(10, xMax), out List<double> specialFreqValues);
			foreach(double magnitude in magValues)
			{
				double y = MagnitudeToYCoordinate(20 * magnitude);
				CvsGrid.Children.Add(new Line()
				{
					X1 = 0,
					Y1 = y,
					X2 = CvsGraph.ActualWidth,
					Y2 = y,
					Stroke = Brushes.Black,
					StrokeThickness = 1,
					SnapsToDevicePixels = true
				});
				TextBlock desc = new TextBlock();
				if(magnitude == 0)
				{
					desc.Text = "0";
				}
				else
				{
					desc.Text = string.Format("{0:###}", 20 * magnitude);
				}
				CvsBase.Children.Add(desc);
				Canvas.SetTop(desc, y + CvsGraph.Margin.Top - 10);
				if(magnitude >= 0)
				{
					Canvas.SetLeft(desc, 8);
				}
				else
				{
					Canvas.SetLeft(desc, 3);
				}
			}
			if(specialMagValues != null)
			{
				foreach(double magnitude in specialMagValues)
				{
					double y = MagnitudeToYCoordinate(20 * magnitude);
					CvsGrid.Children.Add(new Line()
					{
						X1 = 0,
						Y1 = y,
						X2 = CvsGrid.ActualWidth,
						Y2 = y,
						Stroke = Brushes.Black,
						StrokeThickness = 3,
						SnapsToDevicePixels = true
					});
					TextBlock desc = new TextBlock();
					if(magnitude == 0)
					{
						desc.Text = "0";
					}
					else
					{
						desc.Text = string.Format("{0:###}", 20 * magnitude);
					}
					CvsBase.Children.Add(desc);
					Canvas.SetTop(desc, y + CvsGrid.Margin.Top - 10);
					if(magnitude >= 0)
					{
						Canvas.SetLeft(desc, 8);
					}
					else
					{
						Canvas.SetLeft(desc, 3);
					}
				}
			}
			foreach(double frequency in freqValues)
			{
				double x = FrequencyToXCoordinate(frequency);
				CvsGrid.Children.Add(new Line()
				{
					X1 = x,
					Y1 = 0,
					X2 = x,
					Y2 = CvsGrid.ActualHeight,
					Stroke = Brushes.Black,
					StrokeThickness = 1,
					SnapsToDevicePixels = true
				});
			}
			//foreach(double frequencyLog in freqLogValues[1])
			//{
			//	double x = FrequencyToXCoordinate(Math.Pow(10, frequencyLog));
			//	CvsGrid.Children.Add(new Line()
			//	{
			//		X1 = x,
			//		Y1 = 0,
			//		X2 = x,
			//		Y2 = CvsGrid.ActualHeight,
			//		Stroke = Brushes.Gray,
			//		StrokeThickness = 1,
			//		StrokeDashArray = { 2, 2 },
			//		SnapsToDevicePixels = true
			//	});
			//}
			if(specialFreqValues != null)
			{
				foreach(double frequency in specialFreqValues)
				{
					double x = FrequencyToXCoordinate(frequency);
					CvsGrid.Children.Add(new Line()
					{
						X1 = x,
						Y1 = 0,
						X2 = x,
						Y2 = CvsGrid.ActualHeight,
						Stroke = Brushes.Black,
						StrokeThickness = 3,
						SnapsToDevicePixels = true
					});
					TextBlock desc = new TextBlock() { Text = DoubleToText(frequency) };
					CvsBase.Children.Add(desc);
					Canvas.SetLeft(desc, x + CvsGraph.Margin.Left - 5);
					Canvas.SetTop(desc, CvsBase.ActualHeight - CvsGraph.Margin.Left + 5);
				}
			}

			/*
			//double frequency = Math.Pow(10, x / CvsGraph.ActualWidth * deltaX + xMin);
			int minPow = (int)Math.Log10(minFreq);
			int maxPow = (int)Math.Log10(maxFreq);
			if(minPow == maxPow)	// Combine these two ifs?
			{
				int min = (int)(Math.Pow(10, minPow+1) * minFreq);
				int max = (int)(Math.Pow(10, maxPow+1) * maxFreq);
				for(int i = min; i<=max; i++)
				{
					double freq = i * Math.Pow(10, minPow-1);
					double x = (Math.Log10(freq) - xMin) / deltaX * CvsGraph.ActualWidth;
					CvsGraph.Children.Add(new Line()
					{
						X1 = x,
						Y1 = 0,
						X2 = x,
						Y2 = CvsGraph.ActualHeight,
						Stroke = Brushes.Gray
					});
				}
			}
			else
			{
				for(int i = (int)(xMin); i <= (int)xMax; i++)
				{
					double freq = Math.Pow(10, i);
					double x = (Math.Log10(freq) - xMin) / deltaX * CvsGraph.ActualWidth;
					Dispatcher.Invoke(() =>
					{
						CvsGraph.Children.Add(new Line()
						{
							X1 = x,
							Y1 = 0,
							X2 = x,
							Y2 = CvsGraph.ActualHeight,
							Stroke = Brushes.Gray
						});
					});
				}
			}
			*/
		}

		public double MagnitudeToYCoordinate(double mag)
		{
			if(yMax == yMin)
				return CvsGraph.ActualHeight / 2;
			else
				return (1 - (mag - yMin) / (yMax - yMin)) * CvsGraph.ActualHeight;
		}

		public double YCoordinateToMagnitude(double y)
		{
			return yMin + (yMax - yMin) * (1 - y / CvsGraph.ActualHeight);
		}

		public double FrequencyToXCoordinate(double freq)
		{
			if(freq == 0)
				return 0;
			else if(double.IsInfinity(xMin) || double.IsInfinity(xMax) || (xMin == xMax))
				return CvsGraph.ActualWidth / 2;
			else
				return (Math.Log10(freq) - xMin) / (xMax - xMin) * CvsGraph.ActualWidth;
		}

		public double XCoordinateToFrequency(double x)
		{
			double result = Math.Pow(10, x / CvsGraph.ActualWidth * (xMax - xMin) + xMin);
			if(double.IsNaN(result) || double.IsInfinity(result))
				result = double.MaxValue;
			if(result == 0)
				result = double.Epsilon;
			return result;
		}

		private void OutputWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.LeftShift)
			{
				if(CvsBase.Children.Contains(selectionRectangle))
				{
					selectionHorizontal = !selectionHorizontal;
				}
			}
			else if(e.Key == Key.Enter)
			{
				if(outputCycle % 3 != 2)
				{
					ComputeFormula();
					ScrOutExp.Visibility = Visibility.Visible;
					OutputExpression.Visibility = Visibility.Visible;
					TbDescF.Visibility = Visibility.Collapsed;
					TbDescA.Visibility = Visibility.Collapsed;
					TbMousePos.Visibility = Visibility.Collapsed;
					Border.Visibility = Visibility.Collapsed;
					CvsBase.Visibility = Visibility.Collapsed;
					CvsGraph.Visibility = Visibility.Collapsed;
					CvsGrid.Visibility = Visibility.Collapsed;
				}
				else
				{
					ScrOutExp.Visibility = Visibility.Collapsed;
					OutputExpression.Visibility = Visibility.Collapsed;
					TbDescF.Visibility = Visibility.Visible;
					TbDescA.Visibility = Visibility.Visible;
					TbMousePos.Visibility = Visibility.Visible;
					Border.Visibility = Visibility.Visible;
					CvsBase.Visibility = Visibility.Visible;
					CvsGraph.Visibility = Visibility.Visible;
					CvsGrid.Visibility = Visibility.Visible;
				}
				outputCycle++;
			}
			else if(e.Key == Key.R)
			{
				if(e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
				{
					xMin = 0;
					xMax = 7;
					yMin = -120;
					yMax = 20;
				}
				Draw();
			}
			else if(e.Key == Key.D)
			{
				if(lcvd.WindowState == WindowState.Minimized)
				{
					lcvd.WindowState = WindowState.Normal;
				}
				DockLCVD();
				ComponentChangeDialogDocked = true;
			}
		}

		private void OutputWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if(Border.Visibility != Visibility.Collapsed)
			{
				CvsGraph.Width = Border.ActualWidth - 4;
				CvsGraph.Height = Border.ActualHeight - 4;
				CvsGrid.Width = Border.ActualWidth - 4;
				CvsGrid.Height = Border.ActualHeight - 4;
				Draw();
			}
		}


		private const int WmExitSizeMove = 0x232;
		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			var helper = new System.Windows.Interop.WindowInteropHelper(this);
			if(helper.Handle != null)
			{
				var source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
				if(source != null)
					source.AddHook(HwndMessageHook);
			}

			lcvd = new LiveComponentValueDialog(MainWindow.allComponents, this)
			{
				Left = Left + ActualWidth - 14,
				Top = Top,
				Height = ActualHeight
			};
			//lcvd.BringIntoView();
			lcvd.Show();
		}

		private IntPtr HwndMessageHook(IntPtr wnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch(msg)
			{
				case WmExitSizeMove:
					if(!Function.AllValuesSet())    // Unknown variables?
					{
						break;
					}
					DrawFunction();
					handled = true;
					break;
			}
			return IntPtr.Zero;
		}

		private void Timer_Tick(object sender, object y)
		{
			//lastDrawHeight = CvsGraph.ActualHeight;
			//lastDrawWidth = CvsGraph.ActualWidth;
			if(CvsGraph.ActualWidth != lastDrawWidth || CvsGraph.ActualHeight != lastDrawHeight)
			{
				OutputWindow_SizeChanged(sender, null);
				if(!Function.AllValuesSet())    // Unknown variables?
				{
					return;
				}
				DrawFunction();
			}
		}

		static List<double> GetNOrdersLog(double min, double max, out List<double> specialValues)
		{
			List<double> result = new List<double>();
			specialValues = new List<double>();
			int j = 10 * (int)Math.Log10(min);
			int iMin = (int)(min / Math.Pow(10, j / 10) - 1 + 0.5) + j - 1;

			j = 10 * (int)Math.Log10(max);
			int iMax = (int)(max / Math.Pow(10, j / 10) - 1 + 0.5) + j + 1;
			//iMax = 10 * (int)Math.Log10(Max / Min);
			for(int i = iMin; i < iMax; i++)
			{
				double x = Math.Pow(10, Math.Floor(i / 10.0)) * (Mod(i, 10) + 1);
				if(x <= max && x >= min)
				{
					if(!result.Contains(x))
					{
						result.Add(x);
					}
					else
					{
						result.Remove(x);
						specialValues.Add(x);
					}
				}
			}
			return result;
		}

		static int Mod(int x, int m)
		{
			return (x % m + m) % m;
		}


		static List<double>[] GetNOrders(int n, double min, double max, out List<double> specialValues, int specialMask = 10)
		{
			specialValues = null;
			List<double>[] result = new List<double>[n];
			int powerStep = (int)Math.Log10(max - min);
			result[0] = GetPowerSeries(powerStep, min, max, out List<double> specVals, specialMask);
			if(specVals != null && specVals.Count != 0)
				specialValues = specVals;
			for(int i = 1; i < n; i++)
			{
				powerStep--;
				result[i] = GetPowerSeries(powerStep, min, max, out _);
			}
			return result;
		}

		static List<double> GetPowerSeries(int exp, double min, double max, out List<double> specialValues, int specialMask = 10)
		{
			specialValues = null;
			int maxN = (int)(max / (Math.Pow(10, exp))) + 1;
			int minN = (int)(min / (Math.Pow(10, exp)) + 0.5) - 1;
			List<double> result = new List<double>();
			for(int i = minN; i <= maxN; i++)
			{
				double number = i * Math.Pow(10, exp);
				if(number >= min && number <= max)
				{
					if(i % specialMask != 0)
					{
						result.Add(number);
					}
					else
					{
						if(specialValues == null)
						{
							specialValues = new List<double>();
						}
						specialValues.Add(number);
					}
				}
			}
			return result;
		}

		public static string DoubleToText(double d)
		{
			int exponent = (int)Math.Floor(Math.Log10(d));
			double mantisse = Math.Round(d / Math.Pow(10, exponent), 2, MidpointRounding.AwayFromZero);
			string result = "";
			if(mantisse != 1.0)
			{
				result += string.Format("{0:#.##}\xB7", mantisse);
			}
			if(exponent != 0)
			{
				result += string.Format("10{0}", StrToSuperscript(string.Format("{0:##}", exponent)));
			}
			if(result == "")
			{
				result = "1";
			}
			return result;
		}

		static string StrToSuperscript(string s)
		{
			string result = "";
			foreach(char c in s)
			{
				switch(c)
				{
					case '0':
						result += "\x2070";
						break;
					case '1':
						result += "\x00B9";
						break;
					case '2':
						result += "\x00B2";
						break;
					case '3':
						result += "\x00B3";
						break;
					case '4':
						result += "\x2074";
						break;
					case '5':
						result += "\x2075";
						break;
					case '6':
						result += "\x2076";
						break;
					case '7':
						result += "\x2077";
						break;
					case '8':
						result += "\x2078";
						break;
					case '9':
						result += "\x2079";
						break;
					case '+':
						result += "\x207A";
						break;
					case '-':
						result += "\x207B";
						break;
					default:
						result += c;
						break;
				}
			}
			return result;
		}

		private void OutputWindow_Closed(object sender, EventArgs e)
		{
			lcvd?.Close();
		}

		private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			mouseClick = e.GetPosition(CvsGraph);
			if(e.ChangedButton == MouseButton.Left)
			{
				double frequency = XCoordinateToFrequency(mouseClick.X);
				double magnitude = YCoordinateToMagnitude(mouseClick.Y);
				//MessageBox.Show($"Click:\nx: {mouseClick.X}\ny: {mouseClick.Y}\nf  : {Component.PrintValue(frequency)}Hz\nA : {magnitude:#0.##} dB");
				if(selectionRectangle == null)
				{
					selectionRectangle = new Rectangle
					{
						Fill = Brushes.LightBlue,
						Opacity = 0.5
					};
				}
			}
			else
			{

			}
		}

		private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if(e.ChangedButton == MouseButton.Left)
			{
				Point newMousePos = e.GetPosition(CvsGraph);
				if(selectionRectangle != null && CvsBase.Children.Contains(selectionRectangle))
				{
					CvsBase.Children.Remove(selectionRectangle);
				}
				if(mouseClick.X != -1 && mouseClick.Y != -1)
				{
					double leftX = Math.Min(mouseClick.X, newMousePos.X);
					double rightX = Math.Max(mouseClick.X, newMousePos.X);
					double lowY = Math.Max(mouseClick.Y, newMousePos.Y);
					double highY = Math.Min(mouseClick.Y, newMousePos.Y);
					double newXMin = xMin;
					double newXMax = xMax;
					double newYMin = yMin;
					double newYMax = yMax;
					if(leftX != rightX && leftX > 0)
					{
						newXMin = Math.Log10(XCoordinateToFrequency(leftX));
						newXMax = Math.Log10(XCoordinateToFrequency(rightX));
					}
					if(lowY != highY && highY > 0)
					{
						newYMin = YCoordinateToMagnitude(lowY);
						newYMax = YCoordinateToMagnitude(highY);
					}
					if(double.IsInfinity(newXMin) || double.IsNaN(newXMin))
					{
						newXMin = double.MinValue;
					}
					if(double.IsInfinity(newXMax) || double.IsNaN(newXMax))
					{
						newXMax = double.MaxValue;
					}
					if(Keyboard.IsKeyDown(Key.LeftCtrl))
					{
						if(selectionHorizontal)
						{
							newYMin = yMin;
							newYMax = yMax;
						}
						else
						{
							newXMin = xMin;
							newXMax = xMax;
						}
					}
					xMin = newXMin;
					xMax = newXMax;
					yMin = newYMin;
					yMax = newYMax;
					mouseClick.X = -1;
					mouseClick.Y = -1;
					Draw();
				}
			}
		}

		Rectangle selectionRectangle;
		private void OutputWindow_MouseMove(object sender, MouseEventArgs e)
		{
			Point mouse = e.GetPosition(CvsGraph);
			double frequency = XCoordinateToFrequency(mouse.X);
			double magnitude = YCoordinateToMagnitude(mouse.Y);
			TbMousePos.Text = $"f  : {Component.PrintValue(frequency)}Hz\nA : {magnitude:#0.##}dB";

			if(mouseClick.X != -1 && mouseClick.Y != -1 && selectionRectangle != null)
			{
				//Draw Rectangle
				if(CvsBase.Children.Contains(selectionRectangle) && !Mouse.LeftButton.HasFlag(MouseButtonState.Pressed))
				{
					CvsBase.Children.Remove(selectionRectangle);
				}
				else if(!CvsBase.Children.Contains(selectionRectangle) && Mouse.LeftButton.HasFlag(MouseButtonState.Pressed))
				{
					CvsBase.Children.Add(selectionRectangle);
				}
				selectionRectangle.Width = Math.Abs(mouse.X - mouseClick.X);
				selectionRectangle.Height = Math.Abs(mouse.Y - mouseClick.Y);
				if(Keyboard.IsKeyDown(Key.LeftCtrl))
				{
					//if(selectionRectangle.Width > selectionRectangle.Height)
					if(selectionHorizontal)
					{
						selectionRectangle.Height = 1;
						Canvas.SetLeft(selectionRectangle, Math.Min(mouseClick.X, mouse.X) + CvsGraph.Margin.Left);
						Canvas.SetTop(selectionRectangle, mouseClick.Y + CvsGraph.Margin.Top);

						selectionRectangle.Height = CvsGraph.Height;
						Canvas.SetLeft(selectionRectangle, Math.Min(mouseClick.X, mouse.X) + CvsGraph.Margin.Left);
						Canvas.SetTop(selectionRectangle, CvsGraph.Margin.Top);

					}
					else
					{
						selectionRectangle.Width = 1;
						Canvas.SetTop(selectionRectangle, Math.Min(mouseClick.Y, mouse.Y) + CvsGraph.Margin.Top);
						Canvas.SetLeft(selectionRectangle, mouseClick.X + CvsGraph.Margin.Left);

						selectionRectangle.Width = CvsGraph.Width;
						Canvas.SetTop(selectionRectangle, Math.Min(mouseClick.Y, mouse.Y) + CvsGraph.Margin.Top);
						Canvas.SetLeft(selectionRectangle, CvsGraph.Margin.Left);
					}
				}
				else
				{
					Canvas.SetLeft(selectionRectangle, Math.Min(mouseClick.X, mouse.X) + CvsGraph.Margin.Left);
					Canvas.SetTop(selectionRectangle, Math.Min(mouseClick.Y, mouse.Y) + CvsGraph.Margin.Top);
				}
			}
			else
			{
				if(CvsBase.Children.Contains(selectionRectangle))
					CvsBase.Children.Remove(selectionRectangle);
			}
		}

		private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			//TbMousePos.Text = e.Delta.ToString();
			Point currentPos = e.GetPosition(CvsGraph);
			double currentFreq = XCoordinateToFrequency(currentPos.X);
			double currentMagn = YCoordinateToMagnitude(currentPos.Y);
			double dx = xMax - xMin;
			double dy = yMax - yMin;
			double factor = 1.2;
			if(e.Delta > 0)
				factor = 1 / factor;

			double xMin2 = Math.Log10(currentFreq) - factor * currentPos.X / CvsGraph.ActualWidth * dx;
			double xMax2 = xMin2 + factor * dx;
			if(currentFreq == 0 || double.IsInfinity(currentFreq) )
			{
				xMin2 = double.MinValue;
				xMax2 = double.MaxValue;
			}
			double yMin2 = currentMagn - factor * (1 - currentPos.Y / CvsGraph.ActualHeight) * dy;
			double yMax2 = yMin2 + factor * dy;

			xMin = xMin2;
			xMax = xMax2;
			yMin = yMin2;
			yMax = yMax2;
			Draw();
		}

		private void OutputWindow_Activated(object sender, EventArgs e)
		{
			//lcvd?.BringIntoView();
			if(lcvd != null)
			{
				//if(lcvd.WindowState == WindowState.Minimized)
				//{
				//	lcvd.WindowState = WindowState.Normal;
				//}
				//lcvd.Activate();
				//Focus();
				lcvd.Topmost = true;
				lcvd.Topmost = false;
			}
			//Activate();
		}

		private void OutputWindow_LocationChanged(object sender, EventArgs e)
		{
			if(ComponentChangeDialogDocked)
			{
				DockLCVD();
			}
		}

		private void DockLCVD()
		{
			if(lcvd != null)
			{
				lcvd.Width = 300;
				lcvd.Height = ActualHeight;
				lcvd.Left = Left + ActualWidth - 14;
				lcvd.Top = Top;
			}
		}
	}
}
