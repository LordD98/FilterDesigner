using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
//using static FilterDesigner.MainWindow;

namespace FilterDesigner
{
	// TODO:
	// Add Dialog to change names & values
	// Split line => drag immediately
	// Merge branchpoints, delete lines and split nets
	// GetSummands() deep search with recursion?
	// Simplify R1*R1 => R1^2
	// Add output in LateX
	// Add window to plot transfer function, change values on the fly

	public partial class MainWindow : Window
	{
		public static List<Component> allComponents;
		public static ObservableCollection<Net> allNets;

		private static Brush ButtonBackgroundBrush;

		public enum ComponentType { None, Resistor, Capacitor, Inductor };
		public static ComponentType ComponentToAdd = ComponentType.None;
		public static ComponentRotation AddComponentRotation = ComponentRotation.H1;

		public static bool clickedComponent = false;    // Indicates wether a component is clicked
														// before canvas click executes
		public static RoutedCommand Clear = new RoutedUICommand("Clear", "Clear", typeof(MainWindow));

		public MainWindow()
		{
			InitializeComponent();
			Component.mainWindow = this;
			Component.baseCanvas = canvas;
			Net.mainWindow = this;
			Net.baseCanvas = canvas;
			Branchpoint.baseCanvas = canvas;
			canvas.MouseLeftButtonUp += Canvas_LeftMouseUp;
			canvas.MouseLeftButtonDown += Canvas_LeftMouseDown;
			canvas.MouseLeftButtonDown += Net.Canvas_LeftMouseDown;
			canvas.MouseLeftButtonDown += PlaceComponent;
			canvas.MouseRightButtonDown += (x, y) =>
			{
				SetIndicator(ComponentType.None);
				Net.CancelCurrentLine();
			};
			canvas.MouseLeftButtonUp += Component.Symbol_MouseUp;
			canvas.MouseMove += Component.Symbol_MouseMove;

			ButtonBackgroundBrush = btnResistor.Background;
			//Toolbox.DataContext = Component;

			allComponents = new List<Component>();
			allNets = new ObservableCollection<Net>();
			cbxNet1.ItemsSource = allNets;
			cbxNet2.ItemsSource = allNets;

			//Net GND = new Net("GND");
			//Net U1 = new Net("U1");
			//Net U2 = new Net("U2");
			//Net U_dash = new Net("U'");

			//Resistor R1 = new Resistor("R1", 100, 100);
			//Resistor R2 = new Resistor("R2", 200, 100);
			//Resistor R_dash = new Resistor("R'", 150, 200);
			//Capacitor C1 = new Capacitor("C1", 300, 150);
			//Inductor L = new Inductor("L", 200, 300);

			//R1.Connect(U1, U_dash);
			//R2.Connect(U_dash, U2);
			//R_dash.Connect(U2, U1);
			//C1.Connect(GND, U2);

			//DrawAll();

			//DispatcherTimer timer = new DispatcherTimer();
			//timer.Tick += Timer_Tick;
			//timer.Interval = TimeSpan.FromMilliseconds(100);
			//timer.Start();
		}

		void Timer_Tick(object x, object y)
		{
			if(Keyboard.FocusedElement != null && Keyboard.FocusedElement != tbxResult && !tbxResult.Text.EndsWith(Keyboard.FocusedElement?.ToString()))
			{
				if(tbxResult.Text != "")
					tbxResult.Text += "\n";
				tbxResult.Text += Keyboard.FocusedElement?.ToString();
			}
		}

		private void Canvas_LeftMouseDown(object sender, MouseEventArgs e)
		{
			if(!clickedComponent)
			{
				Keyboard.ClearFocus();
				FocusManager.SetFocusedElement(this, this);
				Focus();
			}
		}

		private void Canvas_LeftMouseUp(object sender, MouseEventArgs e)
		{
			clickedComponent = false;
		}

		public List<Path> FindPaths(Net A, Net B, List<Component> forbiddenComponents = null)
		{
			List<Path> paths = new List<Path>();
			bool done = false;
			Net currentNet = A;
			//Component source;
			//List<Net> visitedNets = new List<Net>();	// Nets that are completed
			Path currentPath = new Path
			{
				Start = A,
				End = B
			};
			List<Net> currentNetOrder = new List<Net>	// Represents the current parse state 
			{
				A
			};

			//List<Component> usedComponents = new List<Component>();
			Component currentComp = null;
			while(!done)
			{
				currentComp = currentNet.GetNextComponent(currentComp);
				if(currentComp == null)
				{
					currentNetOrder.Remove(currentNet);     // maybe combine these? (currentNet always
					if(currentNetOrder.Count == 0)
					{
						done = true;
					}
					else
					{
						currentComp = currentPath.Components.Last();
						currentPath.Components.Remove(currentComp);
						currentNet = currentNetOrder.Last();    //				of currentNetOrder?)

					}
					continue;
				}

				if(currentPath.Components.Contains(currentComp)
					|| (forbiddenComponents?.Contains(currentComp) ?? false)
					|| currentNetOrder.Contains(currentComp.OtherNet(currentNet))
					|| currentComp.IsUseless())    // comp. is connected badly
				{
					continue;
				}
				else
				{
					if(currentComp.IsConnected(B))
					{
						paths.Add(currentPath.Copy().Add(currentComp));
						continue;
					}
					else
					{
						currentNet = currentComp.OtherNet(currentNet);
						currentNetOrder.Add(currentNet);
						currentPath.Add(currentComp);
						currentComp = null;
					}
				}

				//if()
			}

			return paths;
		}

		public List<Component> GetComponentsInPaths(List<Path> paths)
		{
			List<Component> result = new List<Component>();
			foreach(Path path in paths)
			{
				foreach(Component comp in path.Components)
				{
					if(!result.Contains(comp))
					{
						result.Add(comp);
					}
				}
			}
			return result;
		}

		public string GetImpedanceOfPaths(List<Path> paths)
		{
			if(paths.Count == 0)
			{
				Debug.Assert(false, "Paths are empty!");
				return null;
			}
			string impedance = "";

			if(paths.Count == 1)
			{
				return paths[0].GetImpedance();
			}

			Net startNet = paths[0].Start;
			Net endNet = paths[0].End;

			// Find all (intermediate) nets that are common to all Paths
			List<Net> commonNets = new List<Net>();
			foreach(Net intermediateNet in paths[0].GetIntermediateNets())
			{
				if(paths.All(p => p.ContainsNet(intermediateNet)))
				{
					commonNets.Add(intermediateNet);
				}
			}
			commonNets.Add(endNet);
			if(commonNets.Count == 1)   // No non-trivial common nets
			{
				// There are at least two completely independent List<Path>s
				// Find all groups of paths
				List<List<Path>> listListPaths = new List<List<Path>>();
				foreach(Path path in paths)
				{
					int index = listListPaths.FindIndex(l => l.All(p => p.HasCommonNets(path)));
					if(index != -1)
					{
						listListPaths[index].Add(path);
					}
					else
					{
						listListPaths.Add(new List<Path>());
						listListPaths[listListPaths.Count - 1].Add(path);
					}
				}

				impedance += "1/(";
				foreach(List<Path> lp in listListPaths)
				{
					impedance += $"1/({GetImpedanceOfPaths(lp)})+";
				}
				impedance = impedance.Remove(impedance.Length - 1, 1);
				impedance += ")";
			}
			else
			{
				Net lastCommonNet = paths[0].Start;
				foreach(Net commonNet in commonNets)
				{
					List<Path> stepPaths = new List<Path>();
					foreach(Path p in paths)
					{
						Path subPath = p.SubPath(lastCommonNet, commonNet);
						if(!stepPaths.Contains(subPath))
						{
							stepPaths.Add(subPath);
						}
					}
					impedance += GetImpedanceOfPaths(stepPaths) + "+";
					lastCommonNet = commonNet;
				}
				impedance = impedance.Remove(impedance.Length - 1, 1);
			}
			return impedance;
		}

		public Expression GetExpressionOfPaths(List<Path> paths)
		{
			if(paths.Count == 0)
				return null;

			if(paths.Count == 1)
			{
				return paths[0].GetExpression();
			}

			Net startNet = paths[0].Start;
			Net endNet = paths[0].End;

			// Find all (intermediate) nets that are common to all Paths
			List<Net> commonNets = new List<Net>();
			foreach(Net intermediateNet in paths[0].GetIntermediateNets())
			{
				if(paths.All(p => p.ContainsNet(intermediateNet)))
				{
					commonNets.Add(intermediateNet);
				}
			}
			commonNets.Add(endNet);
			if(commonNets.Count == 1)   // No non-trivial common nets
			{
				// There are at least two completely independent List<Path>s
				// Find all groups of paths
				List<List<Path>> listListPaths = new List<List<Path>>();
				foreach(Path path in paths)
				{
					int index = listListPaths.FindIndex(l => l.All(p => p.HasCommonNets(path)));
					if(index != -1)
					{
						listListPaths[index].Add(path);
					}
					else
					{
						listListPaths.Add(new List<Path>());
						listListPaths[listListPaths.Count - 1].Add(path);
					}
				}

				if(listListPaths.Count == 1)
				{
					return GetExpressionOfPaths(listListPaths[0]);
				}
				else
				{
					Division result = new Division
					{
						Numerator = 1,
						Denominator = new Sum()
					};
					foreach(List<Path> lp in listListPaths)
					{
						Division div = new Division
						{
							Numerator = 1,
							Denominator = GetExpressionOfPaths(lp)
						};
						(result.Denominator as Sum).AddSummand(div);
					}
					return result;
				}
			}
			else
			{
				Net lastCommonNet = paths[0].Start;
				Sum result = new Sum();
				foreach(Net commonNet in commonNets)
				{
					List<Path> stepPaths = new List<Path>();
					foreach(Path p in paths)
					{
						Path subPath = p.SubPath(lastCommonNet, commonNet);
						if(!stepPaths.Contains(subPath))
						{
							stepPaths.Add(subPath);
						}
					}
					result.AddSummand(GetExpressionOfPaths(stepPaths));
					lastCommonNet = commonNet;
				}
				return result;
			}
		}

		public List<Net> GetCommonNets(List<Path> paths)
		{
			// Find all nets that are common to all paths
			if(!paths.All(p => p.Start == paths[0].Start && p.End == paths[0].End)) return null;
			List<Net> commonNets = new List<Net> { paths[0].Start };
			foreach(Net intermediateNet in paths[0].GetIntermediateNets())
			{
				if(paths.All(p => p.ContainsNet(intermediateNet)))
				{
					commonNets.Add(intermediateNet);
				}
			}
			commonNets.Add(paths[0].End);
			return commonNets;
		}

