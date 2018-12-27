using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterDesigner
{
	public abstract class Expression
	{
		public Expression Parent { get; set; }

		public static implicit operator Expression(double value)
		{
			return new ValueExpression(value.ToString());
		}

		/*
		static Expression Parse(string expression)
		{
			Expression result = null;
			if(expression[0] == '(' && GetMatchingBracket(expression, 0) == expression.Length - 1)
				return Parse(expression.Substring(1, expression.Length-2));
			else
			{
				int i = 0;
				int lastBracket = 0;
				int matchingBracket = GetMatchingBracket(expression, 0);
				while(matchingBracket != expression.Length-1)
				{
					if(expression[matchingBracket+1] == '+')
					{
						if(result == null)
							result = new Sum();
						if(result is Sum)
							(result as Sum).AddSummand(Parse(expression.Substring()));
					}
					lastBracket = matchingBracket;
					matchingBracket = GetMatchingBracket(expression, i);
				}
			}


			int openBrackets = 0;
			for(int i = 0; i<expression.Length; )
			{
				if(expression[i] == '(')
				{
					openBrackets++;
					i++;
				}
				if(expression[i] == ')')
				{
					openBrackets--;
					i++;
				}
				if(expression[i] == 'R')
				{
					string currentValueExp = "R";
					while(i != expression.Length
						&& expression[i + 1] != 'R' 
						&& expression[i + 1] != 'L' 
						&& expression[i + 1] != 'C' 
						&& expression[i + 1] != 's'
						&& expression[i + 1] != '(' 
						&& expression[i + 1] != ')' 
						&& expression[i + 1] != '+' 
						&& expression[i + 1] != '*' 
						&& expression[i + 1] != '/' 
						&& expression[i + 1] != '-')
					{
						currentValueExp += expression[i + 1];
						i++;
					}

				}
			}
			return result;
		}
		*/
		private static int GetMatchingBracket(string expression, int bracket)
		{
			if(expression[bracket] != '(') return -1;
			int openBrackets = 0;
			int wantedBracketCount = 0;
			for(int i = 0; i < expression.Length; i++)
			{
				if(i == bracket) wantedBracketCount = openBrackets;
				if(expression[i] == '(')
				{
					openBrackets++;
				}
				if(expression[i] == ')')
				{
					openBrackets--;
				}
				if(openBrackets == wantedBracketCount) return i;
			}
			return -1;
		}

		public abstract string Evaluate();
		public abstract Impedance EvaluateImpedance(double frequency);

		public abstract Expression ToCommonDenominator();

		public abstract void ReplaceChild(Expression oldChild, Expression newChild);

		public abstract void RemoveChild(Expression child);

		public override bool Equals(object other)
		{
			return base.Equals(other);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public abstract bool Contains(Expression other);

		public virtual void Multiply(Expression factor)
		{
			Product p = new Product();
			Parent.ReplaceChild(this, p);
			p.AddFactor(factor);
			p.AddFactor(this);
		}

		//public abstract void Add(Expression summand);
	}

	public class Sum : Expression
	{
		private List<Expression> summands;

		public Sum()
		{
			summands = new List<Expression>();
		}

		public Sum(List<Expression> summands)
		{
			this.summands = new List<Expression>(summands);
		}

		public override string Evaluate()
		{
			if(summands.Count == 0) return "0";
			string result = "";
			foreach(Expression exp in summands)
			{
				string expEvaluate = exp.Evaluate();
				if(expEvaluate.Equals("0")) continue;
				result += "+" + expEvaluate;
			}
			return result.Substring(1);
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			Impedance temp = new Impedance();
			foreach(Expression summand in summands)
			{
				temp = temp + summand.EvaluateImpedance(frequency);
			}
			return temp;
		}

		public void AddSummand(Expression exp)
		{
			if(exp is Sum)
			{
				foreach(Expression e in (exp as Sum).summands)
				{
					AddSummand(e);
				}
			}
			else
			{
				summands.Add(exp);
				exp.Parent = this;
			}
		}

		public override void ReplaceChild(Expression oldChild, Expression newChild)
		{
			int index = summands.IndexOf(oldChild);
			if(index == -1)
			{
				summands.Add(newChild);
			}
			else
			{
				summands[index] = newChild;
			}
			newChild.Parent = this;
		}

		public override void RemoveChild(Expression child)
		{
			summands.Remove(child);
		}

		public override Expression ToCommonDenominator()
		{
			List<Expression> denominators = new List<Expression>();
			Sum numerator = new Sum();
			Product denominator = new Product();
			foreach(Division summand in summands.Where(s => s is Division))
			{
				denominators.Add(summand.Denominator);
				denominator.AddFactor(summand.Denominator);
			}
			foreach(Expression summand in summands)
			{
				Product product = new Product(denominators);
				if(summand is Division)
				{
					product.RemoveChild((summand as Division).Denominator);
					product.AddFactor((summand as Division).Numerator);
				}
				else
				{
					product.AddFactor(summand);
				}
				numerator.AddSummand(product);
			}
			Division result = new Division
			{
				Numerator = numerator,
				Denominator = denominator
			};
			return result;
		}

		public override bool Equals(object other)
		{
			if(!(other is Sum)) return false;
			if(summands.Count != (other as Sum).summands.Count) return false;
			return (other as Sum).summands.All(summands.Contains);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			return summands.Contains(other);
		}
	}

	public class Product : Expression
	{
		private List<Expression> factors;

		public Product()
		{
			factors = new List<Expression>();
		}

		public Product(List<Expression> factors)
		{
			this.factors = new List<Expression>(factors);
		}

		public override string Evaluate()
		{
			string result = "";
			foreach(Expression exp in factors)
			{
				if(exp is Sum)
				{
					string expEvaluate = exp.Evaluate();
					if(expEvaluate.Equals("1"))
					{
						continue;
					}
					if(expEvaluate.Equals("0")) return "0";
					result += "(" + expEvaluate + ")";
				}
				else
				{
					string expEvaluate = exp.Evaluate();
					if(expEvaluate.Equals("1"))
					{
						continue;
					}
					if(expEvaluate.Equals("0")) return "0";
					result += expEvaluate;
				}
			}
			return result;
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			Impedance temp = new Impedance(1);
			foreach(Expression factor in factors)
			{
				temp = temp * factor.EvaluateImpedance(frequency);
			}
			return temp;
		}

		public void AddFactor(Expression exp)
		{
			if(exp is Product)
			{
				foreach(Expression e in (exp as Product).factors)
				{
					AddFactor(e);
				}
			}
			else
			{
				factors.Add(exp);
				exp.Parent = this;
			}
		}

		public override void ReplaceChild(Expression oldChild, Expression newChild)
		{
			int index = factors.IndexOf(oldChild);
			if(index == -1)
			{
				factors.Add(newChild);
			}
			else
			{
				factors[index] = newChild;
			}
			newChild.Parent = this;
		}

		public override void RemoveChild(Expression child)
		{
			factors.Remove(child);
		}

		public override void Multiply(Expression factor)
		{
			AddFactor(factor);
		}

		public override Expression ToCommonDenominator()
		{
			return this;
		}

		public override bool Equals(object other)
		{
			if(!(other is Product)) return false;
			if(factors.Count != (other as Product).factors.Count) return false;
			return (other as Product).factors.All(factors.Contains);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			return factors.Contains(other);
		}
	}

	public class Division : Expression
	{
		private Expression numerator;
		public Expression Numerator
		{
			get
			{
				return numerator;
			}
			set
			{
				numerator = value;
				value.Parent = this;
			}
		}

		private Expression denominator;
		public Expression Denominator
		{
			get
			{
				return denominator;
			}
			set
			{
				denominator = value;
				value.Parent = this;
			}
		}

		public override string Evaluate()
		{
			string result = "";
			if(Numerator is Sum)
			{
				result += "(" + Numerator.Evaluate() + ")";
			}
			else
			{
				result += Numerator.Evaluate();
			}
			if(Denominator is Sum)
			{
				result += "/(" + Denominator.Evaluate() + ")";
			}
			else
			{
				result += "/" + Denominator.Evaluate();
			}
			return result;
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			return Numerator.EvaluateImpedance(frequency) / Denominator.EvaluateImpedance(frequency);
		}

		public override Expression ToCommonDenominator()
		{
			return this;
		}

		public override void ReplaceChild(Expression oldChild, Expression newChild)
		{
			if(Numerator == oldChild)
			{
				Numerator = newChild;
			}
			else if(Denominator == oldChild)
			{
				Denominator = newChild;
			}
			newChild.Parent = this;
		}

		public override void RemoveChild(Expression child)
		{
			if(Numerator == child)
			{
				Numerator = null;
			}
			else if(Denominator == child)
			{
				Denominator = null;
			}
		}

		public override void Multiply(Expression factor)
		{
			if(denominator.Equals(factor))
			{
				Parent.ReplaceChild(this, numerator);
			}
			else if(denominator is Product && (denominator as Product).Contains(factor))
			{
				(denominator as Product).RemoveChild(factor);
			}
			else
			{
				numerator.Multiply(factor);
			}
		}

		public override bool Equals(object other)
		{
			if(!(other is Division)) return false;
			if(!(other as Division).numerator.Equals(numerator)) return false;
			if(!(other as Division).denominator.Equals(denominator)) return false;
			return true;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			return numerator.Equals(other) || denominator.Equals(other);
		}
	}

	public class ValueExpression : Expression
	{
		// Capacitors and Inductors both result in s*Value
		// This allow for more flexible calculation
		private string value = null;
		public Component component;

		public ValueExpression(Component comp)
		{
			component = comp;
		}

		public ValueExpression(string val)
		{
			value = val;
		}

		public override string Evaluate()
		{
			if(value != null) return value;
			if(component is Resistor) return component.Name;
			return "s" + component.Name;
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			if(component is Capacitor)
			{
				return new Impedance(0, 2 * Math.PI * frequency * (component as Capacitor).Capacitance);
			}
			return component.GetImpedance(frequency);
		}

		public override Expression ToCommonDenominator()
		{
			return this;
		}

		public override void ReplaceChild(Expression oldChild, Expression newChild)
		{
		}

		public override void RemoveChild(Expression child)
		{
			return;
		}

		public override bool Equals(object other)
		{
			if(!(other is ValueExpression)) return false;
			if(value != (other as ValueExpression).value) return false;
			if(value != null)
			{
				if(component != (other as ValueExpression).component) return false;
			}
			return true;

		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			return false;
		}
	}
}
