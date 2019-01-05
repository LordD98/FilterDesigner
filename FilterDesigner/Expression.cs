using System;
using System.Collections.Generic;
using System.Linq;

namespace FilterDesigner
{
	public abstract class Expression
	{
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

		public static S_Block S
		{
			get
			{
				return new S_Block();
			}
		}

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

		public abstract Expression ReplaceChild(Expression oldChild, Expression newChild);

		public abstract Expression RemoveChild(Expression child);

		public override bool Equals(object other)
		{
			if(other == null) return false;
			Expression exp1 = this;
			Expression exp2 = null;
			if(other is int)
			{
				exp2 = new ConstExpression((int)other);
			}
			else if(other is double)
			{
				exp2 = new ConstExpression((double)other);
			}
			else if(other is Expression)
			{
				exp2 = other as Expression;
			}
			else return false;
			//if(value == null)
			//{
			//	return false;
			//}
			//return value.Equals(other.ToString());
			exp1 = exp1.Unpack();
			exp2 = exp2.Unpack();
			exp1 = exp1.ToStandardForm();
			exp2 = exp2.ToStandardForm();

			if(exp1 is Product)
			{
				exp1 = (exp1 as Product).ToSum();
			}
			if(exp2 is Product)
			{
				exp2 = (exp2 as Product).ToSum();
			}

			if(exp1 is Division && exp2 is Division)
			{
				Expression e1 = (exp1 as Division).Numerator * (exp2 as Division).Denominator;
				Expression e2 = (exp2 as Division).Numerator * (exp1 as Division).Denominator;
				return e1.Equals(e2);
			}
			else if(exp1 is Division)
			{
				Expression e1 = (exp1 as Division).Numerator;
				Expression e2 = (exp1 as Division).Denominator * exp2;
				return e1.Equals(e2);
			}
			else if(exp2 is Division)
			{
				Expression e1 = (exp2 as Division).Denominator * exp1;
				Expression e2 = (exp2 as Division).Numerator;
				return e1.Equals(e2);
			}
			else if(exp1 is ValueExpression && exp2 is ValueExpression)
			{
				return (exp1 as ValueExpression).Equal(exp2 as ValueExpression);
			}
			else if(exp1 is Product && exp2 is ValueExpression)
			{
				return (exp1 as Product).Equal(exp2 as ValueExpression);
			}
			else if(exp2 is Product && exp1 is ValueExpression)
			{
				return (exp2 as Product).Equal(exp1 as ValueExpression);
			}
			else if(exp1.GetType() != exp2.GetType()) return false;

			if(exp1 is Sum)
			{
				return (exp1 as Sum).Equal(exp2 as Sum);
			}
			else if(exp1 is Product)
			{
				return (exp1 as Product).Equal(exp2 as Product);
			}
			//else if(exp1 is Division)
			//{
			//	return (exp1 as Division).Equal(exp2 as Division);
			//}
			else return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public abstract bool Contains(Expression other);

		public abstract bool Contains(Func<Expression, bool> predicate);

		public abstract List<Expression> GetDenominators(List<Expression> result=null);

		public virtual Expression Multiply(Expression factor)
		{
			Product p = new Product();
			p.AddFactor(factor.Copy());
			p.AddFactor(this);
			return p;
		}
		
		public abstract Expression Copy();

		public abstract Expression Unpack();

		public override string ToString()
		{
			return Evaluate();
		}

		public static implicit operator Expression(double value)
		{
			return new ConstExpression(value);
		}

		public static Sum operator +(Expression exp1, Expression exp2)
		{
			if(exp1 is Sum || exp2 is Sum)
			{
				if(exp1 is Sum && exp2 is Sum) return (exp1 as Sum).Merge(exp2 as Sum);
				Sum copy = null;
				if(exp1 is Sum)
				{
					copy = exp1.Copy() as Sum;
					copy.AddSummand(exp2);
				}
				if(exp2 is Sum)
				{
					copy = exp2.Copy() as Sum;
					copy.AddSummand(exp1);
				}
				return copy;
			}
			else
			{
				return new Sum(exp1, exp2);
			}
		}

		public static Product operator *(Expression exp1, Expression exp2)
		{
			if(exp1 is Product || exp2 is Product)
			{
				if(exp1 is Product && exp2 is Product) return (exp1 as Product).Merge(exp2 as Product);
				Product copy = null;
				if(exp1 is Product)
				{
					copy = exp1.Copy() as Product;
					(copy as Product).AddFactor(exp2);
				}
				if(exp2 is Product)
				{
					copy = exp2.Copy() as Product;
					(copy as Product).AddFactor(exp1);
				}
				return copy;
			}
			else
			{
				return new Product(exp1, exp2);
			}
		}

		public static Division operator /(Expression exp1, Expression exp2)
		{
			return new Division(exp1.Copy(), exp2.Copy());
		}
	}

