using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FilterDesigner
{
	// TODO:
	// Rotate Components, Display Names
	// Split Line, drag immediately
	// Base move on absolute values of Component, not on relative movements
	// Merge Branchpoints, Delete Lines and split nets
	// GetSummands deep Search with recursion

	public partial class MainWindow : Window
	{
		public static List<Component> allComponents;
		public static ObservableCollection<Net> allNets;

		private static Brush ButtonBackgroundBrush;

		public enum ComponentType { None, Resistor, Capacitor, Inductor };
		public static ComponentType ComponentToAdd = ComponentType.None;

		public MainWindow()
		{
			//Expression standardFormTest = 2+(4 + new ValueExpression("s"))*(3+new ValueExpression("s"));


			// 1/(1+1/(1/s))
			Expression testExp = 1 / (1 + 1 / (1 / (1 / new ValueExpression("s"))));

			
			string eval = testExp.Evaluate();
			testExp = testExp.ToCommonDenominator();
			testExp = testExp.ToStandardForm();
			eval = testExp.Evaluate();

			//string s = "1/(sL)+1/(1+1/(sC))";
			//List<string> fods = StringArithmetic.GetFirstOrderDenominators(s);
			//string fraction = StringArithmetic.ToOneDenominator(s);

			InitializeComponent();
			Component.baseCanvas = canvas;
			Net.baseCanvas = canvas;
			Branchpoint.baseCanvas = canvas;
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
		}

		public string Simplify(string expression)
		{
			if(!expression.Contains('/'))
				return expression;
			//string pattern = @"";
			return "";
		}


		public List<Path> FindPaths(Net A, Net B)
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

				if(currentPath.Components.Contains(currentComp))
				{
					continue;
				}
				else
				{
					if(currentComp.IsUseless())    // comp. is connected badly
					{
						continue;
					}
					else if(currentComp.IsConnected(B))
					{
						paths.Add(currentPath.Copy().Add(currentComp));
						continue;
					}
					else if(currentNetOrder.Contains(currentComp.OtherNet(currentNet)))
					{
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

		public string GetImpedanceOfPaths(List<Path> paths)
		{
			if(paths.Count == 0)
				return null;
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

				Division result = new Division();
				result.Numerator = 1;
				result.Denominator = new Sum();
				foreach(List<Path> lp in listListPaths)
				{
					Division div = new Division();
					(result.Denominator as Sum).AddSummand(div);
					div.Numerator = 1;
					div.Denominator = GetExpressionOfPaths(lp);
				}
				return result;
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

		private void BtnTest_Click(object sender, RoutedEventArgs e)
		{
			if(cbxNet1.SelectedItem == cbxNet2.SelectedItem)
				return;
			List<Path> paths = FindPaths(cbxNet1.SelectedItem as Net, cbxNet2.SelectedItem as Net);
			string impedance = GetImpedanceOfPaths(paths);
			//tbResult.Text = impedance;
			//List<string> result = StringArithmetic.GetSummands(impedance);
			//string denom = StringArithmetic.GetDenominator(impedance);
			Expression exp = GetExpressionOfPaths(paths);
			exp = exp.ToCommonDenominator();
			List<Expression> dens = exp.GetDenominators();
			foreach(Expression denExp in dens)
			{
				string den = denExp.Evaluate();
			}
			exp.ToStandardForm();
			string expression = exp.Evaluate();
			tbResult.Text = expression;
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

		private void PlaceComponent(object sender, MouseButtonEventArgs e)
		{
			if(!Net.wireAttached)
			{
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
				newComponent?.Draw();
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
			return nets;    // Shouldn't happen
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
				if(component is Capacitor)
				{
					Division div = new Division
					{
						Numerator = new ValueExpression("1"),
						Denominator = new ValueExpression(component)
					};
					result.AddSummand(div);
				}
				else
				{
					result.AddSummand(new ValueExpression(component));
				}
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
						line.Key.X1 += value - x;
					}
					else
					{
						line.Key.X2 += value - x;
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
						line.Key.Y1 += value - y;
					}
					else
					{
						line.Key.Y2 += value - y;
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
				if(net.Components.Count(c => net.connections[c].Contains(this)) == 0)
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
			if(net.connections.Count(x => (x.Value.Contains(this))) > 0)
			{
				Component attachedComponent = net.Components.First(c => net.connections[c].Contains(this));
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
		public Dictionary<Component, List<Branchpoint>> connections;
		public static Dictionary<Line, Net> wireDictionary = new Dictionary<Line, Net>();

		public static bool wireAttached = false;        // Currently drawing a connection?
		public static Line attachedLine;                // Current line
		public static Net attachedNet;                  // Current net attached to line
		public static Branchpoint attachedLineOrigin;   // Current line origin

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
			connections = new Dictionary<Component, List<Branchpoint>>();
			MainWindow.allNets.Add(this);
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
				this.Merge(attachedNet);
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

		// TODO: Base move on absolute values of Component, not on relative movements
		public void Move(Component component, double deltaX, double deltaY)
		{
			if(connections.ContainsKey(component))
			{
				foreach(Branchpoint bp in connections[component])
				{
					bp.X += deltaX;
					bp.Y += deltaY;
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
					newNet.connections.Add(newComponent, new List<Branchpoint>());
				}
				newNet.connections[newComponent].Add(newBranchPoint);
			}
			else
			{
				newNet = new Net();
				newComponent.SetNet(newPort, newNet);
				newBranchPoint = new Branchpoint(newNet, point_x, point_y);
				newNet.Components.Add(newComponent);
				if(!newNet.connections.ContainsKey(newComponent))
				{
					newNet.connections.Add(newComponent, new List<Branchpoint>());
				}
				newNet.connections[newComponent].Add(newBranchPoint);
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
			foreach(KeyValuePair<Component, List<Branchpoint>> kvp in net.connections)
			{
				if(!connections.ContainsKey(kvp.Key))
				{
					connections.Add(kvp.Key, new List<Branchpoint>());
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
			if(wireAttached)
			{
				Point clickPoint = Mouse.GetPosition(baseCanvas);
				attachedLine.X2 = clickPoint.X;
				attachedLine.Y2 = clickPoint.Y;
				attachedLine.Visibility = Visibility.Visible;

				Line newLine = attachedNet.NewWire(attachedLine.X2, attachedLine.Y2, clickPoint.X, clickPoint.Y);
				Branchpoint newBranchPoint = new Branchpoint(attachedNet, clickPoint.X, clickPoint.Y);
				attachedNet.branchPoints.Add(newBranchPoint);
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
	public abstract class ComponentMargins
	{
		public static double Resistor_PortA_X = -5;
		public static double Resistor_PortA_Y = +5;
		public static double Resistor_PortB_X = +85;
		public static double Resistor_PortB_Y = +5;

		public static double Inductor_PortA_X = -5;
		public static double Inductor_PortA_Y = +5;
		public static double Inductor_PortB_X = +85;
		public static double Inductor_PortB_Y = +5;

		public static double Capacitor_PortA_X = -5;
		public static double Capacitor_PortA_Y = +15;
		public static double Capacitor_PortB_X = +45;
		public static double Capacitor_PortB_Y = +15;
	};

	public abstract class Component
	{
		public string Name { get; set; }

		private double x;
		public double X
		{
			get
			{
				return x;
			}
			set
			{
				x = value;
				Canvas.SetLeft(VisualGroup, x);
				Canvas.SetLeft(PortA, x + PortA_MarginX);
				Canvas.SetLeft(PortB, x + PortB_MarginX);
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
				y = value;
				Canvas.SetTop(VisualGroup, y);
				Canvas.SetTop(PortA, y + PortA_MarginY);
				Canvas.SetTop(PortB, y + PortB_MarginY);
			}
		}

		public static double WireThickness = 3;
		public static Brush LeadColor = Brushes.Black;

		public abstract double PortA_MarginX { get; }   // Update for Rotation in Childs
		public abstract double PortA_MarginY { get; }   //
		public abstract double PortB_MarginX { get; }   //
		public abstract double PortB_MarginY { get; }   //

		private static Component movedComponent;
		private static double moveStartX;       // Start position of movedComponent
		private static double moveStartY;       //
		private static double mousedown_x = -1;
		private static double mousedown_y = -1;

		public static Canvas baseCanvas;
		public static int baseCanvasMaxZIndex = 0;

		public Canvas VisualGroup;

		public ConnectionPort PortA { get; protected set; }
		public ConnectionPort PortB { get; protected set; }
		public Net NetA { get; set; }
		public Net NetB { get; set; }
		//public Impedance Impedance { get; }

		protected Component(string name)
		{
			Name = name;
			VisualGroup = new Canvas
			{
				Background = Brushes.Transparent,
				Width = 90,
				Height = 20,
				SnapsToDevicePixels = true
			};
			VisualGroup.MouseLeftButtonDown += (x, y) => Symbol_MouseLeftDown(this, y);
			VisualGroup.MouseRightButtonDown += (x, y) => Symbol_RightClick(this, y);
			Panel.SetZIndex(VisualGroup, 0);
			PortA = new ConnectionPort();
			PortB = new ConnectionPort();
			PortA.MouseLeftButtonDown += (x, y) => Net.NetPort_Click(this, NetPort.A, y);
			PortB.MouseLeftButtonDown += (x, y) => Net.NetPort_Click(this, NetPort.B, y);
			MainWindow.allComponents.Add(this);
		}

		protected Component(string name, double x, double y) : this(name)
		{
			X = x;
			Y = y;
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
			}
		}

		protected static void Symbol_MouseLeftDown(Component sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			int oldZIndex = Panel.GetZIndex(sender.VisualGroup);
			foreach(Canvas childCanvas in baseCanvas.Children.OfType<Canvas>())
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
			if(Mouse.LeftButton.HasFlag(MouseButtonState.Pressed) && movedComponent != null)
			{
				Point newMousePos = Mouse.GetPosition(baseCanvas);
				double mouseStartDiffX = newMousePos.X - mousedown_x;
				double mouseStartDiffY = newMousePos.Y - mousedown_y;
				double mouseStepDeltaX = moveStartX + mouseStartDiffX - movedComponent.X;
				double mouseStepDeltaY = moveStartY + mouseStartDiffY - movedComponent.Y;

				movedComponent.NetA?.Move(movedComponent, mouseStepDeltaX, mouseStepDeltaY);
				if(movedComponent.NetB != movedComponent.NetA)
				{
					movedComponent.NetB?.Move(movedComponent, mouseStepDeltaX, mouseStepDeltaY);
				}

				movedComponent.X = moveStartX + mouseStartDiffX;        // Move component
				movedComponent.Y = moveStartY + mouseStartDiffY;        //
			}
		}

		public static void Symbol_RightClick(Component sender, MouseButtonEventArgs e)
		{
			string message = "";
			message += $"Name: {sender.Name}\n";
			message += $"NetA: {sender.NetA?.Name ?? "null"}\n";
			message += $"NetB: {sender.NetB?.Name ?? "null"}\n";
			message += $"X: {sender.X}, Y: {sender.Y}";
			MessageBox.Show(message, "Component");
		}

		public abstract (double X, double Y) GetPortCoordinates(NetPort port);

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
	}

	public class Resistor : Component
	{
		public double Resistance { get; set; }

		// Update for Rotation
		public override double PortA_MarginX
		{
			get { return ComponentMargins.Resistor_PortA_X; }
		}
		public override double PortA_MarginY
		{
			get { return ComponentMargins.Resistor_PortA_Y; }
		}
		public override double PortB_MarginX
		{
			get { return ComponentMargins.Resistor_PortB_X; }
		}
		public override double PortB_MarginY
		{
			get { return ComponentMargins.Resistor_PortB_Y; }
		}

		public Resistor(string name) : base(name) { }
		public Resistor(string name, double x, double y) : base(name, x - 45, y - 10) { }
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

		public override string GetValueStr()
		{
			return Name;
		}

		public override void Draw()
		{
			Rectangle symbol = new Rectangle
			{
				Width = 50,
				Height = 20,
				Fill = Brushes.White,
				Stroke = Brushes.Black,
				StrokeThickness = 2,
				SnapsToDevicePixels = true
			};
			Line leads = new Line
			{
				X1 = 0,
				X2 = 90,
				Y1 = 0,
				Y2 = 0,
				Stroke = LeadColor,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};

			Canvas.SetLeft(VisualGroup, X);
			Canvas.SetTop(VisualGroup, Y);
			Canvas.SetLeft(symbol, 20);
			Canvas.SetTop(symbol, 0);
			Canvas.SetLeft(leads, 0);
			Canvas.SetTop(leads, 10);
			Canvas.SetLeft(PortA, X + ComponentMargins.Resistor_PortA_X);
			Canvas.SetTop(PortA, Y + ComponentMargins.Resistor_PortA_Y);
			Canvas.SetLeft(PortB, X + ComponentMargins.Resistor_PortB_X);
			Canvas.SetTop(PortB, Y + ComponentMargins.Resistor_PortB_Y);
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(symbol);
			baseCanvas.Children.Add(PortA);
			baseCanvas.Children.Add(PortB);
			baseCanvas.Children.Add(VisualGroup);
			Panel.SetZIndex(VisualGroup, baseCanvasMaxZIndex++);
			UpdateBaseCanvas();
		}

		public override (double X, double Y) GetPortCoordinates(NetPort port)   // Relative to basCanvas
		{
			double x, y;
			if(port == NetPort.A)
			{
				x = X;
				y = Y + 10;
			}
			else
			{
				x = X + 90;
				y = Y + 10;
			}
			return (x, y);
		}

		public override string ToString()
		{
			return Name;
		}

		public override Impedance GetImpedance(double frequency)
		{
			return new Impedance(Resistance);
		}
	}

	public class Inductor : Component
	{
		public double Inductance { get; set; }

		// Update for Rotation
		public override double PortA_MarginX
		{
			get { return ComponentMargins.Inductor_PortA_X; }
		}
		public override double PortA_MarginY
		{
			get { return ComponentMargins.Inductor_PortA_Y; }
		}
		public override double PortB_MarginX
		{
			get { return ComponentMargins.Inductor_PortB_X; }
		}
		public override double PortB_MarginY
		{
			get { return ComponentMargins.Inductor_PortB_Y; }
		}

		public Inductor(string name) : base(name) { }
		public Inductor(string name, double x, double y) : base(name, x - 45, y - 10) { }
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

		public override string GetValueStr()
		{
			return "s" + Name;
		}

		public override void Draw()
		{
			Rectangle symbol = new Rectangle
			{
				Width = 50,
				Height = 20,
				Fill = Brushes.Black,
				Stroke = Brushes.Black,
				StrokeThickness = 2,
				SnapsToDevicePixels = true
			};
			Line leads = new Line
			{
				X1 = 0,
				X2 = 90,
				Y1 = 0,
				Y2 = 0,
				Stroke = LeadColor,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};

			Canvas.SetLeft(VisualGroup, X);
			Canvas.SetTop(VisualGroup, Y);
			Canvas.SetLeft(symbol, 20);
			Canvas.SetTop(symbol, 0);
			Canvas.SetLeft(leads, 0);
			Canvas.SetTop(leads, 10);
			Canvas.SetLeft(PortA, X + ComponentMargins.Inductor_PortA_X);
			Canvas.SetTop(PortA, Y + ComponentMargins.Inductor_PortA_Y);
			Canvas.SetLeft(PortB, X + ComponentMargins.Inductor_PortB_X);
			Canvas.SetTop(PortB, Y + ComponentMargins.Inductor_PortB_Y);
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(symbol);
			baseCanvas.Children.Add(PortA);
			baseCanvas.Children.Add(PortB);
			baseCanvas.Children.Add(VisualGroup);
			Panel.SetZIndex(VisualGroup, baseCanvasMaxZIndex++);
			UpdateBaseCanvas();
		}

		public override (double X, double Y) GetPortCoordinates(NetPort port)    // Relative to basCanvas
		{
			double x, y;
			if(port == NetPort.A)
			{
				x = X;
				y = Y + 10;
			}
			else
			{
				x = X + 90;
				y = Y + 10;
			}
			return (x, y);
		}

		public override Impedance GetImpedance(double frequency)
		{
			return new Impedance(0, 2 * Math.PI * frequency * Inductance);
		}
	}

	public class Capacitor : Component
	{
		public double Capacitance { get; set; }

		// Update for Rotation:
		public override double PortA_MarginX
		{
			get { return ComponentMargins.Capacitor_PortA_X; }
		}
		public override double PortA_MarginY
		{
			get { return ComponentMargins.Capacitor_PortA_Y; }
		}
		public override double PortB_MarginX
		{
			get { return ComponentMargins.Capacitor_PortB_X; }
		}
		public override double PortB_MarginY
		{
			get { return ComponentMargins.Capacitor_PortB_Y; }
		}

		public Capacitor(string name) : base(name)
		{
			VisualGroup.Width = 50;
			VisualGroup.Height = 40;
		}
		public Capacitor(string name, double x, double y) : base(name, x - 25, y - 20)
		{
			VisualGroup.Width = 50;
			VisualGroup.Height = 40;
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

		public override string GetValueStr()
		{
			return $"1/(s{Name})";
		}

		public override void Draw()
		{
			Border border = new Border
			{
				Width = 10,
				Height = 40,
				BorderThickness = new Thickness(3, 0, 2.5, 0),
				BorderBrush = Brushes.Black,
				SnapsToDevicePixels = true
			};
			Rectangle capContent = new Rectangle
			{
				Width = 10,
				Height = 40,
				Fill = Brushes.White,
				SnapsToDevicePixels = true
			};
			Line leads = new Line
			{
				X1 = 0,
				X2 = 50,
				Y1 = 0,
				Y2 = 0,
				Stroke = LeadColor,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};

			border.Child = capContent;
			Panel.SetZIndex(border, 1);
			Panel.SetZIndex(leads, 0);
			Panel.SetZIndex(capContent, 2);
			Panel.SetZIndex(PortA, 3);
			Panel.SetZIndex(PortB, 4);
			Canvas.SetLeft(VisualGroup, X);
			Canvas.SetTop(VisualGroup, Y);
			Canvas.SetLeft(border, 20);
			Canvas.SetTop(border, 0);
			Canvas.SetLeft(leads, 0);
			Canvas.SetTop(leads, 20);
			Canvas.SetLeft(PortA, X + ComponentMargins.Capacitor_PortA_X);
			Canvas.SetTop(PortA, Y + ComponentMargins.Capacitor_PortA_Y);
			Canvas.SetLeft(PortB, X + ComponentMargins.Capacitor_PortB_X);
			Canvas.SetTop(PortB, Y + ComponentMargins.Capacitor_PortB_Y);
			VisualGroup.Children.Add(border);
			VisualGroup.Children.Add(leads);
			baseCanvas.Children.Add(PortA);
			baseCanvas.Children.Add(PortB);
			baseCanvas.Children.Add(VisualGroup);
			Panel.SetZIndex(VisualGroup, baseCanvasMaxZIndex++);
			UpdateBaseCanvas();
		}

		public override (double X, double Y) GetPortCoordinates(NetPort port)    // Relative to basCanvas
		{
			double x, y;
			if(port == NetPort.A)
			{
				x = X;
				y = Y + 20;
			}
			else
			{
				x = X + 50;
				y = Y + 20;
			}
			return (x, y);
		}

		public override Impedance GetImpedance(double frequency)
		{
			return new Impedance(0, 1 / (2 * Math.PI * frequency * Capacitance));
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