using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FilterDesigner
{
	// TODO:
	// Rotate Components, Display Names
	// Connect

	public partial class MainWindow : Window
	{
		public static List<Component> allComponents;

		public MainWindow()
		{
			InitializeComponent();
			Component.baseCanvas = canvas;
			canvas.MouseLeftButtonUp += Component.Symbol_MouseUp;
			canvas.MouseMove += Component.Symbol_MouseMove;
			allComponents = new List<Component>();

			Net GND = new Net("GND");
			Net U1 = new Net("U1");
			Net U2 = new Net("U2");
			Net U_dash = new Net("U'");

			Resistor R1 = new Resistor("R1", 100, 100);
			Resistor R2 = new Resistor("R2", 200, 100);
			Resistor R_dash = new Resistor("R'", 150, 200);
			Capacitor C1 = new Capacitor("C1", 300, 150);
			Inductor I = new Inductor("L", 200, 300);

			R1.Connect(U1, U_dash);
			R2.Connect(U_dash, U2);
			R_dash.Connect(U2, U1);
			C1.Connect(GND, U2);
			
			DrawAll();
			List<Path> paths = FindPaths(U1, GND);
		}

		public void DrawAll()
		{
			foreach(Component comp in allComponents)
			{
				comp.Draw();
			}
		}

		public List<Path> FindPaths(Net A, Net B)
		{
			List<Path> paths = new List<Path>();
			bool done = false;
			Net currentNet = A;
			//Component source;
			//List<Net> visitedNets = new List<Net>();    // Nets that are completed
			Path currentPath = new Path();
			List<Net> currentNetOrder = new List<Net>();    // Represents the current parse state 
			currentNetOrder.Add(A);
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
						currentComp = currentPath.path.Last();
						currentPath.path.Remove(currentComp);
						currentNet = currentNetOrder.Last();	//				of currentNetOrder?)

					}
					continue;
				}

				if(currentPath.path.Contains(currentComp))
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
	}

	public class Path
	{
		public List<Component> path { get; }

		public Path()
		{
			path = new List<Component>();
		}

		public Path Add(Component component)
		{
			path.Add(component);
			return this;
		}

		public Path Copy()
		{
			Path copy = new Path();
			foreach(Component component in path)
			{
				copy.Add(component);
			}
			return copy;
		}
	}

	public class Net
	{
		public string Name { get; set; }

		public List<Component> components { get; }

		public Net(string name)
		{
			Name = name;
			components = new List<Component>();
		}

		public Component GetNextComponent(Component component)
		{
			if(components.Count == 0)
			{
				return null;
			}
			if(component == null)
			{
				return components.First();
			}
			if(components.Contains(component))
			{
				int index = components.IndexOf(component);
				if(index < components.Count-1)
				{
					return components[index + 1];
				}
			}
			return null;
		}

		public void Connect(Net net, Component component)
		{
			components.Add(component);
			net.components.Add(component);
			component.NetA = this;
			component.NetB = net;
		}
	}

	public enum NetPort { PortA, PortB };

	public abstract class Component
	{
		public string Name { get; set; }

		public const int WireThickness = 3;
		
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
			}
		}

		private static bool wireAttached = false;
		private static Component attachedComponent;
		private static NetPort attachedPort;

		private static double oldX;
		private static double oldY;
		private static double mousedown_x = -1;
		private static double mousedown_y = -1;
		private static Component movedComponent;

		public static Canvas baseCanvas;

		public Canvas VisualGroup;

		public Net NetA { get; set; }
		public Net NetB { get; set; }
		public Impedance Impedance { get; }

		protected Component(string name)
		{
			Name = name;
			VisualGroup = new Canvas
			{
				Background = Brushes.Transparent,  // Transparent
				Width = 90,
				Height = 20
			};
			VisualGroup.MouseLeftButtonDown += (x, y) => Symbol_MouseDown(this, y);
			Panel.SetZIndex(VisualGroup, 0);
			MainWindow.allComponents.Add(this);
		}

		protected Component(string name, int x, int y) : this(name)
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
			A.components.Add(this);
			B.components.Add(this);
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

		protected static void Symbol_MouseDown(Component sender, MouseButtonEventArgs e)
		{
			int oldZIndex = Panel.GetZIndex(sender.VisualGroup);
			foreach(Canvas childCanvas in baseCanvas.Children)
			{
				int currentZIndex = Panel.GetZIndex(childCanvas);
				if(currentZIndex > oldZIndex)
					Panel.SetZIndex(childCanvas, currentZIndex-1);
			}
			Panel.SetZIndex(sender.VisualGroup, baseCanvas.Children.Count-1);
			sender.VisualGroup.CaptureMouse();
			movedComponent = sender;
			Point mousePos = Mouse.GetPosition(baseCanvas);
			mousedown_x = mousePos.X;
			mousedown_y = mousePos.Y;
			oldX = sender.X;
			oldY = sender.Y;
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
				movedComponent.X = oldX - (mousedown_x - newMousePos.X);
				movedComponent.Y = oldY - (mousedown_y - newMousePos.Y);
			}
		}
		
		public static void ClickConnect(Component sender, NetPort port, MouseButtonEventArgs e)
		{
			e.Handled = true;   // Stop event propagation to prevent the Port from hiding

			if(wireAttached)
			{
				wireAttached = false;
				if(attachedComponent == sender && port == attachedPort)
				{
					return;
				}
				Line
			}
			else
			{
				attachedComponent = sender;
				attachedPort = port;
				wireAttached = true;
			}
		}

		public abstract (double X, double Y) GetPortCoordinate(NetPort port);
	}

	public class Resistor : Component
	{
		private double resistance;

		public Resistor(string name) : base(name) { }
		public Resistor(string name, int x, int y) : base(name, x, y) { }

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
				Fill = Brushes.Black,   // Dark Gray/Black/Red?
				Stroke = Brushes.Black,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};
			ConnectionPort portA = new ConnectionPort();
			ConnectionPort portB = new ConnectionPort();

			portA.MouseLeftButtonDown += (x, y) => ClickConnect(this, NetPort.PortA, y);
			portB.MouseLeftButtonDown += (x, y) => ClickConnect(this, NetPort.PortB, y);
			 
			Canvas.SetLeft(VisualGroup, X);
			Canvas.SetTop(VisualGroup, Y);
			Canvas.SetLeft(symbol, 20);
			Canvas.SetTop(symbol, 0);
			Canvas.SetLeft(leads, 0);
			Canvas.SetTop(leads, 10);
			Canvas.SetLeft(portA, -5);
			Canvas.SetLeft(portB, 85);
			Canvas.SetTop(portA, 5);
			Canvas.SetTop(portB, 5);
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(symbol);
			VisualGroup.Children.Add(portA);
			VisualGroup.Children.Add(portB);
			baseCanvas.Children.Add(VisualGroup);
		}

		public override (double X, double Y) GetPortCoordinate(NetPort port)
		{
			if(port == NetPort.PortA)
			{
				return (0, 0); 
			}
			else
			{
				return (0, 0);
			}
		}
	}

	public class Inductor : Component
	{
		private double inductance;
		
		public Inductor(string name) : base(name) { }
		public Inductor(string name, int x, int y) : base(name, x, y) { }

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
				Fill = Brushes.Black,   // Dark Gray/Black/Red?
				Stroke = Brushes.Black,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};
			ConnectionPort portA = new ConnectionPort();
			ConnectionPort portB = new ConnectionPort();

			portA.MouseLeftButtonDown += (x, y) => ClickConnect(this, NetPort.PortA, y);
			portB.MouseLeftButtonDown += (x, y) => ClickConnect(this, NetPort.PortB, y);

			Canvas.SetLeft(VisualGroup, X);
			Canvas.SetTop(VisualGroup, Y);
			Canvas.SetLeft(symbol, 20);
			Canvas.SetTop(symbol, 0);
			Canvas.SetLeft(leads, 0);
			Canvas.SetTop(leads, 10);
			Canvas.SetLeft(portA, -5);
			Canvas.SetLeft(portB, 85);
			Canvas.SetTop(portA, 5);
			Canvas.SetTop(portB, 5);
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(symbol);
			VisualGroup.Children.Add(portA);
			VisualGroup.Children.Add(portB);
			baseCanvas.Children.Add(VisualGroup);
		}
	}

	public class Capacitor : Component
	{
		private double capacitance;
		
		public Capacitor(string name) : base(name)
		{
			VisualGroup.Width = 30;
			VisualGroup.Height = 40;
		}
		public Capacitor(string name, int x, int y) : base(name, x, y)
		{
			VisualGroup.Width = 50;
			VisualGroup.Height = 40;
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
				Height = border.Height,
				Width = border.Width,
				Fill = Brushes.White,
				SnapsToDevicePixels = true
			};
			Line leads = new Line
			{
				X1 = 0,
				X2 = 50,
				Y1 = 0,
				Y2 = 0,
				Fill = Brushes.Black,   // Dark Gray/Black/Red?
				Stroke = Brushes.Black,
				StrokeThickness = WireThickness,
				SnapsToDevicePixels = true
			};
			ConnectionPort portA = new ConnectionPort();
			ConnectionPort portB = new ConnectionPort();
			
			portA.MouseLeftButtonDown += (x, y) => ClickConnect(this, NetPort.PortA, y);
			portB.MouseLeftButtonDown += (x, y) => ClickConnect(this, NetPort.PortB, y);

			border.Child = capContent;
			Panel.SetZIndex(border, 1);
			Panel.SetZIndex(leads, 0);
			Panel.SetZIndex(capContent, 2);
			Panel.SetZIndex(portA, 3);
			Panel.SetZIndex(portB, 4);
			Canvas.SetLeft(VisualGroup, X);
			Canvas.SetTop(VisualGroup, Y);
			Canvas.SetLeft(border, 20);
			Canvas.SetTop(border, 0);
			Canvas.SetLeft(leads, 0);
			Canvas.SetTop(leads, 20);
			Canvas.SetLeft(portA, -5);
			Canvas.SetLeft(portB, 45);
			Canvas.SetTop(portA, 15);
			Canvas.SetTop(portB, 15);
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(border);
			VisualGroup.Children.Add(portA);
			VisualGroup.Children.Add(portB);
			baseCanvas.Children.Add(VisualGroup);
		}
	}

	public class Impedance
	{
		public string value { get; set; }
	}
}