	public class Sum : Expression
	{
		public List<Expression> Summands { get; private set; }

		public Sum()
		{
			Summands = new List<Expression>();
		}

		public Sum(List<Expression> summands)
		{
			this.Summands = new List<Expression>();
			foreach(Expression exp in summands)
			{
				this.Summands.Add(exp.Copy());
			}
			//CleanUp();
		}

		public Sum(params Expression[] terms)
		{
			Summands = new List<Expression>();
			for(int i = 0; i < terms.Length; i++)
			{
				AddSummand(terms[i].Copy());
			}
			//CleanUp();
		}

		public Sum Merge(List<Expression> newSummands)
		{
			Sum copy = Copy() as Sum;
			foreach(Expression exp in newSummands)
			{
				copy.AddSummand(exp);
			}
			return copy;
		}

		public Sum Merge(Sum sum)
		{
			Sum copy = Copy() as Sum;
			foreach(Expression exp in sum.Summands)
			{
				copy.AddSummand(exp);
			}
			return copy;
		}

		public override string Evaluate()
		{
			if(Summands.Count == 0) return "0";
			string result = "";
			foreach(Expression exp in Summands)
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
			foreach(Expression summand in Summands)
			{
				temp = temp + summand.EvaluateImpedance(frequency);
			}
			return temp;
		}

		public void AddSummand(Expression exp)
		{
			if(exp is Sum)
			{
				foreach(Expression e in (exp as Sum).Summands)
				{
					AddSummand(e.Copy());
				}
			}
			else
			{
				Expression copy = exp.Copy();
				Summands.Add(copy);
			}
		}

		public override Expression ReplaceChild(Expression oldChild, Expression newChild)
		{
			Sum copy = Copy() as Sum;
			int index = Summands.IndexOf(oldChild);
			Expression childCopy = newChild.Copy();
			if(index == -1)
			{
				copy.Summands.Add(childCopy);
			}
			else
			{
				copy.Summands[index] = childCopy;
			}
			return copy;
		}

		public override Expression RemoveChild(Expression child)
		{
			Sum copy = Copy() as Sum;
			copy.Summands.Remove(child);
			return copy;
		}

		public override Expression Multiply(Expression factor)
		{
			Sum copy = Copy() as Sum;
			for(int i = 0; i < copy.Summands.Count; i++)
			{
				copy.Summands[i] = copy.Summands[i].Multiply(factor);
			}
			return copy;
		}

		public static Sum MultiplySums(Sum sum1, Sum sum2)
		{
			Sum result = new Sum();
			for(int i = 0; i < sum1.Summands.Count; i++)
			{
				for(int j = 0; j < sum2.Summands.Count; j++)
				{
					result.AddSummand(sum1.Summands[i].Multiply(sum2.Summands[j]));
				}
			}
			return result;
		}

		public override Expression ToCommonDenominator()
		{
			List<Expression> denominators = new List<Expression>();
			Sum Numerator = new Sum();
			Product denominator = new Product();
			foreach(Division summand in Summands.Where(s => s is Division))
			{
				denominators.Add(summand.Denominator);
				denominator.AddFactor(summand.Denominator);
			}
			foreach(Expression summand in Summands)
			{
				Product product = new Product(denominators);
				if(summand is Division)
				{
					product = product.RemoveChild((summand as Division).Denominator) as Product;
					product.AddFactor((summand as Division).Numerator);
				}
				else
				{
					product.AddFactor(summand);
				}
				Numerator.AddSummand(product);
			}
			Division result = new Division
			{
				Numerator = Numerator,
				Denominator = denominator
			};
			return result;
		}