		public Expression GetTransferFunction(Net netA1, Net netA2, Net netB1, Net netB2)
		{
			if(netA1 == null || netA2 == null || netB1 == null || netB2 == null) return null;
			List<Path> inputPaths = FindPaths(netA1, netA2);
			List<Path> endPaths = inputPaths.Where(p => p.ContainsNet(netB1) && p.ContainsNet(netB2)).ToList();
			List<Path> unusedPaths = inputPaths.Where(p => !p.Components.Any(c => endPaths.Any(p2 => p2.Components.Contains(c)))).ToList();
			//List<Path> directEndPaths = FindPaths(netB1, netB2).Where(p => !p.ContainsNet(netA1) && !p.ContainsNet(netA2)).ToList();
			if(ContainsBridge(inputPaths))
			{
				Debug.Assert(false, "Paths contain Bridge!");
				return null;
			}
			inputPaths = inputPaths.Where(p => !unusedPaths.Contains(p)).ToList();

			Path firstPath = endPaths[0];
			{
				List<Net> tempInternNets = firstPath.GetAllNets();
				if(tempInternNets.IndexOf(netB1) > tempInternNets.IndexOf(netB2))
				{
					endPaths.ForEach(p => p.Components.Reverse());
				}
			}

			if(inputPaths.Count == endPaths.Count())
			{
				return new Division(
					GetExpressionOfPaths(FindPaths(netB1, netB2, GetComponentsInPaths(unusedPaths))),
					GetExpressionOfPaths(FindPaths(netA1, netA2, GetComponentsInPaths(unusedPaths)))
				);
			}

			List<Net> commonNets = GetCommonNets(endPaths);
			int upperNet = commonNets.IndexOf(netB1);
			int lowerNet = commonNets.IndexOf(netB2);

			Expression result = 1;
			while(true)
			{
				while(upperNet > 0 && !inputPaths.Any(p => !endPaths.Contains(p) && p.ContainsNet(commonNets[upperNet])))
				{// TODO: handle edge cases?
					upperNet--;
				}
				while(lowerNet < commonNets.Count - 1 && !inputPaths.Any(p => !endPaths.Contains(p) && p.ContainsNet(commonNets[lowerNet])))
				{// TODO: handle edge cases?
					lowerNet++;
				}

				Net upper = commonNets[upperNet];
				Net lower = commonNets[lowerNet];

				result = result * GetTransferFunction(upper, lower, netB1, netB2);
				if(upper == netA1 && lower == netA2) return result;

				endPaths.AddRange(inputPaths.Where(p => !endPaths.Contains(p) && p.ContainsNet(upper) && p.ContainsNet(lower)));
				commonNets = GetCommonNets(endPaths);
				netB1 = upper;
				netB2 = lower;
				upperNet = commonNets.IndexOf(upper);
				lowerNet = commonNets.IndexOf(lower);
			}

			/*
			Recursive:
			while(upperNet > 0 && !inputPaths.Any(p => !endPaths.Contains(p) && p.ContainsNet(commonNets[upperNet])))
			{// TODO: handle edge cases?
				upperNet--;
			}
			while(lowerNet < commonNets.Count-1 && !inputPaths.Any(p => !endPaths.Contains(p) && p.ContainsNet(commonNets[lowerNet])))
			{// TODO: handle edge cases?
				lowerNet++;
			}
			
			Net upper = commonNets[upperNet];
			Net lower = commonNets[lowerNet];
			
			return GetTransferFunction(upper, lower, netB1, netB2) * GetTransferFunction(netA1, netA2, upper, lower);
			*/
		}

		public bool ContainsBridge(List<Path> listPaths)
		{
			// if any path conatins a component which is in another path in the other direction
			if(listPaths.Any(p1 => listPaths.Any(p2 => p2 != p1 && p2.Components.Any(c => p1.Components.Contains(c) && p2.GetNet1(c) != p1.GetNet1(c)))))
				return true;
			else
				return false;
		}

		private void BtnTest_Click(object sender, RoutedEventArgs e)
		{
			if(cbxNet1.SelectedItem == null || cbxNet2.SelectedItem == null)
				return;
			if(cbxNet1.SelectedItem == cbxNet2.SelectedItem)
			{
				tbxResult.Text = "0";
				return;
			}

			List<Path> paths = FindPaths(cbxNet1.SelectedItem as Net, cbxNet2.SelectedItem as Net);
			Expression exp = GetExpressionOfPaths(paths);
			exp = exp.ToCommonDenominator();
			exp = exp.ToStandardForm();
			tbxResult.Text = exp.Evaluate();

			Expression transferFunction = GetTransferFunction
			(
				allNets.First(n => n.Name == "$0"),
				allNets.First(n => n.Name == "$3"),
				allNets.First(n => n.Name == "$2"),
				allNets.First(n => n.Name == "$3")
			);
			if(transferFunction != null)
				MessageBox.Show(transferFunction.ToCommonDenominator().ToStandardForm().Evaluate(), "Result:");
		}

		public void DrawAll()
		{
			foreach(Component comp in allComponents)
			{
				comp.Draw();
			}
		}

		private void BtnResistor_Click(object sender, RoutedEventArgs e)
		{
			if(ComponentToAdd == ComponentType.Resistor)
			{
				SetIndicator(ComponentType.None);
			}
			else
			{
				SetIndicator(ComponentType.Resistor);
			}
		}

		private void BtnCapacitor_Click(object sender, RoutedEventArgs e)
		{
			if(ComponentToAdd == ComponentType.Capacitor)
			{
				SetIndicator(ComponentType.None);
			}
			else
			{
				SetIndicator(ComponentType.Capacitor);
			}
		}

		private void BtnInductor_Click(object sender, RoutedEventArgs e)
		{
			if(ComponentToAdd == ComponentType.Inductor)
			{
				SetIndicator(ComponentType.None);
			}
			else
			{
				SetIndicator(ComponentType.Inductor);
			}
		}

		public void SetIndicator(ComponentType type)
		{
			RectResistor.Fill = Brushes.Transparent;
			RectCapacitor.Fill = Brushes.Transparent;
			RectInductor.Fill = Brushes.Transparent;
			switch(type)
			{
				case ComponentType.Resistor:
					RectResistor.Fill = Brushes.Lime;
					ComponentToAdd = ComponentType.Resistor;
					break;
				case ComponentType.Capacitor:
					RectCapacitor.Fill = Brushes.Lime;
					ComponentToAdd = ComponentType.Capacitor;
					break;
				case ComponentType.Inductor:
					RectInductor.Fill = Brushes.Lime;
					ComponentToAdd = ComponentType.Inductor;
					break;
				case ComponentType.None:
					ComponentToAdd = ComponentType.None;
					break;
			}
		}

		private void BtnRotateLeft_Click(object sender, RoutedEventArgs e)
		{
			AddComponentRotation = (ComponentRotation)(((int)AddComponentRotation + 3) % 4);
			ComponentRotationIndicatorTransform.Angle = (ComponentRotationIndicatorTransform.Angle + 270) % 360;
		}

		private void BtnRotateRight_Click(object sender, RoutedEventArgs e)
		{
			AddComponentRotation = (ComponentRotation)(((int)AddComponentRotation + 1) % 4);
			ComponentRotationIndicatorTransform.Angle = (ComponentRotationIndicatorTransform.Angle + 90) % 360;
		}

		private void PlaceComponent(object sender, MouseButtonEventArgs e)
		{
			if(!Net.wireAttached && !clickedComponent) //?
			{
				e.Handled = true;
				Point mouse = Mouse.GetPosition(canvas);
				Component newComponent = null;
				switch(ComponentToAdd)
				{
					case ComponentType.Resistor:
						newComponent = new Resistor(mouse.X, mouse.Y);
						break;
					case ComponentType.Capacitor:
						newComponent = new Capacitor(mouse.X, mouse.Y);
						break;
					case ComponentType.Inductor:
						newComponent = new Inductor(mouse.X, mouse.Y);
						break;
					case ComponentType.None:
						break;
				}
				if(newComponent != null)
				{
					newComponent.Rotation = AddComponentRotation;
					newComponent.Draw();
					//Keyboard.Focus(newComponent.VisualGroup);	// Doesn't work
				}
			}
		}

		public void ReplaceComponent(Component oldComponent, Component newComponent)
		{
			if(oldComponent == null || newComponent == null) return;
			if(oldComponent.NetA == oldComponent.NetB)
			{
				oldComponent.NetA?.ReplaceComponent(oldComponent, newComponent);
			}
			else
			{
				oldComponent.NetA?.ReplaceComponent(oldComponent, newComponent);
				oldComponent.NetB?.ReplaceComponent(oldComponent, newComponent);
			}
			allComponents.Remove(oldComponent);
			allComponents.Add(newComponent);
			canvas.Children.Remove(oldComponent.VisualGroup);
			canvas.Children.Remove(oldComponent.PortA);
			canvas.Children.Remove(oldComponent.PortB);
			Component.baseCanvasMaxZIndex--;
			newComponent.Draw();
		}

		private void Save_Executed(object sender, RoutedEventArgs e)
		{
			Net.CancelCurrentLine();
			string setting = "";
			foreach(Net n in allNets)
			{
				setting += n.ExportNet();
				setting += "\n";
			}
			foreach(Component c in allComponents)
			{
				setting += c.ExportComponent();
				setting += "\n";
			}
			tbxResult.Text = setting;
			File.WriteAllText("Settings.txt", setting);
		}

		private void Open_Executed(object sender, RoutedEventArgs e)
		{
			Clear_Executed();
			string setting = File.ReadAllText("Settings.txt").Replace("\r", "");
			int i = 0;
			while(i < setting.Length)
			{
				int endIndex = setting.IndexOf('\n', i);
				if(endIndex == -1) endIndex = setting.Length - 1;
				if(endIndex < setting.Length - 2)
				{
					if(setting[endIndex + 1] == '{')
					{
						endIndex = setting.IndexOf('}', i);
					}
				}
				switch(setting[i])
				{
					case 'N':
						Net.ImportNet(setting.Substring(i, endIndex - i + 1));
						break;
					case 'R':
					case 'L':
					case 'C':
						Component comp = Component.ImportComponent(setting.Substring(i, endIndex - i + 1).Replace("\n", ""));
						comp.Draw();
						break;
				}
				i = endIndex + 1;
			}

		}

		private void Clear_Executed(object sender = null, RoutedEventArgs e = null)
		{
			allComponents.Clear();
			allNets.Clear();
			Net.wireDictionary.Clear();
			canvas.Children.Clear();

			Net.attachedLine = null;
			Net.attachedLineOrigin = null;
			Net.attachedNet = null;
			Net.wireAttached = false;
		}

		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.R && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
			{
				if(e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
				{
					BtnRotateLeft_Click(null, null);
				}
				else
				{
					BtnRotateRight_Click(null, null);
				}
			}
		}
	}

	public static class StringArithmetic
	{
		// C_ denotes a complex function that is aware of naming schemes and special symbols 

		public static bool C_ContainsTerm(string factor, string term) // ex.: "sL1L'C","L'"
		{
			if(C_GetComponentTerms(factor).Contains(term))
				return true;
			return false;
		}

		public static List<string> C_GetComponentTerms(string factor) // ex. "sL1L'C"
		{
			List<string> terms = new List<string>();
			string currentTerm = "";
			for(int i = 0; i < factor.Length; i++)
			{
				if(currentTerm != "")
				{
					if(factor[i] == 's' || factor[i] == 'R' || factor[i] == 'L' || factor[i] == 'C')
					{
						terms.Add(currentTerm);
					}
				}
				switch(factor[i])
				{
					case 's':
						currentTerm = "s";
						break;
					case 'R':
						currentTerm = "R";
						break;
					case 'L':
						currentTerm = "L";
						break;
					case 'C':
						currentTerm = "C";
						break;
					default:
						currentTerm += factor[i];
						break;
				}
			}
			return terms;
		}


