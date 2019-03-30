using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NumberScaleTest
{
	class Program   // Non-Logarithmic
	{
		public static double Max;
		public static double Min;

		private static int maxPow; // Smallest Power of 10 smaller than max
		private static int minPow;  // Largest Power of 10 bigger than min

		static int MaxPow => maxPow;

		static int MinPow => minPow;

		static void SetMaxPow(double value)
		{
			maxPow = (int)Math.Log10(value);        // Round down
		}

		static void SetMinPow(double value)
		{
			minPow = (int)(Math.Log10(value) + 0.5);// Round up
		}
				
		static void Main(string[] args)
		{
			Min = 1e3;
			Max = 1e5;
			int powerStep = (int)(Math.Log10(Max - Min));
			int iMax = (int)(10 * Math.Log10(Max/Min));
			//for(int i = 0; i<iMax; i++)
			//{
			//	double x = Math.Pow(10, (int)(Math.Log10(Min)) + i/10 + Math.Log10(i % 10 + 1));
			//	Console.WriteLine($"{i}: {x}");
			//}
			//for(int i = 8; i < 12; i++)
			//{
			//	double x = 990 + Math.Pow(10, i / 10 + Math.Log10(i % 10 + 1));
			//	Console.WriteLine($"{i}: {x}");
			//}
			//Console.ReadKey();
			//return;
			
			int N = 1;
			//Min = 10002;		// => 10001, 10002, 10003, 10004, 10005, 10006
			//Max = 10007;

			while(true)
			{
				string s = DoubleToText(0.00010007e-15);

				Console.Write("Min: ");
				Min = double.Parse(Console.ReadLine())/20.0;
				Console.Write("Max: ");
				Max = double.Parse(Console.ReadLine())/20.0;
				//Console.Write("N: ");
				//N = int.Parse(Console.ReadLine());
				//Console.WriteLine("Mode: ");
				//string mode = Console.ReadLine();


				if(Min >= Max)
				{
					Console.WriteLine("Error, values not allowed!");
					continue;
				}




				//int j = 10 * (int)Math.Log10(Min);
				//int iMin = (int)(Min / Math.Pow(10, j / 10) - 1 + 0.5) + j - 1;
				//
				//j = 10 * (int)Math.Log10(Max);
				//iMax = (int)(Max / Math.Pow(10, j / 10) - 1 + 0.5) + j + 1;
				////iMax = 10 * (int)Math.Log10(Max / Min);
				//for(int i = iMin; i < iMax; i++)
				//{
				//	double x = Math.Pow(10, Math.Floor(i / 10.0)) * (Mod(i, 10) + 1);
				//	if(x <= Max && x >= Min)
				//	{
				//		Console.WriteLine($"{i}: {x}");
				//	}
				//}
				//continue;

				SetMinPow(Min);
				SetMaxPow(Max);
				//Console.WriteLine("min: {0} > {1} => {2}", Min, MinPow, Math.Pow(10, MinPow));
				//Console.WriteLine("max: {0} > {1} => {2}", Max, MaxPow, Math.Pow(10, MaxPow));
				
				List<double>[] result;
				List<double> specialValues;
				result = GetNOrders(2, out specialValues, 10, 10);
				//switch(mode)
				//{
				//	case "LOG":
				//		result = GetNOrdersLogarithmic(N, out specialValues, 10, 10);
				//		break;
				//	case "DEZ":
				//		result = GetNOrdersDecibel(N, out specialValues, 20, 20);
				//		break;
				//	default:
				//		result = GetNOrders(N, out specialValues, 10, 10);
				//		break;
				//}
				if(specialValues != null)
				{
					foreach(double d in specialValues)
					{
						Console.WriteLine($"SP: {20*d}");
					}
				}
				for(int i = 0; i <= N; i++)
				{
					foreach(double d in result[i])   // double d in GetPowerSeries(i)
					{
						Console.WriteLine(20*d);
					}
				}
			}
			Console.ReadKey();
		}

		static string DoubleToText(double d)
		{
			int exponent = (int)Math.Floor(Math.Log10(d));
			double mantisse = Math.Round(d / Math.Pow(10, exponent), 2, MidpointRounding.AwayFromZero);
			string result = "";
			if(mantisse != 1.0)
			{
				result += string.Format("{0:#.##}\xB7", mantisse);
			}
			result += string.Format("10{0}", StrToSuperscript(string.Format("{0:##}", exponent)));
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

		static int Mod(int x, int m) // % can be negative, Mod must be positive
		{
			return (x % m + m) % m;
		}

		static List<double>[] GetNOrdersLogarithmic(int n, out List<double> specialValues, int @base = 10, int specialMask = 10)
		{
			specialValues = null;
			List<double>[] result = new List<double>[n];
			int powerStep = (int)(Math.Log10(Max - Min)/Math.Log10(@base));
			result[0] = GetPowerSeriesLogarithmic(powerStep, out List<double> specVals, @base, specialMask);
			if(specVals != null && specVals.Count != 0)
				specialValues = specVals;
			for(int i = 1; i < n; i++)
			{
				powerStep--;
				result[i] = GetPowerSeriesLogarithmic(powerStep, out _, @base, specialMask);
			}
			return result;
		}

		static List<double> GetPowerSeriesLogarithmic(int exp, out List<double> specialValues, int @base = 10, int specialMask = 10)
		{
			specialValues = null;
			int maxN = (int)(Max / (Math.Pow(@base, exp)));
			int minN = (int)(Min / (Math.Pow(@base, exp)));
			List<double> result = new List<double>();
			for(int i = minN; i <= maxN; i++)
			{
				double number = Math.Pow(@base, Math.Log10(Min) + Math.Log10(i + 1) + (i / 10));
				if(i % @base != 0)
				{
					result.Add(number);
				}
				else if(number >= Min)
				{
					if(specialValues == null)
					{
						specialValues = new List<double>();
					}
					specialValues.Add(number);
				}
			}
			return result;
		}

		static List<double>[] GetNOrdersDecibel(int n, out List<double> specialValues, int @base = 10, int specialMask = 10)
		{
			specialValues = null;
			List<double>[] result = new List<double>[n];
			int powerStep = (int)(Math.Log10(Max - Min) / Math.Log10(@base));
			result[0] = GetPowerSeriesDecibel(powerStep, out List<double> specVals, @base, specialMask);
			if(specVals != null && specVals.Count != 0)
				specialValues = specVals;
			for(int i = 1; i < n; i++)
			{
				powerStep--;
				result[i] = GetPowerSeriesDecibel(powerStep, out _, @base, specialMask);
			}
			return result;
		}

		static List<double> GetPowerSeriesDecibel(int exp, out List<double> specialValues, int @base = 10, int specialMask = 10)
		{
			specialValues = null;
			int maxN = (int)(Max / (Math.Pow(@base, exp)));
			int minN = (int)(Min / (Math.Pow(@base, exp)));
			List<double> result = new List<double>();
			for(int i = minN; i <= maxN; i++)
			{
				double number = i * Math.Pow(@base, exp);
				if(i % @base != 0)
				{
					result.Add(number);
				}
				else if(number >= Min)
				{
					if(specialValues == null)
					{
						specialValues = new List<double>();
					}
					specialValues.Add(number);
				}
			}
			return result;
		}

		static List<double>[] GetNOrders(int n, out List<double> specialValues, int @base = 10, int specialMask = 10)
		{
			specialValues = null;
			List<double>[] result = new List<double>[n];
			int powerStep = (int)(Math.Log10(Max - Min) / Math.Log10(@base));
			result[0] = GetPowerSeries(powerStep, out List<double> specVals, @base, specialMask);
			if(specVals != null && specVals.Count != 0)
				specialValues = specVals;
			for(int i = 1; i < n; i++)
			{
				powerStep--;
				result[i] = GetPowerSeries(powerStep, out _, @base, specialMask);
			}
			return result;
		}

		static List<double> GetPowerSeries(int exp, out List<double> specialValues, int @base = 10, int specialMask = 10)
		{
			specialValues = null;
			int maxN = (int)(Max / (Math.Pow(@base, exp))) + 1;
			int minN = (int)(Min / (Math.Pow(@base, exp)) + 0.5) - 1;
			List<double> result = new List<double>();
			for(int i = minN; i <= maxN; i++)
			{
				double number = i * Math.Pow(@base, exp);
				if(number >= Min && number <= Max)
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
	}
}