		public override Expression ToStandardForm()
		{ // Standardform is a sum of final products, divisions & valueexpressions
			//if(ContainsFraction()) return ToCommonDenominator().ToStandardForm();
			Sum result = Copy() as Sum;
			do
			{
				for(int i = 0; i < result.Summands.Count; i++)
				{
					result.Summands[i] = result.Summands[i].Unpack().ToStandardForm();
				}
				while(result.Summands.Any(s => s is Sum))
				{
					Sum sum = result.Summands.First(s => s is Sum) as Sum;
					result = result.RemoveChild(sum) as Sum;
					result = result.Merge(sum) as Sum;
				}
				List<Product> lp = result.Summands.Where(s => s is Product && !s.IsFinal()).Cast<Product>().ToList();
				for(int i = 0; i < lp.Count; i++)
				{ // Product contains Sums,Products&ValueExpressions
					Expression stExp = lp[i].ToStandardForm();
					if(stExp is Sum)
					{
						result = result.ReplaceChild(lp[i], stExp as Sum) as Sum;
					}
					else if(stExp is Product)
					{
						result = result.ReplaceChild(lp[i], (stExp as Product).ToSum()) as Sum;
					}
				}
			} while(!result.Summands.All(e => (e is Product || e is ValueExpression || e is Division)));
			if(result.Summands.Count < 2) return result.Unpack();
			int[] summandPowers = new int[result.Summands.Count];
			for(int i = 0; i<result.Summands.Count; i++)
			{
				if(result.Summands[i] is Product)
				{
					if((result.Summands[i] as Product).Factors[0] is ConstExpression && (result.Summands[i] as Product).Factors.Count > 1)
					{
						if((result.Summands[i] as Product).Factors[1] is S_Block)
						{
							summandPowers[i] = ((result.Summands[i] as Product).Factors[1] as S_Block).Exponent;
						}
					}
					else if((result.Summands[i] as Product).Factors[0] is S_Block)
					{
						summandPowers[i] = ((result.Summands[i] as Product).Factors[0] as S_Block).Exponent;
					}
				}
				else if(result.Summands[i] is S_Block)
				{
					summandPowers[i] = (result.Summands[i] as S_Block).Exponent;
				}
				else
				{
					summandPowers[i] = 0;
				}
			}
			List<List<Expression>> listListSTerms = new List<List<Expression>>();
			int smallestPower = summandPowers.Min()-1;
			for(int i = 0; i<summandPowers.Length; i++)
			{
				smallestPower = summandPowers.Min(power => power>smallestPower ? power : Int32.MaxValue);
				if(smallestPower == Int32.MaxValue) break;
				listListSTerms.Add(new List<Expression>());
				for(int j = 0; j<summandPowers.Length; j++)
				{
					if(smallestPower == summandPowers[j])
					{
						listListSTerms.Last().Add(result.Summands[j]);
					}
				}
			}
			if(listListSTerms.Count == 1)
			{
				if(listListSTerms[0].Count == 1)
				{
					return listListSTerms[0][0].Unpack();
				}
				else
				{
					foreach(Expression exp in listListSTerms[0])
					{
						// Void ausklammern erstellen?
					}
					//return new Product(new S_Block(summandPowers[0]), new Sum(...));
				}
			}
			for(int i = 0; i<summandPowers.Length; i++)
			{

			}
			return result.Unpack();
		}