		public static bool IsSum(string expression)
		{
			return GetSummands(expression).Count > 1;
		}
		public static bool IsProduct(string expression)
		{
			return GetFactors(expression).Count > 1;
		}
		public static bool IsFraction(string expression)
		{
			(string n, string d) = SplitFraction(expression);
			return !d.Equals("");
		}


		public static string Product(List<string> factors)
		{
			string product = "";
			foreach(string factor in factors)
			{
				if(factor.Equals("1")) continue;
				if(factor.Equals("0")) return "0";
				if(IsSum(factor))
					product += "(" + factor + ")";
			}
			return product;
		}

		public static string Product(string factor1, string factor2)
		{
			if(factor1.Equals("0") || factor2.Equals("0")) return "0";
			if(factor1.Equals("1")) return factor2;
			if(factor2.Equals("1")) return factor1;
			string result = "";
			if(IsSum(factor1))
				result += "(" + factor1 + ")";
			else
				result += factor1;
			if(IsSum(factor2))
				result += "(" + factor2 + ")";
			else
				result += factor2;
			return result;
		}

		public static List<string> GetSummands(string expression)
		{
			List<string> summands = new List<string>();
			int openBrackets = 0;
			int summandStartIndex = 0;
			for(int i = 0; i < expression.Length; i++)
			{
				if(expression[i] == '(')
					openBrackets++;
				if(expression[i] == ')')
					openBrackets--;
				if(openBrackets == 0)
				{
					if(expression[i] == '+')
					{
						summands.Add(expression.Substring(summandStartIndex, i - summandStartIndex));
						summandStartIndex = i + 1;
					}
					else if(i == expression.Length - 1)
					{
						summands.Add(expression.Substring(summandStartIndex));
					}
				}
			}
			return summands;
		}

		public static List<string> GetFactors(string expression)
		{
			List<string> factors = new List<string>();
			int openBrackets = 0;
			int factorStartIndex = 0;
			string factor = null;
			for(int i = 0; i < expression.Length; i++)
			{
				if(expression[i] == '(')
				{
					if(openBrackets == 0)
					{
						if(i > 0 && expression[i - 1] != ')' && expression[i - 1] != '/')
						{
							factor = expression.Substring(factorStartIndex, i - factorStartIndex);
						}
						factorStartIndex = i + 1;
					}
					openBrackets++;
				}
				else if(expression[i] == ')')
				{
					openBrackets--;
					if(openBrackets == 0)
					{
						if(factorStartIndex > 2 && expression[factorStartIndex - 2] == '/')
						{
							factor = "1/" + expression.Substring(factorStartIndex, i - factorStartIndex);
						}
						else
						{
							factor = expression.Substring(factorStartIndex, i - factorStartIndex);
						}
						if(i + 1 != expression.Length && expression[i + 1] != '(')
						{
							factorStartIndex = i + 1;
						}
					}
				}
				else if(openBrackets == 0)
				{
					if(expression[i] == '/' && expression[i - 1] != ')')
					{
						factor = expression.Substring(factorStartIndex, i - factorStartIndex);
						factorStartIndex = i + 1;
					}
					if(expression[i] == '+')
					{
						factors.Clear();
						factors.Add(TrimDown(expression));
						return factors;
					}
					else if(i == expression.Length - 1)
					{
						if(expression[factorStartIndex] == '/') // ind-1?
							factor = "1/" + expression.Substring(factorStartIndex + 1);
						else
							factor = expression.Substring(factorStartIndex);
					}
					//factorStartIndex = i;
				}
				if(factor != null && !factor.Equals("1"))
				{
					factors.Add(TrimDown(factor));
					factor = null;
				}
			}
			return factors;
		}

		public static string GetDenominator(string expression)
		{
			(string num, string den) = SplitFraction(expression);
			return den;
		}

		public static string GetNumerator(string expression)
		{
			(string num, string den) = SplitFraction(expression);
			return num;
		}

		public static (string, string) SplitFraction(string expression)
		{
			int openBrackets = 0;
			for(int i = 0; i < expression.Length; i++)
			{
				if(expression[i] == '(')
					openBrackets++;
				if(expression[i] == ')')
					openBrackets--;
				if(openBrackets == 0 && expression[i] == '/')
				{
					return (TrimDown(expression.Substring(0, i)),
							TrimDown(expression.Substring(i + 1))
							);
				}
			}
			return (expression, "");
		}

		public static string TrimDown(string expression)
		{
			string oldExpression = "";
			string newExpression = expression;
			while(oldExpression != newExpression)
			{
				oldExpression = newExpression;
				newExpression = TrimBrackets(newExpression);
			}
			return newExpression;
		}

		public static string TrimBrackets(string expression)
		{
			if(expression[0] != '(' || expression[expression.Length - 1] != ')')
				return expression;
			int openBrackets = 0;
			for(int i = 0; i < expression.Length; i++)
			{
				if(expression[i] == '(')
					openBrackets++;
				if(expression[i] == ')')
					openBrackets--;
				if(openBrackets == 0 && i != expression.Length - 1)
				{
					return expression;
				}
			}
			return openBrackets == 0 ? expression.Substring(1, expression.Length - 2) : expression;
		}

		public static List<string> GetFirstOrderDenominators(string expression)
		{
			List<string> denominators = new List<string>();
			List<string> summands = GetSummands(expression);
			foreach(string summand in summands)
			{
				if(IsFraction(summand))
				{
					denominators.Add(GetDenominator(summand));
				}
			}
			return denominators;
		}

		public static List<string> GetSecondOrderDenominators(string expression)
		{
			List<string> denominators = new List<string>();
			(string numerator, string denominator) = SplitFraction(expression);
			List<string> summands = GetSummands(numerator);
			for(int i = 0; i < summands.Count; i++)
			{
				List<string> den = GetFactors(GetDenominator(summands[i]));
				foreach(string factor in den)
				{
					if(!denominators.Contains(factor) && !factor.Equals(""))
						denominators.Add(TrimDown(factor));
				}
			}
			summands = GetSummands(denominator);
			for(int i = 0; i < summands.Count; i++)
			{
				List<string> den = GetFactors(GetDenominator(summands[i]));
				foreach(string factor in den)
				{
					if(!denominators.Contains(factor) && !factor.Equals(""))
						denominators.Add(TrimDown(factor));
				}
			}
			return denominators;
		}

		public static string ToOneDenominator(string expression)
		{
			List<string> summands = GetSummands(expression);
			if(summands.Count < 2) return expression;
			List<string> denominators = GetFirstOrderDenominators(expression);
			string denominator = "";
			string numerator = "";
			denominator = Product(denominators);
			foreach(string summand in summands)
			{
				(string summandNum, string summandDen) = SplitFraction(summand);
				numerator += "+" + summandNum;
				List<string> denFactors = GetFactors(summandDen);
				foreach(string factor in denominators)
				{
					if(!summandDen.Equals(factor))
					{
						if(IsSum(factor))
							numerator += "(" + factor + ")";
						else
							numerator += factor;
					}
				}
			}
			numerator = numerator.Substring(1);
			return "(" + numerator + ")/(" + denominator + ")";
		}


		public static string MultiplyExpression(string expression, string factor)
		{
			(string numerator, string denominator) = SplitFraction(expression);

			if(denominator.Equals(TrimDown(factor)))
			{
				return numerator;
			}
			string result = "";

			if(numerator.Equals(1))
			{
				result += factor;
			}
			else
			{
				result += "(" + factor + ")(" + numerator + ")";
			}
			if(!denominator.Equals(""))
			{
				result += "/(" + denominator + ")";
			}
			return result;
		}

		public static string MultiplySums(string factor1, string factor2)
		{
			string result = "";
			List<string> expSummands = GetSummands(factor1);
			List<string> facSummands = GetSummands(factor2);
			for(int i = 0; i < expSummands.Count; i++)
			{
				for(int j = 0; j < facSummands.Count; j++)
				{
					result += expSummands[i] + facSummands[j] + "+";
				}
			}
			result = result.Remove(result.Length - 1, 1);
			return result;
		}

		public static string Simplify(string expression)
		{
			if(!expression.Contains('/'))
				return expression;
			//string pattern = @"";
			return "";
		}
	}

	public class Path
	{
		public List<Component> Components { get; }

		public Net Start { get; set; }
		public Net End { get; set; }

		public Path()
		{
			Components = new List<Component>();
		}

		public Path Add(Component component)
		{
			Components.Add(component);
			return this;
		}

		public Path Copy()
		{
			Path copy = new Path
			{
				Start = Start,
				End = End
			};
			foreach(Component component in Components)
			{
				copy.Add(component);
			}
			return copy;
		}

		public bool ContainsNet(Net net)
		{
			if(Components.Count < 2)
				return net == Start || net == End;
			return Components.Any(c => c.IsConnected(net));


			//Component previous = Components[0];
			//List<Component>.Enumerator enumerator = Components.GetEnumerator();
			//enumerator.MoveNext();
			//do
			//{
			//	if(enumerator.Current.IsConnected(net))
			//	{
			//		return true;
			//	}
			//} while(enumerator.MoveNext());
			//return false;
		}

		public Net GetNet1(Component comp)
		{
			if(!Components.Contains(comp)) return null;
			return GetAllNets().Find(n => comp.IsConnected(n));
		}

		public Net GetNet2(Component comp)
		{
			if(!Components.Contains(comp)) return null;
			return GetAllNets().FindLast(n => comp.IsConnected(n));
		}

		public List<Net> GetIntermediateNets()
		{
			List<Net> nets = new List<Net>();
			Net previousNet = Start;
			foreach(Component component in Components)
			{
				if(component.OtherNet(previousNet) == End)
				{
					return nets;
				}
				nets.Add(component.OtherNet(previousNet));
				previousNet = component.OtherNet(previousNet);
			}
			Debug.Assert(false);
			return nets;    // Shouldn't happen
		}

		public List<Net> GetAllNets()
		{
			List<Net> nets = new List<Net> { Start };
			Net previousNet = Start;
			foreach(Component component in Components)
			{
				nets.Add(component.OtherNet(previousNet));
				previousNet = component.OtherNet(previousNet);
			}
			return nets;
		}

		public string GetImpedance()
		{
			string impedance = "";
			foreach(Component component in Components)
			{
				impedance += component.GetValueStr() + "+";
			}
			if(impedance.Length > 0)
				impedance = impedance.Remove(impedance.Length - 1, 1);
			return impedance;
		}

		public Expression GetExpression()
		{
			Sum result = new Sum();
			foreach(Component component in Components)
			{
				result.AddSummand(component.GetExpression());
			}
			return result;
		}

		public Path SubPath(Net start, Net end)
		{
			Path newPath = new Path();
			int startIndex = Components.FindLastIndex(c => c.IsConnected(start));
			int endIndex = Components.FindIndex(c => c.IsConnected(end));
			newPath.Components.AddRange(Components.GetRange(startIndex, endIndex - startIndex + 1));
			newPath.Start = start;
			newPath.End = end;
			return newPath;
		}

		public Net GetPreviousNet(Net net)
		{
			if(!ContainsNet(net))
			{
				return null;
			}
			Component comp = Components.Find(c => c.IsConnected(net));
			return comp.OtherNet(net);
		}

