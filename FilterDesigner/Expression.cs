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

		public abstract bool ContainsFraction();

		public abstract bool IsFinal();

		public abstract Expression ToCommonDenominator();

		public abstract Expression ToStandardForm();

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

		public abstract List<Expression> GetDenominators(List<Expression> result=null);

		public virtual Expression Multiply(Expression factor)
		{
			Product p = new Product();
			//Parent.ReplaceChild(this, p);
			p.AddFactor(factor.Copy());
			p.AddFactor(this);
			return p;
		}

		public abstract Expression Copy();

		//public abstract Expression Add(Expression summand);
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

		public Sum(Expression exp1, Expression exp2)
		{
			summands = new List<Expression>();
			AddSummand(exp1);
			AddSummand(exp2);
		}

		public void Merge(List<Expression> newSummands)
		{
			foreach(Expression exp in newSummands)
			{
				AddSummand(exp);
			}
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
					AddSummand(e.Copy());
				}
			}
			else
			{
				Expression copy = exp.Copy();
				summands.Add(copy);
				copy.Parent = this;
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

		public override Expression Multiply(Expression factor)
		{
			Sum copy = Copy() as Sum;
			for(int i = 0; i<copy.summands.Count; i++)
			{
				copy.summands[i].Multiply(factor);
			}
			return copy;
		}

		public static Sum MultiplySums(Sum sum1, Sum sum2)
		{
			Sum result = new Sum();
			foreach(Expression exp1 in sum1.summands)
			{
				foreach(Expression exp2 in sum2.summands)
				{
					result.AddSummand(exp1.Multiply(exp2));
				}
			}
			return result;
		}

		public override Expression ToCommonDenominator()	// Only use this when overwriting old Expression
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

		public override Expression ToStandardForm()
		{ // Standardform is a sum of final products&valueexpressions
			if(ContainsFraction()) return null;
			Sum standardForm = Copy() as Sum;
			while(!summands.All(e => (e is Product || e is ValueExpression) && e.IsFinal()))
			{
				foreach(Sum sum in standardForm.summands.Where(s => s is Sum))
				{
					standardForm.Merge(sum.summands);
				}
				List<Product> lp = standardForm.summands.Where(s => s is Product && !s.IsFinal()).Cast<Product>().ToList();
				for(int i = 0; i<lp.Count; i++)
				{ // Product contains Sums,Products&ValueExpressions
					Product stProd = lp[i].ToStandardForm() as Product;
					standardForm.ReplaceChild(lp[i], stProd.ToSum());
				}
			}
			return standardForm;
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

		public override bool ContainsFraction()
		{
			foreach(Expression expression in summands)
			{
				if(expression.ContainsFraction())
				{
					return true;
				}
			}
			return false;
		}

		public override bool IsFinal()
		{
			return summands.All(s => s is ValueExpression);
		}

		public override List<Expression> GetDenominators(List<Expression> result = null)
		{
			List<Expression> newResult;
			if(result == null)
				newResult = new List<Expression>();
			else
				newResult = result;
			foreach(Expression exp in summands)
			{
				if(exp is Division)
				{
					Expression newDen = (exp as Division).Denominator;
					if(!newResult.Contains(newDen))
					{
						newResult.Add(newDen);
					}
				}
				else if(exp is Product)
				{
					newResult = exp.GetDenominators(newResult);
				}
			}
			return newResult;
		}

		public override Expression Copy()
		{
			Sum copy = new Sum();
			foreach(Expression exp in summands)
			{
				copy.summands.Add(exp.Copy());
			}
			return copy;
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
			foreach(Expression factor in factors)
			{
				factor.Parent = this;
			}
			CleanUp();
		}

		public Product(Expression exp1, Expression exp2)
		{
			factors = new List<Expression>();
			AddFactor(exp1);
			AddFactor(exp2);
		}

		public void Merge(List<Expression> newFactors)
		{
			foreach(Expression exp in newFactors)
			{
				AddFactor(exp);
			}
			CleanUp();
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
					AddFactor(e.Copy());
				}
			}
			else
			{
				Expression copy = exp.Copy();
				copy.Parent = this;
				factors.Add(copy);
			}
			CleanUp();
		}

		public override void ReplaceChild(Expression oldChild, Expression newChild)
		{
			int index = factors.IndexOf(oldChild);
			Expression copy = newChild.Copy();
			if(index == -1)
			{
				factors.Add(copy);
			}
			else
			{
				factors[index] = copy;
			}
			copy.Parent = this;
			//copy.CleanUp();
		}

		public override void RemoveChild(Expression child)
		{
			factors.Remove(child);
		}

		public override Expression Multiply(Expression factor)
		{
			Product copy = Copy() as Product;
			copy.AddFactor(factor);
			copy.CleanUp();
			return copy;
		}

		public override Expression ToCommonDenominator()
		{
			return this;
		}

		public override Expression ToStandardForm()
		{ // Standardform is a product of final sums&valueexpressions
			Product result = Copy() as Product;
			if(ContainsFraction()) return null;
			while(!result.factors.All(e => (e is Sum || e is ValueExpression) && e.IsFinal()))
			{
				foreach(Product product in result.factors.Where(f => f is Product))
				{
					result.Merge(product.factors);
				}
				foreach(Sum sum in factors.Where(s => s is Sum))
				{ // Product contains Sums,Products&ValueExpressions
					sum.ToStandardForm();
				}
			}
			return result;
		}

		public Sum ToSum()
		{
			Product copy = Copy() as Product;
			Sum result = new Sum();
			Product productWithoutSums = new Product(copy.factors.Where(f => f is ValueExpression).ToList());
			result.AddSummand(productWithoutSums);
			//List<Expression> sumlessFactors = factors..(f => f is ValueExpression);
			foreach(Sum sum in copy.factors.Where(f => f is Sum))
			{
				result = Sum.MultiplySums(result, sum);
			}
			return result;
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

		public override bool ContainsFraction()
		{
			foreach(Expression expression in factors)
			{
				if(expression.ContainsFraction())
				{
					return true;
				}
			}
			return false;
		}

		public override bool IsFinal()
		{
			return factors.All(f => f is ValueExpression);
		}

		public override List<Expression> GetDenominators(List<Expression> result = null)
		{	// Copies necessary?
			List<Expression> newResult;
			if(result == null)
				newResult = new List<Expression>();
			else
				newResult = result;
			foreach(Expression exp in factors)
			{
				if(exp is Division)
				{
					Expression newDen = (exp as Division).Denominator;
					newResult.Add(newDen);		// Since this is a product, the denominators can be added multiple times
				}
			}
			return newResult;
		}

		public void CleanUp()
		{
			List<Expression> factorsToRemove = new List<Expression>();
			foreach(Expression exp in factors)
			{
				if(exp.Equals(1))
				{
					factorsToRemove.Add(exp);
				}
			}
			factorsToRemove.ForEach(f => factors.Remove(f));
			if(factors.Count == 0)
			{
				factors.Add(1);
			}
			if(factors.Count == 1)	// Careful, don't have references to this from outside
			{
				Parent?.ReplaceChild(this, factors[0]);
			}
		}

		public override Expression Copy()
		{
			Product copy = new Product();
			foreach(Expression exp in factors)
			{
				copy.factors.Add(exp.Copy());
			}
			return copy;
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
			string tempRes = numerator.Evaluate();
			if(tempRes.Length > 1)
			{
				result += "(" + tempRes + ")";
			}
			else
			{
				result += tempRes;
			}
			tempRes = denominator.Evaluate();
			if(tempRes.Length > 1)
			{
				result += "/(" + tempRes + ")";
			}
			else
			{
				result += "/" + tempRes;
			}
			return result;
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			return Numerator.EvaluateImpedance(frequency) / Denominator.EvaluateImpedance(frequency);
		}

		public override Expression ToCommonDenominator()
		{
			return Copy();
		}

		public override void ReplaceChild(Expression oldChild, Expression newChild)
		{
			if(numerator == denominator)
			{
				Parent?.ReplaceChild(this, 1);
				return;
			}
			Expression copy = newChild.Copy();
			if(Numerator == oldChild)
			{
				Numerator = copy;
			}
			else if(Denominator == oldChild)
			{
				Denominator = copy;
			}
			copy.Parent = this;
		}

		public override void RemoveChild(Expression child)
		{
			if(Numerator.Equals(child))
			{
				Numerator = null;
			}
			else if(Denominator.Equals(child))
			{
				Denominator = null;
			}
		}

		public override Expression Multiply(Expression factor)
		{
			Division copy = Copy() as Division;
			if(copy.denominator.Equals(factor))
			{
				Parent?.ReplaceChild(this, copy.numerator);
				return copy.numerator;
			}
			else if(copy.denominator is Product && (copy.denominator as Product).Contains(factor))
			{
				(copy.denominator as Product).RemoveChild(factor);
			}
			else
			{
				copy.numerator.Multiply(factor);
			}
			return copy;
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

		public override bool ContainsFraction()
		{
			return true;
		}

		public override bool IsFinal()
		{
			return numerator is ValueExpression && denominator is ValueExpression;
		}

		public override Expression ToStandardForm()
		{ // Handle Double Fraction
			Division copy = Copy() as Division;
			while(copy.numerator.ContainsFraction() || copy.denominator.ContainsFraction())
			{
				if(copy.numerator is Division)
				{
					copy.denominator = copy.denominator.Multiply((copy.numerator as Division).denominator);
					copy.Numerator = (copy.numerator as Division).numerator;
				}
				if(copy.denominator is Division)
				{
					copy.numerator = copy.numerator.Multiply((copy.denominator as Division).denominator);
					copy.Denominator = (copy.denominator as Division).numerator;
				}
				Expression test = copy.numerator.ToStandardForm();
				if(test != null) copy.numerator = test;
				test = copy.denominator.ToStandardForm();
				if(test != null) copy.denominator = test;
				List<Expression> dens = new List<Expression>();
				if(copy.numerator is Product)
				{
					dens = (copy.numerator as Product).GetDenominators(dens);
				}
				else if(copy.numerator is Sum)
				{
					dens = (copy.numerator as Sum).GetDenominators(dens);
				}
				if(copy.denominator is Product)
				{
					dens = (copy.denominator as Product).GetDenominators(dens);
				}
				else if(copy.denominator is Sum)
				{
					dens = (copy.denominator as Sum).GetDenominators(dens);
				}
				foreach(Expression factor in dens)
				{
					copy.numerator = copy.numerator.Multiply(factor);
					copy.denominator = copy.denominator.Multiply(factor);
				}
			}
			return copy;
		}

		public override List<Expression> GetDenominators(List<Expression> result = null)
		{ // Copies necessary?
			List<Expression> newResult;
			if(result == null)
				newResult = new List<Expression>();
			else
				newResult = result;
			return newResult;
		}

		public override Expression Copy()
		{
			Division copy = new Division
			{
				numerator = numerator,
				denominator = denominator
			};
			return copy;
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
		
		public override Expression Multiply(Expression factor)
		{
			if(this.Equals(1))
			{
				Parent?.ReplaceChild(this, factor);
				return factor;
			}
			else
			{
				Product p = new Product();
				Parent?.ReplaceChild(this, p);
				p.AddFactor(factor);
				p.AddFactor(this);
				return p;
			}
		}

		public override bool Equals(object other)
		{
			if(other is int)
			{
				if(value == null)
				{
					return false;
				}
				return value.Equals(other.ToString());
			}
			if(!(other is ValueExpression)) return false;
			if(value == null && (other as ValueExpression).value == null)
			{
				return component == (other as ValueExpression).component;
			}
			if(value == null || (other as ValueExpression).value == null)
			{
				return false;
			}
			return value.Equals((other as ValueExpression).value);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			return false;
		}

		public override bool ContainsFraction()
		{
			return false;
		}

		public override bool IsFinal()
		{
			return true;
		}

		public override Expression ToStandardForm()
		{
			return this;
		}

		public override List<Expression> GetDenominators(List<Expression> result = null)
		{
			List<Expression> newResult;
			if(result == null)
				newResult = new List<Expression>();
			else
				newResult = result;
			return newResult;
		}

		public override Expression Copy()
		{
			ValueExpression result = new ValueExpression(component)
			{
				value = this.value
			};
			return result;
		}
	}
}
