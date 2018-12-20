using System;
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

	public partial class MainWindow : Window
	{
		public static List<Component> allComponents;

		public MainWindow()
		{
			InitializeComponent();
			Component.baseCanvas = canvas;
			Net.baseCanvas = canvas;
			Branchpoint.baseCanvas = canvas;
			canvas.MouseLeftButtonDown += Net.Canvas_MouseDown;
			canvas.MouseLeftButtonUp += Component.Symbol_MouseUp;
			canvas.MouseMove += Component.Symbol_MouseMove;
			allComponents = new List<Component>();

			//Net GND = new Net("GND");
			//Net U1 = new Net("U1");
			//Net U2 = new Net("U2");
			//Net U_dash = new Net("U'");

			Resistor R1 = new Resistor("R1", 100, 100);
			Resistor R2 = new Resistor("R2", 200, 100);
			Resistor R_dash = new Resistor("R'", 150, 200);
			Capacitor C1 = new Capacitor("C1", 300, 150);
			Inductor I = new Inductor("L", 200, 300);

			//R1.Connect(U1, U_dash);
			//R2.Connect(U_dash, U2);
			//R_dash.Connect(U2, U1);
			//C1.Connect(GND, U2);

			DrawAll();
			//List<Path> paths = FindPaths(U1, GND);
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
						currentNet = currentNetOrder.Last();    //				of currentNetOrder?)

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

	public enum LineEndPoint { Point1, Point2 };

	public class Branchpoint
	{
		public Net net { get; set; }

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
				foreach(KeyValuePair<Line,LineEndPoint> line in wires)
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

		public Branchpoint(Net net, double x, double y)
		{
			this.net = net;
			wires = new Dictionary<Line, LineEndPoint>();
			connectionMarker = new Ellipse
			{
				Width = 10,
				Height = 10,
				Fill = Brushes.LimeGreen,
				SnapsToDevicePixels = true
			};
			connectionMarker.MouseLeftButtonDown += Branchpoint_Click;
			X = x;
			Y = y;
			Panel.SetZIndex(connectionMarker, Component.baseCanvasMaxZIndex + 3);
			baseCanvas.Children.Add(connectionMarker);
		}

		public void Branchpoint_Click(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			if(Net.wireAttached)
			{
				net.Attach(null, this, LineEndPoint.Point2);
				Net.wireAttached = false;
			}
			else
			{
				Line newLine = new Line
				{
					X1 = x,
					Y1 = y,
					X2 = x,
					Y2 = y,
					StrokeThickness = 2,
					Stroke = Brushes.Red,
					Visibility = Visibility.Hidden
				};
				newLine.MouseDown += (x, y) => net.Wire_Click(x as Line, y);
				Panel.SetZIndex(newLine, Component.baseCanvasMaxZIndex + 1);
				baseCanvas.Children.Add(newLine);
				wires.Add(newLine, LineEndPoint.Point1);
				net.wires.Add(newLine);

				Net.attachedNet = net;
				Net.attachedComponent = null;
				Net.attachedLine = newLine;
				Net.wireAttached = true;
			}
		}
	}

	public class Net
	{
		public string Name { get; set; }

		public List<Component> components { get; }
		public List<Branchpoint> branchPoints;
		public List<Line> wires;
		public Dictionary<Component, Branchpoint> connections;


		public static bool wireAttached = false;   // Currently drawing a connection?
		public static Net attachedNet;				//Current net attached to line
		public static Component attachedComponent; // Current line origin
		public static NetPort attachedPort;        // Current line origin
		public static Line attachedLine;

		public static Canvas baseCanvas;

		static int netCount = 0;

		public Net() : this($"${netCount++}") { }

		public Net(string name)
		{
			Name = name;
			components = new List<Component>();
			branchPoints = new List<Branchpoint>();
			wires = new List<Line>();
			connections = new Dictionary<Component, Branchpoint>();
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
				if(index < components.Count - 1)
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

		public void Move(Component component, NetPort port, double deltaX, double deltaY)
		{
			if(connections.ContainsKey(component))
			{
				connections[component].X += deltaX;
				connections[component].Y += deltaY;
			}
		}

		public void Wire_Click(Line sender, MouseButtonEventArgs e) // Splits Line
		{
			Point clickPoint = Mouse.GetPosition(baseCanvas);
			double dx = (sender.X1 - sender.X2);
			double dy = (sender.Y1 - sender.Y2);
			double rel = (clickPoint - new Point(sender.X1, sender.Y1)).Length / Math.Sqrt(dx * dx + dy * dy);
			double point_x = sender.X1 + rel * (sender.X2 - sender.X1);
			double point_y = sender.Y1 + rel * (sender.Y2 - sender.Y1);
			Line newLine = new Line
			{
				X1 = point_x,
				Y1 = point_y,
				X2 = sender.X2,
				Y2 = sender.Y2,
				StrokeThickness = 2,
				Stroke = Brushes.Red
			};
			newLine.MouseDown += (x, y) => Wire_Click(x as Line, y);
			sender.X2 = point_x;
			sender.Y2 = point_y;
			Panel.SetZIndex(newLine, Component.baseCanvasMaxZIndex + 1);
			baseCanvas.Children.Add(newLine);

			Branchpoint newBranchPoint = new Branchpoint(this, point_x, point_y);
			newBranchPoint.wires.Add(newLine, LineEndPoint.Point1);
			newBranchPoint.wires.Add(sender, LineEndPoint.Point2);
			branchPoints.Add(newBranchPoint);
			wires.Add(newLine);
			if(wireAttached)
			{
				this.Merge(attachedNet);
				newBranchPoint.wires.Add(attachedLine, LineEndPoint.Point2);
				attachedLine.X2 = point_x;
				attachedLine.Y2 = point_y;
				wireAttached = false;
			}
		}

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

		public static void ClickConnect(Component newComponent, NetPort newPort, MouseButtonEventArgs e)
		{
			e.Handled = true;   // Stop event propagation to prevent the Port from hiding

			if(wireAttached)
			{
				wireAttached = false;
				if(attachedComponent == newComponent && newPort == attachedPort)
				{
					return;
				}
				Net net = newComponent.GetNet(newPort);
				if(net != null)
				{
					if(attachedNet == net)
					{
						MessageBox.Show("Nets are already connected to each other!", "Nets already connected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						return;
					}
					else
					{
						//MessageBox.Show("Nets are already connected to other nets!", "Nets already connected", MessageBoxButton.YesNoCancel);
						// Merge Nets!
						attachedNet.Merge(net);
						newComponent.SetNet(newPort, attachedNet);
					}
				}
				else
				{
					net = attachedNet;
					if(newPort == NetPort.A)
					{
						newComponent.NetA = net;
					}
					else
					{
						newComponent.NetB = net;
					}
				}

				(attachedLine.X2, attachedLine.Y2) = newComponent.GetPortCoordinates(newPort);
				attachedLine.Visibility = Visibility.Visible;
				// Release Mouse Hold of attachedLine?


				if(!net.components.Contains(newComponent))
				{
					net.components.Add(newComponent);
					Branchpoint newBranchPoint = new Branchpoint(net, attachedLine.X2, attachedLine.Y2);
					newBranchPoint.wires.Add(attachedLine, LineEndPoint.Point2);
					net.branchPoints.Add(newBranchPoint);
					net.connections.Add(newComponent, newBranchPoint);
				}
				else
				{
					if(net.connections.ContainsKey(newComponent))
					{
						net.connections[newComponent].wires.Add(attachedLine, LineEndPoint.Point2);
					}
				}
			}
			else
			{
				(double click_x, double click_y) = newComponent.GetPortCoordinates(newPort);
				if(newComponent.GetNet(newPort) == null)
				{
					attachedNet = new Net();
					newComponent.SetNet(newPort, attachedNet);
				}
				else
				{
					attachedNet = newComponent.GetNet(newPort);
				}
				Branchpoint newBranchPoint = new Branchpoint(attachedNet, click_x, click_y);
				Line newLine = new Line
				{
					X1 = click_x,
					Y1 = click_y,
					X2 = click_x,
					Y2 = click_y,
					StrokeThickness = 2,
					Stroke = Brushes.Red,
					Visibility = Visibility.Hidden
				};
				newLine.MouseDown += (x, y) => attachedNet.Wire_Click(x as Line, y);
				Panel.SetZIndex(newLine, Component.baseCanvasMaxZIndex + 1);
				baseCanvas.Children.Add(newLine);
				newBranchPoint.wires.Add(newLine, LineEndPoint.Point1);
				attachedNet.wires.Add(newLine);
				attachedNet.branchPoints.Add(newBranchPoint);
				attachedNet.components.Add(newComponent);
				attachedNet.connections.Add(newComponent, newBranchPoint);

				attachedComponent = newComponent;
				attachedPort = newPort;
				attachedLine = newLine;
				wireAttached = true;
			}
		}

		private void Merge(Net net)
		{
			foreach(Component component in net.components)
			{
				component.SetNet(component.GetPort(net), this);
			}
			branchPoints.AddRange(net.branchPoints);
			components.AddRange(net.components);
			connections.Concat(net.connections);
			wires.AddRange(net.wires);
		}

		public static void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if(wireAttached)
			{
				Point clickPoint = Mouse.GetPosition(baseCanvas);
				attachedLine.Visibility = Visibility.Visible;
				attachedLine.X2 = clickPoint.X;
				attachedLine.Y2 = clickPoint.Y;
				Line newLine = new Line
				{
					X1 = attachedLine.X2,
					Y1 = attachedLine.Y2,
					X2 = clickPoint.X,
					Y2 = clickPoint.Y,
					StrokeThickness = 2,
					Stroke = Brushes.Red,
					Visibility = Visibility.Hidden
				};
				newLine.MouseDown += (x, y) => attachedNet.Wire_Click(x as Line, y);
				Panel.SetZIndex(newLine, Component.baseCanvasMaxZIndex + 1);
				baseCanvas.Children.Add(newLine);

				Branchpoint newBranchPoint = new Branchpoint(attachedNet, clickPoint.X, clickPoint.Y);
				newBranchPoint.wires.Add(attachedLine, LineEndPoint.Point2);
				newBranchPoint.wires.Add(newLine, LineEndPoint.Point1);
				attachedNet.branchPoints.Add(newBranchPoint);
				attachedNet.wires.Add(newLine);
				attachedLine = newLine;
			}
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

		public const int WireThickness = 3;

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
			PortA = new ConnectionPort();
			PortB = new ConnectionPort();
			PortA.MouseLeftButtonDown += (x, y) => Net.ClickConnect(this, NetPort.A, y);
			PortB.MouseLeftButtonDown += (x, y) => Net.ClickConnect(this, NetPort.B, y);
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

		public static void UpdateBaseCanvas()   // Put into wire visualWrapper?
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

		protected static void Symbol_MouseDown(Component sender, MouseButtonEventArgs e)
		{
			int oldZIndex = Panel.GetZIndex(sender.VisualGroup);
			foreach(Canvas childCanvas in baseCanvas.Children.OfType<Canvas>())
			{
				int currentZIndex = Panel.GetZIndex(childCanvas);
				if(currentZIndex > oldZIndex)
				{
					Panel.SetZIndex(childCanvas, currentZIndex - 1);
				}
			}
			//baseCanvas.Children.AsQueryable().OfType<Canvas>().Count;
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
			// Update ConectionPorts & lines
			if(Mouse.LeftButton.HasFlag(MouseButtonState.Pressed) && movedComponent != null)
			{
				Point newMousePos = Mouse.GetPosition(baseCanvas);
				double mouseStartDiffX = newMousePos.X - mousedown_x;
				double mouseStartDiffY = newMousePos.Y - mousedown_y;
				double mouseStepDeltaX = moveStartX + mouseStartDiffX - movedComponent.X;
				double mouseStepDeltaY = moveStartY + mouseStartDiffY - movedComponent.Y;

				movedComponent.NetA?.Move(movedComponent, NetPort.A, mouseStepDeltaX, mouseStepDeltaY);
				movedComponent.NetB?.Move(movedComponent, NetPort.B, mouseStepDeltaX, mouseStepDeltaY);

				movedComponent.X = moveStartX + mouseStartDiffX;        // Move component
				movedComponent.Y = moveStartY + mouseStartDiffY;        //

				//double oldPortX = oldX - movedComponent.PortA_MarginX;
				//double oldPortY = oldY - movedComponent.PortA_MarginY;
				//
				//movedComponent.GetPortCoordinate(NetPort.A);
			}
		}

		public abstract (double X, double Y) GetPortCoordinates(NetPort port);

		//public abstract void SetPortCoordinates(NetPort port, double x, double y);

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

		public NetPort GetPort(Net net)	// Make sure net is part of component
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
	}

	public class Resistor : Component
	{
		private double resistance;

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

		//public override void SetPortCoordinates(NetPort port, double x, double y)
		//{
		//	if(port == NetPort.A)
		//	{
		//		Canvas.SetLeft(PortA, x);
		//		Canvas.SetLeft(PortA, y);
		//	}
		//	else
		//	{
		//		Canvas.SetLeft(PortB, x);
		//		Canvas.SetLeft(PortB, y);
		//	}
		//}
	}

	public class Inductor : Component
	{
		private double inductance;

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
	}

	public class Capacitor : Component
	{
		private double capacitance;

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
			VisualGroup.Children.Add(leads);
			VisualGroup.Children.Add(border);
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
	}

	public class Impedance
	{
		public string value { get; set; }
	}
}