		public Net GetNextNet(Net net)
		{
			if(!ContainsNet(net))
			{
				return null;
			}
			Component comp = Components.FindLast(c => c.IsConnected(net));
			return comp.OtherNet(net);
		}

		public bool HasCommonNets(Path otherPath)
		{
			List<Net> nets = GetIntermediateNets();
			List<Net> otherNets = otherPath.GetIntermediateNets();
			return nets.Any(n => otherNets.Contains(n));
		}

		public override bool Equals(object otherPath)
		{
			if(!(otherPath is Path))
				return false;
			if(Components.Count != (otherPath as Path).Components.Count)
				return false;
			for(int i = 0; i < Components.Count; i++)
			{
				if(Components[i] != (otherPath as Path).Components[i])
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()   // To get rid of compiler warning
		{
			return base.GetHashCode();
		}
	}

	public enum LineEndPoint { Point1, Point2 };

	public class Branchpoint
	{
		public Net net;

		public Dictionary<Line, LineEndPoint> wires;

		public Ellipse connectionMarker;

		private double x;
		public double X
		{
			get
			{
				return x;
			}
			set
			{
				Canvas.SetLeft(connectionMarker, value - 5);
				foreach(KeyValuePair<Line, LineEndPoint> line in wires)
				{
					if(line.Value == LineEndPoint.Point1)
					{
						line.Key.X1 = value;
					}
					else
					{
						line.Key.X2 = value;
					}
				}
				x = value;
			}
		}

		private double y;
		public double Y
		{
			get
			{
				return y;
			}
			set
			{
				Canvas.SetTop(connectionMarker, value - 5);
				foreach(KeyValuePair<Line, LineEndPoint> line in wires)
				{
					if(line.Value == LineEndPoint.Point1)
					{
						line.Key.Y1 = value;
					}
					else
					{
						line.Key.Y2 = value;
					}
				}
				y = value;
			}
		}

		public static Canvas baseCanvas;

		private static Branchpoint mousedownBranchpoint;
		private static bool branchpointMoving = false;

		public Branchpoint(Net net, double x, double y)
		{
			this.net = net;
			net.branchPoints.Add(this);
			wires = new Dictionary<Line, LineEndPoint>();
			connectionMarker = new Ellipse
			{
				Width = 10,
				Height = 10,
				Fill = Brushes.LimeGreen,
				SnapsToDevicePixels = true
			};
			connectionMarker.MouseLeftButtonDown += Branchpoint_MouseDown;
			connectionMarker.MouseLeftButtonUp += Branchpoint_MouseUp;
			connectionMarker.MouseMove += Branchpoint_MouseMove;
			connectionMarker.MouseLeave += Branchpoint_MouseLeave;
			connectionMarker.MouseRightButtonDown += Branchpoint_RightClick;
			X = x;
			Y = y;
			Panel.SetZIndex(connectionMarker, Component.baseCanvasMaxZIndex + 3);
			baseCanvas.Children.Add(connectionMarker);
		}

		public static Branchpoint ImportBranchpoint(string str, Net net)
		{
			string[] data = str.Split(';');
			Branchpoint bp;
			try
			{
				bp = new Branchpoint(net, double.Parse(data[1]), double.Parse(data[2]));
				string[] connectedWires = data[3].Split('.');
				foreach(string s in connectedWires)
				{
					string[] wire = s.Split(',');
					int wireIndex = Int32.Parse(wire[0]);
					LineEndPoint lep = (LineEndPoint)Int32.Parse(wire[1]);
					bp.wires[net.wires[wireIndex]] = lep;
				}
			}
			catch
			{
				bp = null;
			}
			return bp;
		}

		public string ExportBranchpoint()
		{
			string result = $"B;{X};{Y};";
			foreach(KeyValuePair<Line, LineEndPoint> kvp in wires)
			{
				result += $"{net.wires.IndexOf(kvp.Key)},{(int)kvp.Value}.";
			}
			return result.Substring(0, result.Length - 1);
		}

		public void Branchpoint_MouseDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			mousedownBranchpoint = this;
			connectionMarker.CaptureMouse();
		}

		public void Branchpoint_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if(mousedownBranchpoint != null)
			{
				connectionMarker.ReleaseMouseCapture();
				if(branchpointMoving)
				{
					branchpointMoving = false;
				}
				else
				{
					if(Net.wireAttached)
					{
						bool cancel = false;
						if(this == Net.attachedLineOrigin)      // Same Branchpoint clicked again
						{
							cancel = true;
						}
						else
						{
							foreach(Line line in wires.Keys)    // Don't add a wire twice
							{
								if(Net.attachedLineOrigin.wires.ContainsKey(line))
								{
									cancel = true;
								}
							}
						}
						if(cancel)
						{
							Net.CancelCurrentLine();
							return;
						}
					}
					net.Branchpoint_Connect(mousedownBranchpoint);
				}
				mousedownBranchpoint = null;
			}
		}

		public void Branchpoint_MouseLeave(object sender, MouseEventArgs e)
		{
			if(Mouse.LeftButton == MouseButtonState.Released)
			{
				if(mousedownBranchpoint == this)
				{
					mousedownBranchpoint = null;
				}
			}
			else
			{

			}
		}

		public void Branchpoint_MouseMove(object sender, MouseEventArgs e)
		{
			if(mousedownBranchpoint == this)
			{
				branchpointMoving = true;
				if(net.Components.Count(c => net.connections[c].Any(t => t.Item1 == this)) == 0)
				{
					Point mousePos = Mouse.GetPosition(baseCanvas);
					X = mousePos.X;
					Y = mousePos.Y;
				}
			}
		}

		public void Branchpoint_RightClick(object senderBp, MouseButtonEventArgs e)
		{
			string message = "";
			message += $"Net: {net.Name}\n";
			message += $"{wires.Count} ";
			message += (wires.Count == 1) ? "Wire" : "Wires";
			message += " attached\n";
			if(net.connections.Count(x => (x.Value.Any(t => t.Item1 == this))) > 0)
			{
				Component attachedComponent = net.Components.First(c => net.connections[c].Any(t => t.Item1 == this));
				message += $"Attached Component: {attachedComponent.Name}\n";
			}
			message += $"X: {X}, Y: {Y}";
			MessageBox.Show(message, "Branchpoint");
		}
	}

	public class Net
	{
		public string Name { get; set; }
		public static readonly double WireThickness = 4;
		public static readonly Brush WireColor = Brushes.Red;

		public List<Component> Components { get; }
		public List<Branchpoint> branchPoints;
		public List<Line> wires;                        //Necessary? Maybe highlight net
		public Dictionary<Component, List<(Branchpoint, NetPort)>> connections;
		public static Dictionary<Line, Net> wireDictionary = new Dictionary<Line, Net>();

		public static bool wireAttached = false;        // Currently drawing a connection?
		public static Line attachedLine;                // Current line
		public static Net attachedNet;                  // Current net attached to line
		public static Branchpoint attachedLineOrigin;   // Current line origin

		public static MainWindow mainWindow;
		public static Canvas baseCanvas;

		public Net() : this("")
		{
			int i = 0;
			while(true)
			{
				if(!MainWindow.allNets.Any(c => c.Name.Equals($"${i}")))
				{
					break;
				}
				i++;
			}
			Name = $"${i}";
		}

		public Net(string name)
		{
			Name = name;
			Components = new List<Component>();
			branchPoints = new List<Branchpoint>();
			wires = new List<Line>();
			connections = new Dictionary<Component, List<(Branchpoint, NetPort)>>();
			MainWindow.allNets.Add(this);
		}

		public static Net ImportNet(string str)
		{
			string[] net = str.Split(new[] { "\n", "{", "}" }, StringSplitOptions.RemoveEmptyEntries);
			if(!net[0].StartsWith("N;")) return null;
			Net result = new Net(net[0].Substring(2));
			for(int i = 1; i < net.Length; i++)
			{
				if(net[i].StartsWith("B;"))
				{
					Branchpoint bp = Branchpoint.ImportBranchpoint(net[i], result);
				}
				else if(net[i].StartsWith("W;"))
				{
					string[] lineData = net[i].Split(';');
					Line line = result.NewWire
					(
						double.Parse(lineData[1]),
						double.Parse(lineData[2]),
						double.Parse(lineData[3]),
						double.Parse(lineData[4])
					);
					line.Visibility = Visibility.Visible;
				}
			}
			return result;
		}

		public string ExportNet()
		{
			string result = "";
			result += $"N;{Name}\n{{\n";    // {{ is { in $""
			foreach(Line line in wires)
			{
				result += $"W;{line.X1};{line.Y1};{line.X2};{line.Y2}\n";
			}
			foreach(Branchpoint bp in branchPoints)
			{
				result += bp.ExportBranchpoint();
				result += "\n";
			}
			result += "}";
			return result;
		}

		public Component GetNextComponent(Component component)
		{
			if(Components.Count == 0)
			{
				return null;
			}
			if(component == null)
			{
				return Components.First();
			}
			if(Components.Contains(component))
			{
				int index = Components.IndexOf(component);
				if(index < Components.Count - 1)
				{
					return Components[index + 1];
				}
			}
			return null;
		}

		/*
		// Necessary?
		public void Connect(Net net, Component component)
		{
			Components.Add(component);
			net.Components.Add(component);
			component.NetA = this;
			component.NetB = net;
		}

		// Necessary?
		// Manually attach a wire to an existing Branchpoint
		public void Attach(Line line, Branchpoint branchPoint, LineEndPoint endPoint)
		{
			if(line == null)
			{
				if(wireAttached == false || attachedLine == null)
				{
					return;
				}
				line = attachedLine;
			}
			branchPoint.wires.Add(line, endPoint);
			branchPoint.net.Merge(attachedNet);
			if(endPoint == LineEndPoint.Point1)
			{
				line.X1 = branchPoint.X;
				line.Y1 = branchPoint.Y;
			}
			else
			{
				line.X2 = branchPoint.X;
				line.Y2 = branchPoint.Y;
			}
			line.Visibility = Visibility.Visible;
		}
		*/

		public Line NewWire(double x1, double y1, double x2, double y2)
		{
			Line newLine = new Line
			{
				X1 = x1,
				Y1 = y1,
				X2 = x2,
				Y2 = y2,
				StrokeThickness = WireThickness,
				Stroke = WireColor,
				Visibility = Visibility.Collapsed
			};
			wires.Add(newLine);
			wireDictionary.Add(newLine, this);
			newLine.MouseLeftButtonDown += (x, y) => wireDictionary[x as Line].Wire_Click(x as Line, y);
			newLine.MouseRightButtonDown += (x, y) => wireDictionary[x as Line].Wire_RightClick(x as Line, y);
			Panel.SetZIndex(newLine, Component.baseCanvasMaxZIndex + 1);
			baseCanvas.Children.Add(newLine);
			return newLine;
		}

		public void ReplaceComponent(Component oldComponent, Component newComponent)
		{
			if(oldComponent == null || newComponent == null) return;
			Components.Remove(oldComponent);
			Components.Add(newComponent);
			List<(Branchpoint, NetPort)> oldListBps = connections[oldComponent];
			connections.Remove(oldComponent);
			connections[newComponent] = oldListBps;
			// TODO: Update branchpoint positions?
		}