		public bool Equal(Sum other)
		{
			if(Summands.Count != other.Summands.Count) return false;
			List<int> visitedIndices = new List<int>();
			for(int i = 0; i < Summands.Count; i++)
			{
				int newIndex = -1;
				do
				{
					newIndex++;
					newIndex = other.Summands.IndexOf(Summands[i], newIndex);
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
			return Summands.Contains(other);
		}

		public override bool Contains(Func<Expression, bool> predicate)
		{
			return Summands.Any(predicate);
		}

		public override bool ContainsFraction()
		{
			foreach(Expression expression in Summands)
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
			return Summands.All(s => s is ValueExpression);
		}

		public override List<Expression> GetDenominators(List<Expression> result = null)
		{
			List<Expression> newResult;
			if(result == null)
				newResult = new List<Expression>();
			else
				newResult = result;
			foreach(Expression exp in Summands)
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
			foreach(Expression exp in Summands)
			{
				Expression subCopy = exp.Copy();
				copy.Summands.Add(subCopy);
			}
			return copy;
		}

		public override Expression Unpack()
		{
			if(Summands.Count == 1)
			{
				return Summands[0].Unpack();
			}
			else
			{
				//Product unpacked = Copy() as Sum;
				//for(int i = 0; i < unpacked.summands.Count; i++)
				//{
				//	unpacked.summands[i] = unpacked.summands[i].Unpack();
				//}
				//return unpacked;
				return this;
			}
		}

		/* Necessary?
		public void CleanUp() 
		{
			while(summands.Any(s => s is Sum))
			{
				foreach(Expression exp in (summands.First(s => s is Sum) as Sum).summands)
				{
					summands.Add(exp.Copy());
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
		}
		*/
	}

	public class Product : Expression
	{
		public List<Expression> Factors { get; private set; }

		public Product()
		{
			Factors = new List<Expression>();
		}

		public Product(List<Expression> Factors)
		{
			this.Factors = new List<Expression>(Factors);
		}
		
		public Product(params Expression[] terms)
		{
			Factors = new List<Expression>();
			for(int i = 0; i < terms.Length; i++)
			{
				AddFactor(terms[i]);
			}
		}
		
		public Product Merge(List<Expression> newFactors)
		{
			Product copy = Copy() as Product;
			foreach(Expression exp in newFactors)
			{
				copy.AddFactor(exp);
			}
			return copy;
		}

		public Product Merge(Product prod)
		{
			Product copy = Copy() as Product;
			foreach(Expression exp in prod.Factors)
			{
				copy.AddFactor(exp);
			}
			return copy;
		}
		
		public override string Evaluate()
		{
			string result = "";
			if(Factors.All(f => f.Equals(1))) return "1";
			foreach(Expression exp in Factors)
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
				if(result.Equals("")) return "1";
			}
			return result;
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			Impedance temp = new Impedance(1);
			foreach(Expression factor in Factors)
			{
				temp = temp * factor.EvaluateImpedance(frequency);
			}
			return temp;
		}

		public void AddFactor(Expression exp)
		{
			if(exp is Product)
			{
				foreach(Expression e in (exp as Product).Factors)
				{
					AddFactor(e.Copy());
				}
			}
			else
			{
				Expression copy = exp.Copy();
				Factors.Add(copy);
			}
		}

		public override Expression ReplaceChild(Expression oldChild, Expression newChild)
		{
			Product copy = Copy() as Product;
			int index = Factors.IndexOf(oldChild);
			Expression childCopy = newChild.Copy();
			if(index == -1)
			{
				Factors.Add(childCopy);
			}
			else
			{
				Factors[index] = childCopy;
			}
			return copy;
		}

		public override Expression RemoveChild(Expression child)
		{
			Product copy = Copy() as Product;
			copy.Factors.Remove(child);
			return copy;
		}

		public override Expression Multiply(Expression factor)
		{
			Product copy = Copy() as Product;
			int index = copy.Factors.FindIndex(f => f is Sum && f.GetDenominators().Contains(factor) || f is Division && (f as Division).Denominator.Contains(factor));
			if(index != -1)
			{
				copy.Factors[index] = copy.Factors[index].Multiply(factor);
			}
			else
			{
				copy.AddFactor(factor);
			}
			return copy;
		}

		public override Expression ToCommonDenominator()
		{
			return this;
		}

		public override Expression ToStandardForm()
		{ // Standardform is a product of sums and reduced ValueExpressions
			Product result = Copy() as Product;
			do
			{
				for(int i = 0; i < result.Factors.Count; i++)
				{
					result.Factors[i] = result.Factors[i].Unpack().ToStandardForm();
				}
				while(result.Factors.Any(s => s is Product))
				{
					Product product = result.Factors.First(s => s is Product) as Product;
					result = result.RemoveChild(product) as Product;
					result = result.Merge(product) as Product;
				}
				if(Factors.Count(f => f is ConstExpression) > 1 || Factors.Count(f => f is S_Block) > 1)
				{
					int s_count = 0;
					double const_count = 1;
					List<Expression> newFactors = new List<Expression>();
					for(int i = 0; i < result.Factors.Count; i++)
					{
						if(result.Factors[i] is S_Block)
						{
							s_count += (result.Factors[i] as S_Block).Exponent;
						}
						else if(result.Factors[i] is ConstExpression)
						{
							const_count *= (result.Factors[i] as ConstExpression).Value;
						}
						else
						{
							newFactors.Add(result.Factors[i]);
						}
					}
					if(s_count != 0)
					{
						newFactors.Insert(0, new S_Block(s_count));
					}
					if(const_count != 1)
					{
						newFactors.Insert(0, new ConstExpression(const_count));
					}
					result.Factors = newFactors;
				}
			} while(!result.Factors.All(e => (e is Sum || e is ValueExpression || e is Division)));
			return result.Unpack();
		}

		public Expression ToSum()
		{
			Product copy = Copy() as Product;
			Sum result = new Sum();
			Product productWithoutSums = new Product(copy.Factors.Where(f => f is ValueExpression).ToList());
			if(productWithoutSums.Factors.Count == 0)
			{
				result.AddSummand(1);
			}
			else
			{
				result.AddSummand(productWithoutSums);
			}
			//List<Expression> sumlessFactors = Factors..(f => f is ValueExpression);
			foreach(Sum sum in copy.Factors.Where(f => f is Sum))
			{
				result = Sum.MultiplySums(result, sum);
			}
			return result.Unpack();
		}

		public bool Equal(Product other)
		{
			if(Factors.Count != other.Factors.Count) return false;
			List<int> visitedIndices = new List<int>();
			for(int i = 0; i < Factors.Count; i++)
			{
				int newIndex = -1;
				do
				{
					newIndex++;
					newIndex = other.Factors.IndexOf(Factors[i], newIndex);
					if(newIndex == -1) return false;
				}
				while(visitedIndices.Contains(newIndex));
				visitedIndices.Add(newIndex);
			}
			return true;
		}

		public bool Equal(ValueExpression other)
		{
			if(Factors.Count(f => f is Division || f is Sum || f is Product) != 0) return false;
			int s_count = 0;
			double const_count = 1;
			List<ComponentExpression> componentExpressions = new List<ComponentExpression>();
			for(int i = 0; i < Factors.Count; i++)
			{
				if(Factors[i] is S_Block)
				{
					s_count += (Factors[i] as S_Block).Exponent;
				}
				else if(Factors[i] is ConstExpression)
				{
					const_count *= (Factors[i] as ConstExpression).Value;
				}
				else if(Factors[i] is ComponentExpression)
				{
					componentExpressions.Add(Factors[i] as ComponentExpression);
				}
			}
			if(other is ComponentExpression)
			{
				if(componentExpressions.Count != 1) return false;
				if(s_count != 0) return false;
				if(const_count != 1) return false;
				return componentExpressions[0].Equal(other);
			}
			else if(other is ConstExpression)
			{
				if(componentExpressions.Count != 0) return false;
				if(s_count != 0) return false;
				return (other as ConstExpression).Value == const_count;
			}
			else if(other is S_Block)
			{
				if(componentExpressions.Count != 0) return false;
				if(const_count != 1) return false;
				return (other as S_Block).Exponent == s_count;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			return Factors.Contains(other);
		}

		public override bool Contains(Func<Expression, bool> predicate)
		{
			return Factors.Any(predicate);
		}

		public override bool ContainsFraction()
		{
			foreach(Expression expression in Factors)
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
			return Factors.All(f => f is ValueExpression);
		}

		public override List<Expression> GetDenominators(List<Expression> result = null)
		{
			List<Expression> newResult;
			if(result == null)
				newResult = new List<Expression>();
			else
				newResult = result;
			foreach(Expression exp in Factors)
			{
				if(exp is Division)
				{
					Expression newDen = (exp as Division).Denominator;
					newResult.Add(newDen);		// Since this is a product, the denominators can be added multiple times
				}
				else if(exp is Sum)
				{
					newResult = exp.GetDenominators(newResult);
				}
			}
			return newResult;
		}

		public override Expression Copy()
		{
			Product copy = new Product();
			foreach(Expression exp in Factors)
			{
				Expression subCopy = exp.Copy();
				copy.Factors.Add(subCopy);
			}
			return copy;
		}

		public override Expression Unpack()
		{
			if(Factors.Count(f => !f.Equals(1)) == 1)
			{
				return Factors.Find(f => !f.Equals(1)).Unpack();
			}
			else
			{
				//Product unpacked = Copy() as Product;
				//for(int i = 0; i < unpacked.Factors.Count; i++)
				//{
				//	unpacked.Factors[i] = unpacked.Factors[i].Unpack();
				//}
				//return unpacked;
				return this;
			}
		}
	}

	public class Division : Expression
	{
		public Expression Numerator { get; set; }
		public Expression Denominator { get; set; }

		public Division() { }

		public Division(Expression num, Expression den)
		{
			Numerator = num;
			Denominator = den;
		}

		public override string Evaluate()
		{
			string result = "";
			string tempRes = Numerator.Evaluate();
			if(tempRes.Length > 1)
			{
				result += "(" + tempRes + ")";
			}
			else
			{
				result += tempRes;
			}
			tempRes = Denominator.Evaluate();
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
			return this;
		}

		public override Expression ReplaceChild(Expression oldChild, Expression newChild)
		{
			Division copy = Copy() as Division;
			if(Numerator.Equals(Denominator))
			{
				return 1;
			}
			Expression childCopy = newChild.Copy();
			if(Numerator.Equals(oldChild))
			{
				copy.Numerator = childCopy;
			}
			else if(Denominator.Equals(oldChild))
			{
				copy.Denominator = childCopy;
			}
			return copy;
		}

		public override Expression RemoveChild(Expression child)
		{
			Division copy = Copy() as Division;
			if(Numerator.Equals(child))
			{
				copy.Numerator = null;
			}
			else if(Denominator.Equals(child))
			{
				copy.Denominator = null;
			}
			return copy;
		}

		public override Expression Multiply(Expression factor)
		{
			Division copy = Copy() as Division;
			if(copy.Denominator.Equals(factor))
			{
				return copy.Numerator;
			}
			else if(copy.Denominator is Product && (copy.Denominator as Product).Contains(factor))
			{
				(copy.Denominator as Product).RemoveChild(factor);
			}
			else
			{
				copy.Numerator = copy.Numerator.Multiply(factor);
			}
			return copy;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			return Numerator.Equals(other) || Denominator.Equals(other);
		}

		public override bool Contains(Func<Expression, bool> predicate)
		{
			return Numerator.Contains(predicate) || Denominator.Contains(predicate);
		}

		public override bool ContainsFraction()
		{
			return true;
		}

		public override bool IsFinal()
		{
			return Numerator is ValueExpression && Denominator is ValueExpression;
		}

		public override Expression ToStandardForm()
		{ // Handle Double Fraction
			Division copy = Copy() as Division;
			do
			{
				copy.Numerator = copy.Numerator.Unpack().ToStandardForm();
				copy.Denominator = copy.Denominator.Unpack().ToStandardForm();
				List<Expression> numFactors;
				List<Expression> FactorsToRemove = new List<Expression>();
				if(copy.Numerator is Product)
				{
					numFactors = (copy.Numerator as Product).Factors;
				}
				else
				{
					numFactors = new List<Expression> { copy.Numerator };
				}
				if(copy.Denominator is Product)
				{
					Product denProd = copy.Denominator as Product;
					while(denProd.Factors.Any(numFactors.Contains))
					{
						Expression commonFactor = denProd.Factors.First(numFactors.Contains);
						denProd = denProd.RemoveChild(commonFactor) as Product;
						numFactors.Remove(commonFactor);
					}
					if(numFactors.Count == 0)
					{
						copy.Numerator = 1;
					}
					else if(numFactors.Count == 1)
					{
						copy.Numerator = numFactors[0];
					}
					else
					{
						copy.Numerator = new Product(numFactors);
					}

					if(denProd.Factors.Count == 0)
					{
						return copy.Numerator.ToStandardForm();
					}
					else if(denProd.Factors.Count == 1)
					{
						copy.Denominator = denProd.Factors[0].Copy();
					}
					else
					{
						copy.Denominator = denProd;
					}
				}
				else if(numFactors.Contains(copy.Denominator))
				{
					numFactors.Remove(copy.Denominator);
					if(numFactors.Count == 0)
					{
						return 1;
					}
					else if(numFactors.Count == 1)
					{
						return numFactors[0].ToStandardForm();
					}
					else
					{
						return new Product(numFactors);
					}
				}
				if(copy.Denominator.Equals(1)) return Numerator.ToStandardForm();

				if(copy.Numerator is Division && copy.Denominator is Division)
				{
					Division oldNum = copy.Numerator as Division;
					copy.Numerator = oldNum.Numerator * (copy.Denominator as Division).Denominator;
					copy.Denominator = oldNum.Denominator * (copy.Denominator as Division).Numerator;
				}
				else if(copy.Numerator is Division)
				{
					copy.Denominator = copy.Denominator.Multiply((copy.Numerator as Division).Denominator);
					copy.Numerator = (copy.Numerator as Division).Numerator;
				}
				else if(copy.Denominator is Division)
				{
					copy.Numerator = copy.Numerator.Multiply((copy.Denominator as Division).Denominator);
					copy.Denominator = (copy.Denominator as Division).Numerator;
				}
				List<Expression> dens = new List<Expression>();
				if(copy.Numerator is Product)
				{
					dens = (copy.Numerator as Product).GetDenominators(dens);
				}
				else if(copy.Numerator is Sum)
				{
					dens = (copy.Numerator as Sum).GetDenominators(dens);
				}
				if(copy.Denominator is Product)
				{
					dens = (copy.Denominator as Product).GetDenominators(dens);
				}
				else if(copy.Denominator is Sum)
				{
					dens = (copy.Denominator as Sum).GetDenominators(dens);
				}
				foreach(Expression factor in dens)
				{
					copy.Numerator = copy.Numerator.Multiply(factor);
					copy.Denominator = copy.Denominator.Multiply(factor);
				}
			} while(copy.Numerator.ContainsFraction() || copy.Denominator.ContainsFraction());
			if(copy.Denominator.Equals(1))
			{
				return copy.Numerator.ToStandardForm();
			}
			if(copy.Numerator is Product)
			{
				copy.Numerator = (copy.Numerator as Product).ToSum();
			}
			if(copy.Denominator is Product)
			{
				copy.Denominator = (copy.Denominator as Product).ToSum();
			}
			if(copy.Denominator.Equals(copy.Numerator))
			{
				return 1;
			}
			return copy.Unpack();
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
			return new Division(Numerator.Copy(), Denominator.Copy());
		}

		public override Expression Unpack()
		{
			if(Denominator.Equals(1))
				return Numerator.Unpack();
			else
				return new Division(Numerator.Unpack(), Denominator.Unpack());
		}

	}

	public abstract class ValueExpression : Expression
	{
		public sealed override Expression ToCommonDenominator()
		{
			return this;
		}

		public sealed override Expression ReplaceChild(Expression oldChild, Expression newChild)
		{
			return this;
		}

		public sealed override Expression RemoveChild(Expression child)
		{
			return this;
		}

		public sealed override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public sealed override bool Contains(Expression other)
		{
			return false;
		}

		public sealed override bool Contains(Func<Expression, bool> predicate)
		{
			return predicate(this);
		}

		public sealed override bool ContainsFraction()
		{
			return false;
		}

		public sealed override bool IsFinal()
		{
			return true;
		}

		public override Expression ToStandardForm()
		{
			return this;
		}

		public sealed override Expression Unpack()
		{
			return this;
		}

		public override Expression Multiply(Expression factor)
		{
			if(Is1())
			{
				return factor;
			}
			else if(!(factor is ValueExpression))
			{
				return factor.Multiply(this);
			}
			else if(this is ConstExpression && factor is ConstExpression)
			{
				return new ConstExpression((this as ConstExpression).Value * (factor as ConstExpression).Value);
			}
			else if(this is S_Block && factor is S_Block)
			{
				return new S_Block((this as S_Block).Exponent + (factor as S_Block).Exponent);
			}
			else
			{
				Product result = new Product();
				result.AddFactor(factor);
				result.AddFactor(this);
				return result;
			}
		}

		public bool Equal(ValueExpression other)
		{
			if(other.Is1() && Is1())
			{
				return true;
			}
			else if(GetType() != other.GetType())
			{
				return false;
			}
			else if(this is ConstExpression)
			{
				return (this as ConstExpression).Value == (other as ConstExpression).Value;
			}
			else if(this is S_Block)
			{
				return (this as S_Block).Exponent == (other as S_Block).Exponent;
			}
			else if(this is ComponentExpression)
			{
				ComponentExpression thisExp = this as ComponentExpression;
				ComponentExpression otherExp = other as ComponentExpression;
				if(thisExp.Value == null && otherExp.Value == null)
				{
					return thisExp.component == otherExp.component;
				}
				if(thisExp.Value == null || otherExp.Value == null)
				{
					return false;
				}
				return thisExp.Value.Equals(otherExp.Value);
			}
			return false;
		}

		public sealed override List<Expression> GetDenominators(List<Expression> result = null)
		{
			List<Expression> newResult;
			if(result == null)
				newResult = new List<Expression>();
			else
				newResult = result;
			return newResult;
		}

		public abstract bool Is1();
	}

	public class ComponentExpression : ValueExpression
	{
		// Capacitors and Inductors both result in s*Value
		// This allow for more flexible calculation
		public string Value { get; set; }
		public Component component;

		public ComponentExpression(Component comp)
		{
			component = comp;
		}

		public ComponentExpression(string val)
		{
			Value = val;
		}

		public override string Evaluate()
		{
			if(Value != null)
				return Value;
			else
				return component.Name;
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			if(component is Resistor)
			{
				return (component as Resistor).Resistance;
			}
			else if(component is Capacitor)
			{
				return (component as Capacitor).Capacitance;
			}
			else if(component is Inductor)
			{
				return (component as Inductor).Inductance;
			}
			return null;
		}
		
		public override Expression Copy()
		{
			return new ComponentExpression(component);
		}

		public override bool Is1()
		{
			return false;
		}
	}

	public class ConstExpression : ValueExpression
	{
		public double Value;
		
		public ConstExpression(double val)
		{
			Value = val;
		}

		public override string Evaluate()
		{
			return Value.ToString();
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			return new Impedance(Value);
		}
		
		public override Expression Copy()
		{
			return new ConstExpression(Value);
		}

		public override bool Is1()
		{
			return Value == 1;
		}
	}

	public class S_Block : ValueExpression
	{
		public int Exponent { get; set; }

		public S_Block(int exponent = 1)
		{
			Exponent = exponent;
		}

		public override string Evaluate()
		{
			if(Exponent <= 0)
				return "";
			else if(Exponent == 1)
				return "s";
			else
				return $"s^{Exponent}";
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			return new Impedance(0, 2 * Math.PI * frequency);
		}
		
		public override Expression ToStandardForm()
		{
			if(Exponent == 0)
				return new ConstExpression(1);
			else
				return this;
		}

		public override Expression Copy()
		{
			return new S_Block(Exponent);
		}

		public override bool Is1()
		{
			return Exponent == 0;
		}
	}
}
