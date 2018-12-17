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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FilterDesigner
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			Net GND = new Net("GND");
			Net U1 = new Net("U1");
			Net U2 = new Net("U2");

			Resistor R1 = new Resistor("R1");
			Capacitor C1 = new Capacitor("C1");

			U1.Connect(U2, R1);
			U2.Connect(GND, C1);

			List<Path> paths = FindPaths(U1, U2);
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
			List<Component> usedComponents = new List<Component>();
			Component currentComp = null;
			while(!done)
			{
				currentComp = currentNet.GetNextComponent(currentComp);
				if(currentComp == null)
				{
					currentPath.path.Remove(currentComp);
					currentNetOrder.Remove(currentNet);     // maybe combine these? (currentNet always
					if(currentNetOrder.Count == 0)
					{
						done = true;
					}
					currentNet = currentNetOrder.Last();	//				of currentNetOrder?)
					continue;
				}

				if(usedComponents.Contains(currentComp))
				{
					continue;
				}
				else
				{
					usedComponents.Add(currentComp);
					currentPath.Add(currentComp);
					currentNetOrder.Add(currentComp.OtherNet(currentNet));
				}
				
				//if()
				if(currentComp.NetA == currentNet && currentComp.NetB == currentNet)
				{
					continue;
				}
				else if(currentComp.IsConnected(B))
				{
					paths.Add(currentPath.Copy());
					currentPath.path.Remove(currentComp);
					currentNetOrder.Remove(currentNet);
					currentNet = currentComp.OtherNet(currentNet);
					continue;
				}
				else if(!currentNetOrder.Contains(currentComp.OtherNet(currentComp.NetA)))
				{
					currentNetOrder.Add(currentComp.OtherNet(currentComp.NetA));
				}
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

		public void Add(Component component)
		{
			path.Add(component);
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
				if(index == components.Count - 1)
				{
					return components[index + 1];
				}
			}
			return component;   // return null/exception?
		}

		public void Connect(Net net, Component component)
		{
			components.Add(component);
			net.components.Add(component);
			component.NetA = this;
			component.NetB = net;
		}
	}

	public abstract class Component
	{
		public string Name { get; set; }
		public Net NetA { get; set; }
		public Net NetB { get; set; }
		public Impedance Impedance { get; }

		protected Component(string name)
		{
			Name = name;
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

	}

	public class Resistor : Component
	{
		private double resistance;

		public string Name { get; set; }
		public Net NetA { get; set; }
		public Net NetB { get; set; }
		public Impedance Impedance { get; }

		public Resistor(string name) : base(name) { }

		public override string GetValueStr()
		{
			return Name;
		}
	}

	public class Inductor : Component
	{
		private double inductance;

		public string Name { get; set; }
		public Net NetA { get; set; }
		public Net NetB { get; set; }
		public Impedance Impedance { get; }

		public Inductor(string name) : base(name) { }

		public override string GetValueStr()
		{
			return "s" + Name;
		}
	}

	public class Capacitor : Component
	{
		private double capacitance;

		public string Name { get; set; }
		public Net NetA { get; set; }
		public Net NetB { get; set; }
		public Impedance Impedance { get; }

		public Capacitor(string name) : base(name) { }

		public override string GetValueStr()
		{
			return $"1/(s{Name})";
		}
	}

	public class Impedance
	{
		public string value { get; set; }
	}
}