		public static void CancelCurrentLine()
		{
			if(wireAttached)
			{
				attachedNet.wires.Remove(attachedLine);
				attachedLineOrigin.wires.Remove(attachedLine);
				wireDictionary.Remove(attachedLine);
				attachedLine = null;
				attachedLineOrigin = null;
				attachedNet = null;
				wireAttached = false;
			}
		}

		public void Branchpoint_Connect(Branchpoint bp)
		{
			if(wireAttached)
			{
				wireAttached = false;
				if(Int32.Parse(Name.Substring(1)) > Int32.Parse(attachedNet.Name.Substring(1)))
				{
					attachedNet.Merge(this);
				}
				else
				{
					this.Merge(attachedNet);
				}
				bp.wires.Add(attachedLine, LineEndPoint.Point2);
				attachedLine.X2 = bp.X;
				attachedLine.Y2 = bp.Y;
				attachedLine.Visibility = Visibility.Visible;
				wireDictionary[attachedLine] = this;
			}
			else
			{
				attachedNet = this;
				Line newLine = attachedNet.NewWire(bp.X, bp.Y, bp.X, bp.Y);
				bp.wires.Add(newLine, LineEndPoint.Point1);
				if(!attachedNet.branchPoints.Contains(bp))
				{
					attachedNet.branchPoints.Add(bp);
				}
				attachedLine = newLine;
				attachedLineOrigin = bp;
				wireAttached = true;
			}
		}

		public void Move(Component component)
		{
			if(connections.ContainsKey(component))
			{
				foreach((Branchpoint, NetPort) bp in connections[component])
				{
					(bp.Item1.X, bp.Item1.Y) = component.GetPortCoordinates(bp.Item2);
				}
			}
		}

		public void Wire_Click(Line sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			Point clickPoint = Mouse.GetPosition(baseCanvas);
			double dx = (sender.X1 - sender.X2);
			double dy = (sender.Y1 - sender.Y2);
			double rel = (clickPoint - new Point(sender.X1, sender.Y1)).Length / Math.Sqrt(dx * dx + dy * dy);
			double point_x = sender.X1 + rel * (sender.X2 - sender.X1);
			double point_y = sender.Y1 + rel * (sender.Y2 - sender.Y1);

			Line splitLine = this.NewWire(point_x, point_y, sender.X2, sender.Y2);
			splitLine.Visibility = Visibility.Visible;
			sender.X2 = point_x;
			sender.Y2 = point_y;

			foreach(Branchpoint branchpoint in branchPoints.Where(b => b.wires.ContainsKey(sender)))
			{
				LineEndPoint lep = branchpoint.wires[sender];
				if(lep == LineEndPoint.Point2)
				{
					branchpoint.wires.Remove(sender);
					branchpoint.wires.Add(splitLine, LineEndPoint.Point2);
				}
			}
			Branchpoint newBranchPoint = new Branchpoint(this, point_x, point_y);
			newBranchPoint.wires.Add(splitLine, LineEndPoint.Point1);
			newBranchPoint.wires.Add(sender, LineEndPoint.Point2);

			this.Branchpoint_Connect(newBranchPoint);
		}

		public void Wire_RightClick(Line sender, MouseButtonEventArgs e)
		{
			string message = "";
			message += $"Net: {Name}\n";
			if(Components.Count > 0)
			{
				message += "Components: (";
				foreach(Component component in Components)
				{
					message += component.Name + ", ";
				}
				message = message.TrimEnd(',', ' ');
				message += ")\n";
			}
			else
			{
				message += "Empty net\n";
			}
			message += $"X1: {sender.X1} Y1: {sender.Y1}\n";
			message += $"X2: {sender.X2} Y2: {sender.Y2}\n";
			MessageBox.Show(message, "Net");
		}

		public static void NetPort_Click(Component newComponent, NetPort newPort, MouseButtonEventArgs e)
		{
			e.Handled = true;
			newComponent.HideNetPort(newPort);
			Net newNet;
			Branchpoint newBranchPoint;
			(double point_x, double point_y) = newComponent.GetPortCoordinates(newPort);
			if(wireAttached)
			{
				newNet = attachedNet;
				newComponent.SetNet(newPort, newNet);
				if(!newNet.Components.Contains(newComponent))
				{
					newNet.Components.Add(newComponent);
				}
				newBranchPoint = new Branchpoint(newNet, point_x, point_y);
				if(!newNet.connections.ContainsKey(newComponent))
				{
					newNet.connections.Add(newComponent, new List<(Branchpoint, NetPort)>());
				}
				newNet.connections[newComponent].Add((newBranchPoint, newPort));
			}
			else
			{
				newNet = new Net();
				newComponent.SetNet(newPort, newNet);
				newBranchPoint = new Branchpoint(newNet, point_x, point_y);
				newNet.Components.Add(newComponent);
				if(!newNet.connections.ContainsKey(newComponent))
				{
					newNet.connections.Add(newComponent, new List<(Branchpoint, NetPort)>());
				}
				newNet.connections[newComponent].Add((newBranchPoint, newPort));
			}
			newNet.Branchpoint_Connect(newBranchPoint);
		}

		public void Merge(Net net)
		{
			if(this == net) return;
			foreach(Component component in net.Components)
			{
				component.SetNet(net, this);
			}
			net.branchPoints.ForEach((b) => b.net = this);
			branchPoints.AddRange(net.branchPoints);
			Components.AddRange(net.Components);
			foreach(KeyValuePair<Component, List<(Branchpoint, NetPort)>> kvp in net.connections)
			{
				if(!connections.ContainsKey(kvp.Key))
				{
					connections.Add(kvp.Key, new List<(Branchpoint, NetPort)>());
				}
				connections[kvp.Key].AddRange(kvp.Value);
			}
			foreach(Line line in net.wires)
			{
				wireDictionary[line] = this;
			}
			wires.AddRange(net.wires);
			MainWindow.allNets.Remove(net);
		}

		public static void Canvas_LeftMouseDown(object sender, MouseButtonEventArgs e)
		{
			if(!MainWindow.clickedComponent && wireAttached)
			{
				Point clickPoint = Mouse.GetPosition(baseCanvas);
				attachedLine.X2 = clickPoint.X;
				attachedLine.Y2 = clickPoint.Y;
				attachedLine.Visibility = Visibility.Visible;

				Line newLine = attachedNet.NewWire(attachedLine.X2, attachedLine.Y2, clickPoint.X, clickPoint.Y);
				Branchpoint newBranchPoint = new Branchpoint(attachedNet, clickPoint.X, clickPoint.Y);
				newBranchPoint.wires.Add(attachedLine, LineEndPoint.Point2);
				newBranchPoint.wires.Add(newLine, LineEndPoint.Point1);
				attachedLine = newLine;
				attachedLineOrigin = newBranchPoint;

			}
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public enum NetPort { A, B };
	public enum ComponentRotation { H1, V1, H2, V2 };   // Horizontal & vertical orientations
	public abstract class ComponentMargins  // Distance to center/origin of component
	{
		public static double Resistor_PortA_X = -44;
		public static double Resistor_PortA_Y = +0;
		public static double Resistor_PortB_X = +44;
		public static double Resistor_PortB_Y = +0;

		public static double Inductor_PortA_X = -44;
		public static double Inductor_PortA_Y = +0;
		public static double Inductor_PortB_X = +44;
		public static double Inductor_PortB_Y = +0;

		public static double Capacitor_PortA_X = -25;
		public static double Capacitor_PortA_Y = +0;
		public static double Capacitor_PortB_X = +25;
		public static double Capacitor_PortB_Y = +0;
	};

	public abstract class Component
	{
		private string name;
		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
				if(nameText != null)
				{
					nameText.Text = name;
				}
			}
		}

		protected double x;
		public double X
		{
			get
			{
				return x;
			}
			set
			{
				x = value;
				Canvas.SetLeft(VisualGroup, x - VisualGroup.Width / 2);
				Canvas.SetLeft(PortA, x + PortA_MarginX - ConnectionPort.Radius);
				Canvas.SetLeft(PortB, x + PortB_MarginX - ConnectionPort.Radius);
				NetA?.Move(this);
				NetB?.Move(this);
			}
		}

		protected double y;
		public double Y
		{
			get
			{
				return y;
			}
			set
			{
				y = value;
				Canvas.SetTop(VisualGroup, y - VisualGroup.Height / 2);
				Canvas.SetTop(PortA, y + PortA_MarginY - ConnectionPort.Radius);
				Canvas.SetTop(PortB, y + PortB_MarginY - ConnectionPort.Radius);
				NetA?.Move(this);
				NetB?.Move(this);
			}
		}

		public static double WireThickness = 3;
		public static Brush LeadColor = Brushes.Black;
		public ComponentRotation Rotation;

		public abstract double PortA_MarginX { get; }   // Update for Rotation in Childs
		public abstract double PortA_MarginY { get; }   //
		public abstract double PortB_MarginX { get; }   //
		public abstract double PortB_MarginY { get; }   //

		private static Component movedComponent;
		private static double moveStartX;       // Start position of movedComponent
		private static double moveStartY;       //
		private static double mousedown_x = -1;
		private static double mousedown_y = -1;

		public static MainWindow mainWindow;
		public static Canvas baseCanvas;
		public static int baseCanvasMaxZIndex = 0;

		public BorderedCanvas VisualGroup;
		protected TextBlock nameText;

		public ConnectionPort PortA { get; protected set; }
		public ConnectionPort PortB { get; protected set; }
		public Net NetA { get; set; }
		public Net NetB { get; set; }
		//public Impedance Impedance { get; }

		protected Component(string name)
		{
			Name = name;
			VisualGroup = new BorderedCanvas
			{
				Width = 94,
				Height = 30,
				Focusable = true,
				IsEnabled = true,
				SnapsToDevicePixels = true
			};
			VisualGroup.MouseLeftButtonDown += (x, y) => Symbol_MouseLeftDown(this, y);
			VisualGroup.MouseRightButtonDown += (x, y) => Symbol_MouseRightDown(this, y);
			VisualGroup.KeyDown += (x, y) => Symbol_KeyDown(this, y);
			VisualGroup.MouseDoubleClick += (x, y) => Symbol_MouseDoubleClick(this, y);
			Panel.SetZIndex(VisualGroup, 0);
			PortA = new ConnectionPort();
			PortB = new ConnectionPort();
			PortA.MouseLeftButtonDown += (x, y) => Net.NetPort_Click(this, NetPort.A, y);
			PortB.MouseLeftButtonDown += (x, y) => Net.NetPort_Click(this, NetPort.B, y);
			MainWindow.allComponents?.Add(this);
		}

		protected Component(string name, double x, double y) : this(name)
		{
			X = x;
			Y = y;
		}

