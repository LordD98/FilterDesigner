using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterDesigner
{
	public abstract class Expression
	{
		public Expression Parent { get; set; }	// Try to remove?

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

		//public abstract Expression Add(Expression summand);

		public abstract Expression Copy();

		public override string ToString()
		{
			return Evaluate();
		}

		public static Expression operator +(Expression exp1, Expression exp2)
		{
			if(exp1 is Sum || exp2 is Sum)
			{
				if(exp1 is Sum && exp2 is Sum) return (exp1 as Sum).Merge(exp2 as Sum);
				Expression copy = null;
				if(exp1 is Sum)
				{
					copy = exp1.Copy();
					(copy as Sum).AddSummand(exp2);
				}
				if(exp2 is Sum)
				{
					copy = exp2.Copy();
					(copy as Sum).AddSummand(exp1);
				}
				return copy;
			}
			else
			{
				return new Sum(exp1, exp2);
			}
		}

		public static Expression operator *(Expression exp1, Expression exp2)
		{
			if(exp1 is Product || exp2 is Product)
			{
				if(exp1 is Product && exp2 is Product) return (exp1 as Product).Merge(exp2 as Product);
				Expression copy = null;
				if(exp1 is Product)
				{
					copy = exp1.Copy();
					(copy as Product).AddFactor(exp2);
				}
				if(exp2 is Product)
				{
					copy = exp2.Copy();
					(copy as Product).AddFactor(exp1);
				}
				return copy;
			}
			else
			{
				return new Product(exp1, exp2);
			}
		}

		public static Expression operator /(Expression exp1, Expression exp2)
		{
			return new Division(exp1.Copy(), exp2.Copy());
		}
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
			this.summands = new List<Expression>();
			foreach(Expression exp in summands)
			{
				this.summands.Add(exp.Copy());
			}
			//CleanUp();
		}

		public Sum(params Expression[] terms)
		{
			summands = new List<Expression>();
			for(int i = 0; i < terms.Length; i++)
			{
				AddSummand(terms[i].Copy());
			}
			//CleanUp();
		}

		public Expression Merge(List<Expression> newSummands)
		{
			Sum copy = Copy() as Sum;
			foreach(Expression exp in newSummands)
			{
				copy.AddSummand(exp);
			}
			//CleanUp();
			return copy;
		}

		public Expression Merge(Sum sum)
		{
			Sum copy = Copy() as Sum;
			foreach(Expression exp in sum.summands)
			{
				copy.AddSummand(exp);
			}
			//CleanUp();
			return copy;
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
			//CleanUp();
		}

		public override void ReplaceChild(Expression oldChild, Expression newChild)
		{
			int index = summands.IndexOf(oldChild);
			Expression copy = newChild.Copy();
			if(index == -1)
			{
				summands.Add(copy);
			}
			else
			{
				summands[index] = copy;
			}
			copy.Parent = this;
		}

		public override void RemoveChild(Expression child)
		{
			summands.Remove(child);
		}

		public override Expression Multiply(Expression factor)
		{
			Sum copy = Copy() as Sum;
			for(int i = 0; i < copy.summands.Count; i++)
			{
				copy.summands[i] = copy.summands[i].Multiply(factor);
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

		public override Expression ToStandardForm()
		{ // Standardform is a sum of final products, divisions & valueexpressions
		  //if(ContainsFraction()) return null;
			Sum result = Copy() as Sum;
			do
			{
				for(int i = 0; i < result.summands.Count; i++)
				{
					result.summands[i] = result.summands[i].ToStandardForm();
				}
				while(result.summands.Any(s => s is Sum))
				{
					Sum sum = result.summands.First(s => s is Sum) as Sum;
					result.RemoveChild(sum);
					result = result.Merge(sum) as Sum;
				}
				List<Product> lp = result.summands.Where(s => s is Product && !s.IsFinal()).Cast<Product>().ToList();
				for(int i = 0; i < lp.Count; i++)
				{ // Product contains Sums,Products&ValueExpressions
					Product stProd = lp[i].ToStandardForm() as Product;
					result.ReplaceChild(lp[i], stProd.ToSum());
				}
			} while(!result.summands.All(e => (e is Product || e is ValueExpression || e is Division)));
			result.Parent = Parent;
			return result;
		}

		public override bool Equals(object other)
		{
			if(!(other is Sum)) return false;
			if(summands.Count != (other as Sum).summands.Count) return false;
			//(other as Sum).summands.All(summands.Contains)
			List<int> visitedIndices = new List<int>();
			for(int i = 0; i < summands.Count; i++)
			{
				int newIndex = -1;
				do
				{
					newIndex++;
					newIndex = (other as Sum).summands.IndexOf(summands[i], newIndex);
					if(newIndex == -1) return false;
				}
				while(visitedIndices.Contains(newIndex));
				visitedIndices.Add(newIndex);
			}
			return true;
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
				Expression subCopy = exp.Copy();
				subCopy.Parent = this;
				copy.summands.Add(subCopy);
			}
			return copy;
		}

		public void CleanUp() //Necessary?
		{
			while(summands.Any(s => s is Sum))
			{
				foreach(Expression exp in (summands.First(s => s is Sum) as Sum).summands)
				{
					summands.Add(exp.Copy());
					exp.Parent = this;
				}
			}
			foreach(Sum sum in summands.Where(s => s is Sum))
			{
				foreach(Expression exp in sum.summands)
				{
					summands.Add(exp.Copy());
				}
			}
			List<Expression> summandsToRemove = new List<Expression>();
			foreach(Expression exp in summands)
			{
				if(exp.Equals(0))
				{
					summandsToRemove.Add(exp);
				}
			}
			summandsToRemove.ForEach(f => summands.Remove(f));
			if(summands.Count == 0)
			{
				summands.Add(0);
			}
			if(summands.Count == 1)  // Careful, don't have references to this from outside
			{
				Parent?.ReplaceChild(this, summands[0]);
			}
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
		
		public Product(params Expression[] terms)
		{
			factors = new List<Expression>();
			for(int i = 0; i < terms.Length; i++)
			{
				AddFactor(terms[i]);
			}
		}
		
		public Expression Merge(List<Expression> newFactors)
		{
			Product copy = Copy() as Product;
			foreach(Expression exp in newFactors)
			{
				copy.AddFactor(exp);
			}
			CleanUp();
			return copy;
		}

		public Expression Merge(Product prod)
		{
			Product copy = Copy() as Product;
			foreach(Expression exp in prod.factors)
			{
				copy.AddFactor(exp);
			}
			CleanUp();
			return copy;
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
					if(!result.Equals("")) result += "*";
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
			//if(ContainsFraction()) return null;
			while(!result.factors.All(e => (e is Sum || e is ValueExpression || e is Division)))
			{
				while(result.factors.Any(s => s is Product))
				{
					Product product = result.factors.First(s => s is Product) as Product;
					result.RemoveChild(product);
					result = result.Merge(product) as Product;
				}
				foreach(Sum sum in factors.Where(s => s is Sum))
				{ // Product contains Sums,Products&ValueExpressions
					sum.ToStandardForm();
				}
			}
			result.Parent = Parent;
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
			List<int> visitedIndices = new List<int>();
			for(int i = 0; i < factors.Count; i++)
			{
				int newIndex = -1;
				do
				{
					newIndex++;
					newIndex = (other as Product).factors.IndexOf(factors[i], newIndex);
					if(newIndex == -1) return false;
				}
				while(visitedIndices.Contains(newIndex));
				visitedIndices.Add(newIndex);
			}
			return true;
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

		public override Expression Copy()
		{
			Product copy = new Product();
			foreach(Expression exp in factors)
			{
				Expression subCopy = exp.Copy();
				subCopy.Parent = this;
				copy.factors.Add(subCopy);
			}
			return copy;
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

		public Division() { }

		public Division(Expression num, Expression den)
		{
			Numerator = num;
			Denominator = den;
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
			if(numerator.Equals(denominator))
			{
				Parent?.ReplaceChild(this, 1);
				return;
			}
			Expression copy = newChild.Copy();
			if(numerator.Equals(oldChild))
			{
				Numerator = copy;
			}
			else if(denominator.Equals(oldChild))
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
				copy.numerator = copy.numerator.ToStandardForm();
				copy.denominator = copy.denominator.ToStandardForm();
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
			copy.numerator.Parent = this;
			copy.denominator.Parent = this;
			copy.Parent = Parent;
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
				numerator = numerator.Copy(),
				denominator = denominator.Copy()
			};
			copy.numerator.Parent = this;
			copy.denominator.Parent = this;
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

		public ValueExpression(double val)
		{
			value = val.ToString();
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