		public static Component ImportComponent(string str)
		{
			try
			{
				Component result;
				string[] data = str.Split(';');
				switch(data[0][0])
				{
					case 'R':
					{
						result = new Resistor
						(
							data[1],
							double.Parse(data[2]),
							double.Parse(data[3]),
							double.Parse(data[4])
						);
						break;
					}
					case 'C':
					{
						result = new Capacitor
						(
							data[1],
							double.Parse(data[2]),
							double.Parse(data[3]),
							double.Parse(data[4])
						);
						break;
					}
					case 'L':
					{
						result = new Inductor
						(
							data[1],
							double.Parse(data[2]),
							double.Parse(data[3]),
							double.Parse(data[4])
						);
						break;
					}
					default:
					{
						return null;
					}
				}

				if(!Enum.TryParse(data[5], out ComponentRotation rotation))
				{
					rotation = ComponentRotation.H1;
				}
				result.Rotation = rotation;

				if(data[6] != "-1")
				{
					string[] netData = data[6].Split('.');
					Net net = MainWindow.allNets[Int32.Parse(netData[0])];
					result.NetA = net;
					net.Components.Add(result);
					if(!net.connections.ContainsKey(result))
					{
						net.connections[result] = new List<(Branchpoint, NetPort)>();
					}
					net.connections[result].Add((net.branchPoints[Int32.Parse(netData[1])], (NetPort)Int32.Parse(netData[2])));
					result.HideNetPort(NetPort.A);
				}
				if(data[7] != "-1")
				{
					string[] netData = data[7].Split('.');
					Net net = MainWindow.allNets[Int32.Parse(netData[0])];
					result.NetB = net;
					net.Components.Add(result);
					if(!net.connections.ContainsKey(result))    // this is false if and only if NetA==NetB
					{
						net.connections[result] = new List<(Branchpoint, NetPort)>();
					}
					net.connections[result].Add((net.branchPoints[Int32.Parse(netData[1])], (NetPort)Int32.Parse(netData[2])));
					result.HideNetPort(NetPort.B);
				}
				return result;
			}
			catch
			{
				return null;
			}
		}

		public virtual string ExportComponent()
		{
			string result = $";{X};{Y};{Rotation}";
			result += $";{MainWindow.allNets.IndexOf(NetA)}";
			if(NetA != null)
			{
				foreach((Branchpoint, NetPort) bp in NetA.connections[this])
				{
					result += $".{NetA.branchPoints.IndexOf(bp.Item1)}.{(int)bp.Item2}";
				}
			}
			result += $";{MainWindow.allNets.IndexOf(NetB)}";
			if(NetB != null)
			{
				foreach((Branchpoint, NetPort) bp in NetB.connections[this])
				{
					result += $".{NetB.branchPoints.IndexOf(bp.Item1)}.{(int)bp.Item2}";
				}
			}
			return result;
		}

		public abstract string GetValueStr();

		public bool IsConnected(Net net)
		{
			return net == NetA || net == NetB;
		}

		public Net OtherNet(Net net)
		{
			return net == NetA ? NetB : NetA;
		}

		public void Connect(Net A, Net B)
		{
			A.Components.Add(this);
			B.Components.Add(this);
			NetA = A;
			NetB = B;
		}

		public bool IsUseless()
		{
			if(NetA == null || NetB == null || NetA == NetB)
			{
				return true;
			}
			return false;
		}

		public abstract void Draw();

		public abstract void DrawRotation();
		public virtual void RotateLeft()
		{
			Rotation = (ComponentRotation)(((int)Rotation + 1) % 4);
			DrawRotation();
		}
		public virtual void RotateRight()
		{
			Rotation = (ComponentRotation)(((int)Rotation - 1) % 4);
			DrawRotation();
		}

		public abstract void ShowInfo();

		public static void UpdateBaseCanvas()
		{
			//baseCanvasMaxZIndex = baseCanvas.Children.OfType<Canvas>().Count();
			foreach(UIElement elem in baseCanvas.Children)
			{
				if(elem is Line)
				{
					Panel.SetZIndex(elem, baseCanvasMaxZIndex + 1);
				}
				else if(elem is ConnectionPort)
				{
					Panel.SetZIndex(elem, baseCanvasMaxZIndex + 2);
				}
				else if(elem is Ellipse)    // Branchpoint
				{
					Panel.SetZIndex(elem, baseCanvasMaxZIndex + 3);
				}
			}
		}

		public void HideNetPort(NetPort port)
		{
			if(port == NetPort.A)
			{
				PortA.Visibility = Visibility.Collapsed;
			}
			else
			{
				PortB.Visibility = Visibility.Collapsed;
			}
		}

		public void ShowNetPort(NetPort port)
		{
			if(port == NetPort.A)
			{
				PortA.Visibility = Visibility.Visible;
			}
			else
			{
				PortB.Visibility = Visibility.Visible;
			}
		}

		protected static void Symbol_MouseLeftDown(Component sender, MouseButtonEventArgs e)
		{
			//Keyboard.Focus(sender.VisualGroup.grid);
			sender.VisualGroup.KeyboardFocus();
			//e.Handled = true;
			MainWindow.clickedComponent = true;
			int oldZIndex = Panel.GetZIndex(sender.VisualGroup);
			foreach(BorderedCanvas childCanvas in baseCanvas.Children.OfType<BorderedCanvas>())
			{
				int currentZIndex = Panel.GetZIndex(childCanvas);
				if(currentZIndex > oldZIndex)
				{
					Panel.SetZIndex(childCanvas, currentZIndex - 1);
				}
			}
			Panel.SetZIndex(sender.VisualGroup, baseCanvasMaxZIndex);
			sender.VisualGroup.CaptureMouse();
			movedComponent = sender;
			Point mousePos = Mouse.GetPosition(baseCanvas);
			mousedown_x = mousePos.X;
			mousedown_y = mousePos.Y;
			moveStartX = sender.X;
			moveStartY = sender.Y;
		}

		public static void Symbol_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if(movedComponent != null)
			{
				movedComponent.VisualGroup.ReleaseMouseCapture();
				movedComponent = null;
				mousedown_x = -1;
				mousedown_y = -1;
			}
		}

		public static void Symbol_MouseMove(object sender, MouseEventArgs e)
		{
			if(Mouse.LeftButton.HasFlag(MouseButtonState.Pressed) && movedComponent != null && MainWindow.clickedComponent)
			{
				Point newMousePos = Mouse.GetPosition(baseCanvas);
				double mouseStartDiffX = newMousePos.X - mousedown_x;
				double mouseStartDiffY = newMousePos.Y - mousedown_y;
				double mouseStepDeltaX = moveStartX + mouseStartDiffX - movedComponent.X;
				double mouseStepDeltaY = moveStartY + mouseStartDiffY - movedComponent.Y;

				movedComponent.X = moveStartX + mouseStartDiffX;        // Move component
				movedComponent.Y = moveStartY + mouseStartDiffY;        //
			}
		}

		public static void Symbol_MouseRightDown(Component sender, MouseButtonEventArgs e)
		{
			sender.ShowInfo();
		}

		public static void Symbol_KeyDown(Component sender, KeyEventArgs e)
		{
			if(e.Key == Key.R && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
			{
				e.Handled = true;
				if(e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
				{
					sender.RotateRight();
				}
				else
				{
					sender.RotateLeft();
				}
			}
		}

		public virtual void Symbol_MouseDoubleClick(Component component, MouseButtonEventArgs e)
		{
			e.Handled = true;   // This is necessary so that after the dialog is closed,
								// the void Component.Symbol_MouseLeftDown doesn't get called
								// and the Component doesn't get focused when the baseCanvas is clicked
			MainWindow.clickedComponent = false;
			ComponentDialog dialog;
			if(component is Resistor)
				dialog = new ComponentDialog(Name, (component as Resistor).Resistance, MainWindow.ComponentType.Resistor);
			else if(component is Capacitor)
				dialog = new ComponentDialog(Name, (component as Capacitor).Capacitance, MainWindow.ComponentType.Capacitor);
			else // if(component is Inductor)
				dialog = new ComponentDialog(Name, (component as Inductor).Inductance, MainWindow.ComponentType.Inductor);
			dialog.Owner = mainWindow;
			if(dialog.ShowDialog() ?? false)
			{
				if(dialog.TypeModified)
				{
					Component newComponent;
					switch(dialog.ResultType)
					{
						default:
						case MainWindow.ComponentType.Resistor:
							newComponent = new Resistor(dialog.ResultName, component.X, component.Y);
							break;
						case MainWindow.ComponentType.Capacitor:
							newComponent = new Capacitor(dialog.ResultName, component.X, component.Y);
							break;
						case MainWindow.ComponentType.Inductor:
							newComponent = new Inductor(dialog.ResultName, component.X, component.Y);
							break;
					}
					newComponent.NetA = component.NetA;
					newComponent.NetB = component.NetB;
					newComponent.Rotation = component.Rotation;
					mainWindow.ReplaceComponent(component, newComponent);
				}
				else
				{
					component.Name = dialog.ResultName;
					dialog.ChangeComponentValue(component);
				}
			}
			else
			{
			}
		}

		public virtual (double X, double Y) GetPortCoordinates(NetPort port)
		{
			double x, y;
			if(port == NetPort.A)
			{
				x = X + PortA_MarginX;
				y = Y + PortA_MarginY;
			}
			else
			{
				x = X + PortB_MarginX;
				y = Y + PortB_MarginY;
			}
			return (x, y);
		}

		public Net GetNet(NetPort port)
		{
			return port == NetPort.A ? NetA : NetB;
		}

		public void SetNet(NetPort port, Net net)
		{
			if(port == NetPort.A)
			{
				NetA = net;
			}
			else
			{
				NetB = net;
			}
		}

		public void SetNet(Net oldNet, Net newNet)
		{
			if(NetA == oldNet)
			{
				NetA = newNet;
			}
			if(NetB == oldNet)
			{
				NetB = newNet;
			}
		}

		public NetPort GetPort(Net net) // Make sure net is part of component
		{
			if(net == NetA)
			{
				return NetPort.A;
			}
			else if(net == NetB)
			{
				return NetPort.B;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public abstract Impedance GetImpedance(double frequency);

		public abstract Expression GetExpression();
	}

	public class Resistor : Component
	{
		public double Resistance { get; set; }

		public override double PortA_MarginX
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Resistor_PortA_X;
					case ComponentRotation.H2:
						return ComponentMargins.Resistor_PortB_X;
					case ComponentRotation.V1:
						return ComponentMargins.Resistor_PortA_Y;
					case ComponentRotation.V2:
						return ComponentMargins.Resistor_PortB_Y;
					default:
						return ComponentMargins.Resistor_PortA_X;
				}
			}
		}
		public override double PortA_MarginY
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Resistor_PortA_Y;
					case ComponentRotation.H2:
						return ComponentMargins.Resistor_PortB_Y;
					case ComponentRotation.V1:
						return ComponentMargins.Resistor_PortA_X;
					case ComponentRotation.V2:
						return ComponentMargins.Resistor_PortB_X;
					default:
						return ComponentMargins.Resistor_PortA_Y;
				}
			}
		}
		public override double PortB_MarginX
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Resistor_PortB_X;
					case ComponentRotation.H2:
						return ComponentMargins.Resistor_PortA_X;
					case ComponentRotation.V1:
						return ComponentMargins.Resistor_PortB_Y;
					case ComponentRotation.V2:
						return ComponentMargins.Resistor_PortA_Y;
					default:
						return ComponentMargins.Resistor_PortB_X;
				}
			}
		}
		public override double PortB_MarginY
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Resistor_PortB_Y;
					case ComponentRotation.H2:
						return ComponentMargins.Resistor_PortA_Y;
					case ComponentRotation.V1:
						return ComponentMargins.Resistor_PortB_X;
					case ComponentRotation.V2:
						return ComponentMargins.Resistor_PortA_X;
					default:
						return ComponentMargins.Resistor_PortB_Y;
				}
			}
		}

		private Rectangle symbol;
		private Line leads;

		public Resistor(string name, double resistance = 0) : base(name)
		{
			Resistance = resistance;
		}
		public Resistor(string name, double x, double y) : base(name, x, y) { }
		public Resistor(string name, double resistance, double x, double y) : this(name, x, y)
		{
			Resistance = resistance;
		}
		public Resistor(double x, double y) : this("", x, y)
		{
			int i = 1;
			while(true)
			{
				if(!MainWindow.allComponents.Any(c => c.Name.Equals($"R{i}")))
				{
					break;
				}
				i++;
			}
			Name = $"R{i}";
		}
		public Resistor(double resistance, double x, double y) : this(x, y)
		{
			Resistance = resistance;
		}

		public override string ExportComponent()
		{
			return $"R;{Name};{Resistance}" + base.ExportComponent();
		}

		public override string GetValueStr()
		{
			return Name;
		}

		public override void Draw()
		{
			symbol = new Rectangle
			{
				Width = 50,
				Height = 20,
				Fill = Brushes.White,
				Stroke = Brushes.Black,
				StrokeThickness = 2,
				SnapsToDevicePixels = true
			};
			leads = new Line
			{
				X1 = 0,
				X2 = 90,
				Y1 = 0,
				Y2 = 0,
				Stroke = LeadColor,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};
			RenderOptions.SetEdgeMode(symbol, EdgeMode.Aliased);
			RenderOptions.SetEdgeMode(leads, EdgeMode.Aliased);

			nameText = new TextBlock
			{
				Text = Name
			};

			//Canvas.SetLeft(VisualGroup, X-VisualGroup.Width/2);
			//Canvas.SetTop(VisualGroup, Y-VisualGroup.Height/2);
			Canvas.SetLeft(symbol, 22);
			Canvas.SetTop(symbol, 5);
			Canvas.SetLeft(leads, 2);
			Canvas.SetTop(leads, 15);
			Canvas.SetLeft(nameText, VisualGroup.Width / 2 - 40);   // -20
			Canvas.SetTop(nameText, VisualGroup.Height / 2 - 15);   // -25
			Canvas.SetLeft(PortA, X + PortA_MarginX - ConnectionPort.Radius);
			Canvas.SetTop(PortA, Y + PortA_MarginY - ConnectionPort.Radius);
			Canvas.SetLeft(PortB, X + PortB_MarginX - ConnectionPort.Radius);
			Canvas.SetTop(PortB, Y + PortB_MarginY - ConnectionPort.Radius);
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(symbol);
			VisualGroup.Children.Add(nameText);
			baseCanvas.Children.Add(PortA);
			baseCanvas.Children.Add(PortB);
			baseCanvas.Children.Add(VisualGroup);
			Panel.SetZIndex(VisualGroup, baseCanvasMaxZIndex++);
			DrawRotation();
			UpdateBaseCanvas();
		}
		
		public override void DrawRotation()
		{
			if((int)Rotation % 2 == 0)  // Horizontal
			{
				VisualGroup.Width = 94;
				VisualGroup.Height = 30;
				symbol.Width = 50;
				symbol.Height = 20;
				leads.X1 = 0;
				leads.X2 = 90;
				leads.Y1 = 0;
				leads.Y2 = 0;
				Canvas.SetLeft(symbol, 22);
				Canvas.SetTop(symbol, 5);
				Canvas.SetLeft(leads, 2);
				Canvas.SetTop(leads, 15);
			}
			else
			{
				VisualGroup.Width = 30;
				VisualGroup.Height = 94;
				symbol.Width = 20;
				symbol.Height = 50;
				leads.X1 = 0;
				leads.X2 = 0;
				leads.Y1 = 0;
				leads.Y2 = 90;
				Canvas.SetLeft(symbol, 5);
				Canvas.SetTop(symbol, 22);
				Canvas.SetLeft(leads, 15);
				Canvas.SetTop(leads, 2);
			}
			Canvas.SetLeft(PortA, X + PortA_MarginX - ConnectionPort.Radius);
			Canvas.SetTop(PortA, Y + PortA_MarginY - ConnectionPort.Radius);
			Canvas.SetLeft(PortB, X + PortB_MarginX - ConnectionPort.Radius);
			Canvas.SetTop(PortB, Y + PortB_MarginY - ConnectionPort.Radius);
			X = x;
			Y = y;
		}

		public override void ShowInfo()
		{
			string message = "";
			message += $"Name: {Name}\n";
			message += $"Value: {Resistance} Ohm\n";
			message += $"NetA: {NetA?.Name ?? "null"}\n";
			message += $"NetB: {NetB?.Name ?? "null"}\n";
			message += $"X: {X}, Y: {Y}";
			MessageBox.Show(message, "Resistor");
		}

		public override string ToString()
		{
			return Name;
		}

		public override Impedance GetImpedance(double frequency)
		{
			return new Impedance(Resistance);
		}

		public override Expression GetExpression()
		{
			return new ComponentExpression(this);
		}
	}

	public class Inductor : Component
	{
		public double Inductance { get; set; }

		public override double PortA_MarginX
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Inductor_PortA_X;
					case ComponentRotation.H2:
						return ComponentMargins.Inductor_PortB_X;
					case ComponentRotation.V1:
						return ComponentMargins.Inductor_PortA_Y;
					case ComponentRotation.V2:
						return ComponentMargins.Inductor_PortB_Y;
					default:
						return ComponentMargins.Inductor_PortA_X;
				}
			}
		}
		public override double PortA_MarginY
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Inductor_PortA_Y;
					case ComponentRotation.H2:
						return ComponentMargins.Inductor_PortB_Y;
					case ComponentRotation.V1:
						return ComponentMargins.Inductor_PortA_X;
					case ComponentRotation.V2:
						return ComponentMargins.Inductor_PortB_X;
					default:
						return ComponentMargins.Inductor_PortA_Y;
				}
			}
		}
		public override double PortB_MarginX
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Inductor_PortB_X;
					case ComponentRotation.H2:
						return ComponentMargins.Inductor_PortA_X;
					case ComponentRotation.V1:
						return ComponentMargins.Inductor_PortB_Y;
					case ComponentRotation.V2:
						return ComponentMargins.Inductor_PortA_Y;
					default:
						return ComponentMargins.Inductor_PortB_X;
				}
			}
		}
		public override double PortB_MarginY
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Inductor_PortB_Y;
					case ComponentRotation.H2:
						return ComponentMargins.Inductor_PortA_Y;
					case ComponentRotation.V1:
						return ComponentMargins.Inductor_PortB_X;
					case ComponentRotation.V2:
						return ComponentMargins.Inductor_PortA_X;
					default:
						return ComponentMargins.Inductor_PortB_Y;
				}
			}
		}

		private Rectangle symbol;
		private Line leads;

		public Inductor(string name, double inductance = 0) : base(name)
		{
			Inductance = inductance;
		}
		public Inductor(string name, double x, double y) : base(name, x, y) { }
		public Inductor(string name, double inductance, double x, double y) : this(name, x, y)
		{
			Inductance = inductance;
		}
		public Inductor(double x, double y) : this("", x, y)
		{
			int i = 1;
			while(true)
			{
				if(!MainWindow.allComponents.Any(c => c.Name.Equals($"L{i}")))
				{
					break;
				}
				i++;
			}
			Name = $"L{i}";
		}
		public Inductor(double inductance, double x, double y) : this(x, y)
		{
			Inductance = inductance;
		}

		public override string ExportComponent()
		{
			return $"L;{Name};{Inductance}" + base.ExportComponent();
		}

		public override string GetValueStr()
		{
			return "s" + Name;
		}

		public override void Draw()
		{
			symbol = new Rectangle
			{
				Width = 50,
				Height = 20,
				Fill = Brushes.Black,
				Stroke = Brushes.Black,
				StrokeThickness = 2,
				SnapsToDevicePixels = true
			};
			leads = new Line
			{
				X1 = 0,
				X2 = 90,
				Y1 = 0,
				Y2 = 0,
				Stroke = LeadColor,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};
			RenderOptions.SetEdgeMode(symbol, EdgeMode.Aliased);
			RenderOptions.SetEdgeMode(leads, EdgeMode.Aliased);

			nameText = new TextBlock
			{
				Text = Name
			};

			//Canvas.SetLeft(VisualGroup, X-VisualGroup.Width/2);
			//Canvas.SetTop(VisualGroup, Y-VisualGroup.Height/2);
			Canvas.SetLeft(symbol, 22);
			Canvas.SetTop(symbol, 5);
			Canvas.SetLeft(leads, 2);
			Canvas.SetTop(leads, 15);
			Canvas.SetLeft(nameText, VisualGroup.Width / 2 - 20);
			Canvas.SetTop(nameText, VisualGroup.Height / 2 - 25);
			Canvas.SetLeft(PortA, X + ComponentMargins.Inductor_PortA_X - ConnectionPort.Radius);
			Canvas.SetTop(PortA, Y + ComponentMargins.Inductor_PortA_Y - ConnectionPort.Radius);
			Canvas.SetLeft(PortB, X + ComponentMargins.Inductor_PortB_X - ConnectionPort.Radius);
			Canvas.SetTop(PortB, Y + ComponentMargins.Inductor_PortB_Y - ConnectionPort.Radius);
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(symbol);
			VisualGroup.Children.Add(nameText);
			baseCanvas.Children.Add(PortA);
			baseCanvas.Children.Add(PortB);
			baseCanvas.Children.Add(VisualGroup);
			Panel.SetZIndex(VisualGroup, baseCanvasMaxZIndex++);
			DrawRotation();
			UpdateBaseCanvas();
		}

		public override void DrawRotation()
		{
			if((int)Rotation % 2 == 0)  // Horizontal
			{
				VisualGroup.Width = 94;
				VisualGroup.Height = 30;
				symbol.Width = 50;
				symbol.Height = 20;
				leads.X1 = 0;
				leads.X2 = 90;
				leads.Y1 = 0;
				leads.Y2 = 0;
				Canvas.SetLeft(symbol, 22);
				Canvas.SetTop(symbol, 5);
				Canvas.SetLeft(leads, 2);
				Canvas.SetTop(leads, 15);

			}
			else
			{
				VisualGroup.Width = 30;
				VisualGroup.Height = 94;
				symbol.Width = 20;
				symbol.Height = 50;
				leads.X1 = 0;
				leads.X2 = 0;
				leads.Y1 = 0;
				leads.Y2 = 90;
				Canvas.SetLeft(symbol, 5);
				Canvas.SetTop(symbol, 22);
				Canvas.SetLeft(leads, 15);
				Canvas.SetTop(leads, 2);
			}
			Canvas.SetLeft(PortA, X + PortA_MarginX - ConnectionPort.Radius);
			Canvas.SetTop(PortA, Y + PortA_MarginY - ConnectionPort.Radius);
			Canvas.SetLeft(PortB, X + PortB_MarginX - ConnectionPort.Radius);
			Canvas.SetTop(PortB, Y + PortB_MarginY - ConnectionPort.Radius);
			X = x;
			Y = y;
		}

		public override void ShowInfo()
		{
			string message = "";
			message += $"Name: {Name}\n";
			message += $"Value: {Inductance}H\n";
			message += $"NetA: {NetA?.Name ?? "null"}\n";
			message += $"NetB: {NetB?.Name ?? "null"}\n";
			message += $"X: {X}, Y: {Y}";
			MessageBox.Show(message, "Inductor");
		}

		public override Impedance GetImpedance(double frequency)
		{
			return new Impedance(0, 2 * Math.PI * frequency * Inductance);
		}

		public override Expression GetExpression()
		{
			return new Product(Expression.S, new ComponentExpression(this));
		}
	}

	public class Capacitor : Component
	{
		public double Capacitance { get; set; }

		public override double PortA_MarginX
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Capacitor_PortA_X;
					case ComponentRotation.H2:
						return ComponentMargins.Capacitor_PortB_X;
					case ComponentRotation.V1:
						return ComponentMargins.Capacitor_PortA_Y;
					case ComponentRotation.V2:
						return ComponentMargins.Capacitor_PortB_Y;
					default:
						return ComponentMargins.Capacitor_PortA_X;
				}
			}
		}
		public override double PortA_MarginY
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Capacitor_PortA_Y;
					case ComponentRotation.H2:
						return ComponentMargins.Capacitor_PortB_Y;
					case ComponentRotation.V1:
						return ComponentMargins.Capacitor_PortA_X;
					case ComponentRotation.V2:
						return ComponentMargins.Capacitor_PortB_X;
					default:
						return ComponentMargins.Capacitor_PortA_Y;
				}
			}
		}
		public override double PortB_MarginX
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Capacitor_PortB_X;
					case ComponentRotation.H2:
						return ComponentMargins.Capacitor_PortA_X;
					case ComponentRotation.V1:
						return ComponentMargins.Capacitor_PortB_Y;
					case ComponentRotation.V2:
						return ComponentMargins.Capacitor_PortA_Y;
					default:
						return ComponentMargins.Capacitor_PortB_X;
				}
			}
		}
		public override double PortB_MarginY
		{
			get
			{
				switch(Rotation)
				{
					case ComponentRotation.H1:
						return ComponentMargins.Capacitor_PortB_Y;
					case ComponentRotation.H2:
						return ComponentMargins.Capacitor_PortA_Y;
					case ComponentRotation.V1:
						return ComponentMargins.Capacitor_PortB_X;
					case ComponentRotation.V2:
						return ComponentMargins.Capacitor_PortA_X;
					default:
						return ComponentMargins.Capacitor_PortB_Y;
				}
			}
		}

		private Line leads;
		private Rectangle capContent;
		private Border border;

		public Capacitor(string name, double capacitance = 0) : base(name)
		{
			VisualGroup.Width = 54;
			VisualGroup.Height = 50;
			Capacitance = capacitance;
		}
		public Capacitor(string name, double x, double y) : base(name, x, y)
		{
			VisualGroup.Width = 54;
			VisualGroup.Height = 50;
			X = x;
			Y = y;
		}
		public Capacitor(string name, double capacitance, double x, double y) : this(name, x, y)
		{
			Capacitance = capacitance;
		}
		public Capacitor(double x, double y) : this("", x, y)
		{
			int i = 1;
			while(true)
			{
				if(!MainWindow.allComponents.Any(c => c.Name.Equals($"C{i}")))
				{
					break;
				}
				i++;
			}
			Name = $"C{i}";
		}
		public Capacitor(double capacitance, double x, double y) : this(x, y)
		{
			Capacitance = capacitance;
		}

		public override string ExportComponent()
		{
			return $"C;{Name};{Capacitance}" + base.ExportComponent();
		}

		public override string GetValueStr()
		{
			return $"1/(s{Name})";
		}

		public override void Draw()
		{
			border = new Border
			{
				Width = 10,
				Height = 40,
				BorderThickness = new Thickness(3, 0, 2.5, 0),
				BorderBrush = Brushes.Black,
				SnapsToDevicePixels = true
			};
			capContent = new Rectangle
			{
				Width = 10,
				Height = 40,
				Fill = Brushes.White,
				SnapsToDevicePixels = true
			};
			leads = new Line
			{
				X1 = 0,
				X2 = 50,
				Y1 = 0,
				Y2 = 0,
				Stroke = LeadColor,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};
			nameText = new TextBlock
			{
				Text = Name
			};

			RenderOptions.SetEdgeMode(border, EdgeMode.Aliased);
			RenderOptions.SetEdgeMode(capContent, EdgeMode.Aliased);
			RenderOptions.SetEdgeMode(leads, EdgeMode.Aliased);

			border.Child = capContent;
			//Canvas.SetLeft(VisualGroup, X-VisualGroup.Width/2);
			//Canvas.SetTop(VisualGroup, Y-VisualGroup.Height/2);
			Canvas.SetLeft(border, 22);
			Canvas.SetTop(border, 5);
			Canvas.SetLeft(leads, 2);
			Canvas.SetTop(leads, 25);
			Canvas.SetLeft(nameText, VisualGroup.Width / 2 - 20);
			Canvas.SetTop(nameText, VisualGroup.Height / 2 - 25);
			Canvas.SetLeft(PortA, X + ComponentMargins.Capacitor_PortA_X - ConnectionPort.Radius);
			Canvas.SetTop(PortA, Y + ComponentMargins.Capacitor_PortA_Y - ConnectionPort.Radius);
			Canvas.SetLeft(PortB, X + ComponentMargins.Capacitor_PortB_X - ConnectionPort.Radius);
			Canvas.SetTop(PortB, Y + ComponentMargins.Capacitor_PortB_Y - ConnectionPort.Radius);
			Panel.SetZIndex(border, 1);
			Panel.SetZIndex(leads, 0);
			Panel.SetZIndex(capContent, 2);
			Panel.SetZIndex(PortA, 3);
			Panel.SetZIndex(PortB, 4);
			VisualGroup.Children.Add(border);
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(nameText);
			baseCanvas.Children.Add(PortA);
			baseCanvas.Children.Add(PortB);
			baseCanvas.Children.Add(VisualGroup);
			Panel.SetZIndex(VisualGroup, baseCanvasMaxZIndex++);
			DrawRotation();
			UpdateBaseCanvas();
		}

		public override void DrawRotation()
		{
			if((int)Rotation % 2 == 0)  // Horizontal
			{
				VisualGroup.Width = 54;
				VisualGroup.Height = 50;
				border.Width = 10;
				border.Height = 40;
				border.BorderThickness = new Thickness(3, 0, 2.5, 0);
				capContent.Width = 10;
				capContent.Height = 40;
				leads.X1 = 0;
				leads.X2 = 50;
				leads.Y1 = 0;
				leads.Y2 = 0;
				Canvas.SetLeft(border, 22); //?
				Canvas.SetTop(border, 5);   //?
				Canvas.SetLeft(leads, 2);   //?
				Canvas.SetTop(leads, 25);   //?
			}
			else
			{
				VisualGroup.Width = 50;
				VisualGroup.Height = 54;
				border.Width = 40;
				border.Height = 10;
				border.BorderThickness = new Thickness(0, 3, 0, 2.5);
				capContent.Width = 40;
				capContent.Height = 10;
				leads.X1 = 0;
				leads.X2 = 0;
				leads.Y1 = 0;
				leads.Y2 = 50;
				Canvas.SetLeft(border, 5);  //?
				Canvas.SetTop(border, 22);  //?
				Canvas.SetLeft(leads, 25);  //?
				Canvas.SetTop(leads, 2);    //?
			}
			Canvas.SetLeft(PortA, X + PortA_MarginX - ConnectionPort.Radius);
			Canvas.SetTop(PortA, Y + PortA_MarginY - ConnectionPort.Radius);
			Canvas.SetLeft(PortB, X + PortB_MarginX - ConnectionPort.Radius);
			Canvas.SetTop(PortB, Y + PortB_MarginY - ConnectionPort.Radius);
			X = x;
			Y = y;
		}

		public override void ShowInfo()
		{
			string message = "";
			message += $"Name: {Name}\n";
			message += $"Value: {Capacitance}F\n";
			message += $"NetA: {NetA?.Name ?? "null"}\n";
			message += $"NetB: {NetB?.Name ?? "null"}\n";
			message += $"X: {X}, Y: {Y}";
			MessageBox.Show(message, "Capacitor");
		}

		public override Impedance GetImpedance(double frequency)
		{
			return new Impedance(0, 1 / (2 * Math.PI * frequency * Capacitance));
		}

		public override Expression GetExpression()
		{
			return new Division
			{
				Numerator = 1,
				Denominator = new Product
				(
					Expression.S,
					new ComponentExpression(this)
				)
			};
		}
	}

	public class Impedance
	{
		public string Value
		{
			get
			{
				string value = "";
				if(Resistance == 0 && Reactance == 0)
				{
					return "0";
				}
				if(Resistance != 0)
				{
					value = $"{Resistance}";
					if(Reactance != 0)
					{
						value += "+";
					}
					else return value;
				}
				if(Reactance != 0)
				{
					value += $"j*{Reactance}";
				}
				return value;
			}
		}
		public double Resistance { get; set; }
		public double Reactance { get; set; }

		public Impedance(double resistance = 0.0, double reactance = 0.0)
		{
			Resistance = resistance;
			Reactance = reactance;
		}

		public static implicit operator Impedance(double resistance)
		{
			return new Impedance(resistance);
		}

		public static Impedance operator +(Impedance Z1, Impedance Z2)
		{
			return new Impedance()
			{
				Resistance = Z1.Resistance + Z2.Resistance,
				Reactance = Z1.Reactance + Z2.Reactance
			};
		}

		public static Impedance operator -(Impedance Z1, Impedance Z2)
		{
			return new Impedance
			{
				Resistance = Z1.Resistance - Z2.Resistance,
				Reactance = Z1.Reactance - Z2.Reactance
			};
		}

		public static Impedance operator *(Impedance Z1, Impedance Z2)
		{
			return new Impedance
			{
				Resistance = Z1.Resistance * Z2.Resistance - Z1.Reactance * Z2.Reactance,
				Reactance = Z1.Resistance * Z2.Reactance + Z1.Reactance * Z2.Resistance
			};
		}

		public static Impedance operator /(Impedance Z1, Impedance Z2)
		{
			double a = Z1.Resistance;
			double b = Z1.Reactance;
			double c = Z2.Resistance;
			double d = Z2.Reactance;
			double Z2_Abs2 = c * c + d * d;
			return new Impedance
			{
				Resistance = (a * c + b * d) / Z2_Abs2,
				Reactance = (b * c - a * d) / Z2_Abs2
			};
		}

		public static Impedance operator |(Impedance Z1, Impedance Z2)  // Parallel-Operator
		{
			return (Z1 * Z2) / (Z1 + Z2);
		}
	}

	//public static class ObervableCollectionExtension
	//{
	//	public static T Find<T>(this ObservableCollection<T> nets, Predicate<T> match)
	//	{
	//		return nets.ToList().Find(match);
	//	}
	//}
}