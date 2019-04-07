using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FilterDesigner
{
	public enum Component_Order { RLC, RCL };
	public enum Product_Order { _2SC, _S2C, _2CS };
	public enum Sum_Order { S2_S, S_S2 };

	public abstract class Expression
	{
		public static Component_Order ComponentOrder = Component_Order.RLC;
		public static Product_Order ProductOrder = Product_Order._2SC;
		public static Sum_Order SumOrder = Sum_Order.S2_S;

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

		public abstract Impedance EvaluateImpedance(double frequency);
		public abstract string Evaluate();
		public abstract string EvaluateLaTeX();
		public abstract Expression EvaluateToConst();

		public abstract bool ContainsFraction();

		public abstract bool IsFinal();
		
		public abstract Expression ToCommonDenominator();

		public abstract Expression ToStandardForm(); // bool topLevel = true

		public abstract Expression ReplaceChild(Expression oldChild, Expression newChild);

		public abstract Expression RemoveChild(Expression child);

		public override bool Equals(object other)
		{
			if(other == null) return false;
			Expression exp1 = this;
			Expression exp2 = null;
			if(other is int)
			{
				if((int)other == 1 && this is ValueExpression) return (this as ValueExpression).Is1();
				exp2 = new ConstExpression((int)other);
			}
			else if(other is double)
			{
				if((double)other == 1.0 && this is ValueExpression) return (this as ValueExpression).Is1();
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
			
			// Maybe Immediately deal with contained Fractions? (To speed up the comparison) 

			exp1 = exp1.Unpack();
			exp2 = exp2.Unpack();
			if(exp1 is Product)
			{
				exp1 = exp1.ToCommonDenominator().ToStandardForm();
			}
			else
			{
				exp1 = exp1.ToStandardForm();
			}
			if(exp2 is Product)
			{
				exp2 = exp2.ToCommonDenominator().ToStandardForm();
			}
			else
			{
				exp2 = exp2.ToStandardForm();
			}

			if(exp1 is Product)
			{
				exp1 = (exp1 as Product).ToSum().Unpack().ToStandardForm();
			}
			if(exp2 is Product)
			{
				exp2 = (exp2 as Product).ToSum().Unpack().ToStandardForm();
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

		public abstract bool IsConst();

		public abstract bool AllValuesSet();

		public static int Compare(Expression x, Expression y)
		{
			if(!(x is ComponentExpression) && !(y is ComponentExpression))
			{
				return 0;
			}
			else if(!(x is ComponentExpression))
			{
				return -1;
			}
			else if(!(y is ComponentExpression))
			{
				return 1;
			}
			else // both are ComponentExpressions
			{
				string xEval = x.Evaluate();
				string yEval = y.Evaluate();
				if(xEval.Equals("") && yEval.Equals(""))
				{
					return 0;
				}
				else if(xEval.Equals(""))
				{
					return -1;
				}
				else if(yEval.Equals(""))
				{
					return 1;
				}
				else
				{
					if(xEval[0] == yEval[0])
					{
						return 0;
					}
					else if(xEval[0] == 'R')
					{
						return -1;
					}
					else if(xEval[0] == 'C')
					{
						if(Expression.ComponentOrder == Component_Order.RLC)
							return 1;
						else
							return yEval[0] == 'R' ? 1 : -1;
					}
					else // 'L'
					{
						if(Expression.ComponentOrder == Component_Order.RCL)
							return 1;
						else
							return yEval[0] == 'R' ? 1 : -1;
					}
				}
			}
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public abstract bool Contains(Expression other);

		public abstract bool Contains(Func<Expression, bool> predicate);

		public abstract bool DeepContains(Expression other);

		public abstract bool DeepContains(Func<Expression, bool> predicate);

		public abstract bool ContainsFactor(Expression other);

		public abstract List<Expression> GetDenominators(List<Expression> result = null);

		public abstract List<Expression> GetCommonFactors(List<Expression> oldFactors = null);

		public abstract Expression FactorOut(Expression factor);

		public static Expression PolynomialDivision(Sum s1, Expression e2)
		{
			return PolynomialDivision(s1, new Sum(e2));
		}

		public static Expression PolynomialDivision(Sum s1, Sum s2)
		{
			if(s1 == null || s2 == null || s1.Summands.Count == 0 || s2.Summands.Count == 0)
				return null;
			Sum result = new Sum();
			Sum copy = s1.Copy() as Sum;
			Expression anchor = s2.Summands[0];
			int index = 0;
			do
			{
				index = copy.Summands.FindIndex(s => s.ContainsFactor(anchor));
				if(index == -1)
					return null;
				Expression temp = copy.Summands[index].FactorOut(anchor)?.Unpack();
				result.AddSummand(temp);
				temp = s2.Multiply(temp);
				temp = copy.Subtract(temp);
				if(temp is Sum)
				{
					copy = temp as Sum;
				}
				else
				{
					// temp == 0???
					//result.AddSummand(temp.FactorOut(anchor));
					if(temp.Equals(0))
						return result;
					else
						return null;
				}
			} while(copy.Summands.Count != 0);
			return result;
		}

		public virtual Expression Multiply(Expression factor)
		{
			Product p = new Product();
			p.AddFactor(this);
			p.AddFactor(factor.Copy());
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

		public static Expression operator *(Expression exp1, Expression exp2)
		{
			if(exp1.IsConst() && exp1.Equals(1)) return exp2;
			else if(exp2.IsConst() && exp2.Equals(1)) return exp1;
			else if(exp1 is Product || exp2 is Product)
			{
				if(exp1 is Product)
				{
					return (exp1 as Product).Multiply(exp2);
				}
				else if(exp2 is Product)
				{
					return (exp2 as Product).Multiply(exp1);
				}
				Debug.Assert(false);
				return null;
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

		public Sum Expand()
		{
			Sum result = new Sum();
			for(int i = 0; i<Summands.Count; i++)
			{
				if(Summands[i] is Sum || Summands[i] is ValueExpression)
					result.AddSummand(Summands[i]);
				else if(Summands[i] is Product)
					result.AddSummand((Summands[i] as Product).ToSum());
				else Debug.Assert(false);
			}
			Debug.Assert(result.Summands.Count > 0);
			return result;
		}

		public Expression Subtract(Expression exp)
		{
			if(Contains(exp))
				return RemoveChild(exp);
			Sum expanded = Expand();
			if(exp is Product && (exp as Product).ToSum() is Sum)
				exp = (exp as Product).ToSum();
			if(exp is Sum)
			{
				Sum exp2 = (exp as Sum).Expand();
				for(int i = 0; i < exp2.Summands.Count; i++)
				{
					if(!expanded.Contains(exp2.Summands[i]))
						return null;
					Expression temp = expanded.RemoveChild(exp2.Summands[i]);
					if(temp is Sum)
					{
						expanded = temp as Sum;
					}
					else if(i == exp2.Summands.Count-1)
					{
						break;
					}
					else
					{
						Debug.Assert(false);
					}
				}
			}
			else
			{
				expanded.RemoveChild(exp);
			}
			return expanded;
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

		public override string EvaluateLaTeX()
		{
			if(Summands.Count == 0) return "0";
			string result = "";
			foreach(Expression exp in Summands)
			{
				string expEvaluate = exp.EvaluateLaTeX();
				if(expEvaluate.Equals("0")) continue;
				result += "+" + expEvaluate;
			}
			return result.Substring(1);
		}

		public override Expression EvaluateToConst()
		{
			if(!AllValuesSet())
				return null;
			Sum result = new Sum();
			double value = 0;
			foreach(Expression exp in Summands)
			{
				Expression summand = exp.EvaluateToConst();
				if(summand is ConstExpression)   // factor.IsConst()
					value += (summand as ConstExpression).Value;
				else
					result.Summands.Add(exp.EvaluateToConst());
			}
			if(result.Summands.Count == 0)
				return new ConstExpression(value);
			result.Summands.Add(new ConstExpression(value));
			return result;
		}

		public void AddSummand(Expression exp, int index = -1)
		{
			if(index == -1) index = Summands.Count;
			if(exp is Sum)
			{
				foreach(Expression e in (exp as Sum).Summands)
				{
					AddSummand(e.Copy(), index++);
				}
			}
			else
			{
				Expression copy = exp.Copy();
				Summands.Insert(index, copy);
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
				copy.Summands.RemoveAt(index);
				copy.AddSummand(childCopy, index);
			}
			return copy;
		}

		public override Expression RemoveChild(Expression child)
		{
			//Sum copy = Copy() as Sum;
			Sum copy = Expand() as Sum; // Causes Problems
			if(child is Sum && !Summands.Contains(child))
			{
				foreach(Expression subChild in (child as Sum).Summands)
				{
					copy.Summands.Remove(subChild);
				}
			}
			else
			{
				copy.Summands.Remove(child);
			}
			return copy;
		}

		public override Expression Multiply(Expression factor)
		{
			Sum copy = Copy() as Sum;
			//if(copy.Summands.All(s => !s.GetDenominators().Any(d => d.ContainsFactor(factor))))
			if(!copy.GetDenominators().Contains(factor))
			{
				//return base.Multiply(factor);
				return copy * factor;
			}
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
			List<Expression> denominators = GetDenominators();
			Sum numerator = new Sum();
			Expression denominator = new Product(denominators);
			for(int i = 0; i < Summands.Count; i++)
			{
				numerator.AddSummand(Summands[i].Multiply(denominator.Copy()));
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
		  //if(ContainsFraction()) return ToCommonDenominator().ToStandardForm();
			Sum copy = Copy() as Sum;
			do
			{
				if(copy.Summands.Count(s => s.IsConst()) > 1)
				{
					double constSummand = 0;
					copy.Summands.ForEach(s =>
					{
						if(s.IsConst())
						{
							constSummand += (s.ToStandardForm() as ConstExpression).Value;
						}
					});
					copy.Summands.RemoveAll(s => s.IsConst());
					copy.Summands.Add(new ConstExpression(constSummand));
				}

				for(int i = 0; i < copy.Summands.Count; i++)
				{
					copy.Summands[i] = copy.Summands[i].Unpack().ToStandardForm();
				}
				while(copy.Summands.Any(s => s is Sum))
				{
					int index = copy.Summands.FindIndex(s => s is Sum);
					Sum sum = copy.Summands[index] as Sum;
					copy.Summands.RemoveAt(index);
					copy = copy.Merge(sum) as Sum;
				}
				List<Product> lp = copy.Summands.Where(s => s is Product && !s.IsFinal()).Cast<Product>().ToList();
				for(int i = 0; i < lp.Count; i++)
				{ // Product contains Sums,Products&ValueExpressions
					Expression stExp = lp[i].ToStandardForm();
					if(stExp is Sum)
					{
						copy = copy.ReplaceChild(lp[i], stExp as Sum) as Sum;
					}
					else if(stExp is Product)
					{
						copy = copy.ReplaceChild(lp[i], (stExp as Product).ToSum()) as Sum;
					}
				}
			} while(!copy.Summands.All(e => (e is Product || e is ValueExpression || e is Division)));

			while(Summands.Any(s => s is Product && (s as Product).Factors.Any(f => !(f is S_Block) && f.DeepContains(e => e is S_Block))))
			{
				for(int i = 0; i<Summands.Count; i++)
				{
					if(Summands[i] is Product && (Summands[i] as Product).Factors.Any(f => !(f is S_Block) && f.DeepContains(e => e is S_Block)))
						Summands[i] = (Summands[i] as Product).ToSum().ToStandardForm();
				}
			}

			//if(result.Summands.Count < 2) return result.Unpack();
			int[] summandPowers = new int[copy.Summands.Count];
			for(int i = 0; i < copy.Summands.Count; i++)
			{
				if(copy.Summands[i] is Product)
				{
					if((copy.Summands[i] as Product).Factors[0] is ConstExpression && (copy.Summands[i] as Product).Factors.Count > 1)
					{
						if((copy.Summands[i] as Product).Factors[1] is S_Block)
						{
							summandPowers[i] = ((copy.Summands[i] as Product).Factors[1] as S_Block).Exponent;
						}
					}
					else if((copy.Summands[i] as Product).Factors[0] is S_Block)
					{
						summandPowers[i] = ((copy.Summands[i] as Product).Factors[0] as S_Block).Exponent;
					}
				}
				else if(copy.Summands[i] is S_Block)
				{
					summandPowers[i] = (copy.Summands[i] as S_Block).Exponent;
				}
				else
				{
					summandPowers[i] = 0;
				}
			}
			if(summandPowers.All(s => s == 0)) return copy.Unpack();
			Dictionary<int, List<Expression>> dictListSTerms = new Dictionary<int, List<Expression>>();
			int smallestPower = summandPowers.Min() - 1;
			for(int i = 0; i < summandPowers.Length; i++)
			{
				smallestPower = summandPowers.Min(power => power > smallestPower ? power : Int32.MaxValue);
				if(smallestPower == Int32.MaxValue) break;
				dictListSTerms.Add(smallestPower, new List<Expression>());
				for(int j = 0; j < summandPowers.Length; j++)
				{
					if(smallestPower == summandPowers[j])
					{
						dictListSTerms[smallestPower].Add(copy.Summands[j]);
					}
				}
			}
			if(dictListSTerms.Count == 1)
			{
				if(dictListSTerms.First().Value.Count == 1)
				{
					return dictListSTerms.First().Value[0].Unpack();
				}
				else
				{
					S_Block commonSBlock = new S_Block(summandPowers[0]);
					Sum commonTerms = new Sum();
					foreach(Expression exp in dictListSTerms.First().Value)
					{
						if(exp is Sum)
						{
							commonTerms.AddSummand((exp as Sum).FactorOut(commonSBlock));
						}
						else if(exp is Product)
						{
							commonTerms.AddSummand((exp as Product).FactorOut(commonSBlock));
						}
					}
					return new Product(commonSBlock, commonTerms.Unpack());
				}
			}
			switch(SumOrder)
			{
				default:
				case Sum_Order.S2_S:
					dictListSTerms = dictListSTerms.OrderByDescending(kvp => kvp.Key).ToDictionary(key => key.Key, val => val.Value);
					break;
				case Sum_Order.S_S2:
					dictListSTerms = dictListSTerms.OrderBy(kvp => kvp.Key).ToDictionary(key => key.Key, val => val.Value);
					break;
			}
			Sum res = new Sum();
			foreach(KeyValuePair<int, List<Expression>> kvp in dictListSTerms)
			{
				S_Block s_block = new S_Block(kvp.Key);
				Sum sum = new Sum();
				foreach(Expression expression in kvp.Value)
				{
					if(expression is Product)
					{
						sum.AddSummand((expression as Product).FactorOut(s_block));
					}
					else if(expression is S_Block)
					{
						if(expression.Equals(s_block))
							sum.AddSummand(1);
						else
							sum.AddSummand(new S_Block((expression as S_Block).Exponent - kvp.Key));
					}
					else if(kvp.Key == 0)
					{
						sum.AddSummand(expression);
					}
				}
				if(kvp.Key == 0)
				{
					res.AddSummand(sum.ToStandardForm());
				}
				else if(sum.IsConst())
				{
					ConstExpression constExp = sum.ToStandardForm() as ConstExpression;
					if(constExp.Is1())
						res.AddSummand(s_block);
					else
						res.AddSummand(new Product(sum.ToStandardForm(), s_block));
				}
				else
					res.AddSummand(new Product(s_block, sum.ToStandardForm()));
			}
			return res.Unpack();
		}

		protected internal bool Equal(Sum other)
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

		public override bool IsConst()
		{
			return Summands.All(s => s.IsConst());
		}

		public override bool AllValuesSet()
		{
			return Summands.All(s => s.AllValuesSet());
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			return Equals(other) || Summands.Contains(other);
		}

		public override bool Contains(Func<Expression, bool> predicate)
		{
			return Summands.Any(predicate);
		}

		public override bool DeepContains(Expression other)
		{
			foreach(Expression summand in Summands)
			{
				if(summand.DeepContains(other))
				{
					return true;
				}
			}
			return Equals(other);
		}

		public override bool DeepContains(Func<Expression, bool> predicate)
		{
			bool b = Summands.Any(s => s.DeepContains(predicate));
			if(b)
				return b;
			else
				return predicate(this);
		}

		public override bool ContainsFactor(Expression other)
		{
			if(Equals(other))
				return true;
			else
				return Summands.All(s => s.ContainsFactor(other));
		}

		public override List<Expression> GetCommonFactors(List<Expression> oldFactors = null) // Does not handle constants & division
		{
			if(Summands.Count == 0)
				return new List<Expression>();
			else if(Summands.Count == 1)
			{
				if(oldFactors?.Contains(Summands[0]) ?? true)
				{
					return new List<Expression> { Summands[0] };
				}
				else
				{
					return new List<Expression>();
				}
			}
			else
			{
				Sum copy = Copy() as Sum;
				List<Expression> tempFactors = new List<Expression>();
				List<Expression> Result = new List<Expression>();
				if(copy.Summands[0] is Product)
				{
					foreach(Expression exp in (copy.Summands[0] as Product).Factors)
					{
						if(exp is S_Block)
						{
							for(int i = 0; i < (exp as S_Block).Exponent; i++)
							{
								tempFactors.Add(Expression.S);
							}
						}
						else
						{
							tempFactors.Add(exp);
						}
					}
				}
				else if(copy.Summands[0] is S_Block)
				{
					for(int i = 0; i < (copy.Summands[0] as S_Block).Exponent; i++)
						tempFactors.Add(Expression.S);
				}
				else
				{
					tempFactors.Add(copy.Summands[0]);
				}
				for(int i = 0; i < tempFactors.Count; i++)
				{
					if(copy.FactorOut(tempFactors[i]) is Sum reduced)
					{
						copy = reduced;
						Result.Add(tempFactors[i]);
					}
				}

				if(Result.Count == 0)
				{
					return new List<Expression>();
				}
				if(oldFactors == null)
				{
					int s_count = 0;
					Result.ForEach(f =>
					{
						if(f is S_Block)
						{
							s_count += (f as S_Block).Exponent;
						}
					});
					if(s_count != 0)
					{
						Result.RemoveAll(f => f is S_Block);
						Result.Add(new S_Block(s_count));
					}
					return Result;
				}
				else
				{
					List<Expression> oldFactorsCopy = new List<Expression>(oldFactors.Where(f => !(f is S_Block)));
					foreach(S_Block S_Factor in oldFactors.Where(f => f is S_Block))
					{
						for(int i = 0; i<S_Factor.Exponent; i++)
						{
							oldFactorsCopy.Add(S);
						}
					}
					List<Expression> newResult = new List<Expression>();
					foreach(Expression exp in Result)
					{
						if(oldFactorsCopy.Count(f => f.Equals(exp)) == 0)
						{
							continue;
						}
						oldFactorsCopy.Remove(exp);
						newResult.Add(exp);
					}
					int s_count = 0;			// Cleanup
					newResult.ForEach(f =>
					{
						if(f is S_Block)
						{
							s_count += (f as S_Block).Exponent;
						}
					});
					if(s_count != 0)
					{
						newResult.RemoveAll(f => f is S_Block);
						newResult.Add(new S_Block(s_count));
					}
					return newResult;
				}
			}
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
					if(newDen is Product)
					{
						foreach(Expression factor in (newDen as Product).Factors)
						{
							if(!newResult.Any(e => e.DeepContains(factor)))
							{
								newResult.Add(factor);
							}
						}
					}
					else
					{
						if(!newResult.Contains(newDen))
						{
							newResult.Add(newDen);
						}
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
				return this;
			}
		}

		public override Expression FactorOut(Expression factor) // Doesn't handle fractions
		{
			if(factor.Equals(1))
				return this;
			else if(factor.Equals(this))
				return 1;
			else if(Summands.Any(s => !s.ContainsFactor(factor) && !s.Equals(factor)))
				return null;
			Sum result = Copy() as Sum;
			for(int i = 0; i < result.Summands.Count; i++)
			{
				result.Summands[i] = result.Summands[i].FactorOut(factor);
			}
			return result;
		}
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

		public override Impedance EvaluateImpedance(double frequency)
		{
			Impedance temp = new Impedance(1);
			foreach(Expression factor in Factors)
			{
				temp = temp * factor.EvaluateImpedance(frequency);
			}
			return temp;
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
					if(!result.Equals("") && !result.EndsWith(")")) result += "*";
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

		public override string EvaluateLaTeX()
		{
			string result = "";
			if(Factors.All(f => f.Equals(1))) return "1";
			foreach(Expression exp in Factors)
			{
				if(exp is Sum)
				{
					string expEvaluate = exp.EvaluateLaTeX();
					if(expEvaluate.Equals("1"))
					{
						continue;
					}
					if(expEvaluate.Equals("0")) return "0";
					if(!result.Equals("") && !result.EndsWith(")")) result += "*";
					result += "(" + expEvaluate + ")";
				}
				else
				{
					string expEvaluate = exp.EvaluateLaTeX();
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

		public override Expression EvaluateToConst()
		{
			if(!AllValuesSet())
				return null;
			Product result = new Product();
			double value = 1;
			foreach(Expression exp in Factors)
			{
				Expression factor = exp.EvaluateToConst();
				if(factor is ConstExpression)	// factor.IsConst()
					value *= (factor as ConstExpression).Value;
				else
					result.Factors.Add(exp.EvaluateToConst());
			}
			if(result.Factors.Count == 0)
				return new ConstExpression(value);
			result.Factors.Add(new ConstExpression(value));
			return result;
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
			if(factor is Product)
			{
				foreach(Expression exp in (factor as Product).Factors)
				{
					int index = copy.Factors.FindIndex(f => f is Division && (f as Division).Denominator.Contains(exp));
					if(index == -1)
					{
						index = copy.Factors.FindIndex(f => f is Sum && f.GetDenominators().Contains(exp));
					}
					if(index != -1)
					{
						copy.Factors[index] = copy.Factors[index].Multiply(exp);
					}
					else
					{
						copy.AddFactor(exp);
					}
				}
			}
			else
			{
				int index = copy.Factors.FindIndex(f => f is Division && (f as Division).Denominator.Contains(factor));
				if(index == -1)
				{
					index = copy.Factors.FindIndex(f => f is Sum && f.GetDenominators().Contains(factor));
				}
				if(index != -1)
				{
					copy.Factors[index] = copy.Factors[index].Multiply(factor);
				}
				else
				{
					if(factor is S_Block)
					{
						index = copy.Factors.FindIndex(f => f is S_Block);
						if(index == -1)
							copy.AddFactor(factor);
						else
							(copy.Factors[index] as S_Block).Exponent += (factor as S_Block).Exponent;
					}
					else
						copy.AddFactor(factor);
				}
			}
			return copy;
		}

		public override Expression ToCommonDenominator()
		{
			if(Factors.Count == 1 || Factors.Count(f => f is Division) == 0) return this;
			List<Expression> nums = new List<Expression>();
			List<Expression> dens = new List<Expression>();
			foreach(Expression exp in Factors)
			{
				if(exp is Division)
				{
					nums.Add((exp as Division).Numerator.Copy());
					dens.Add((exp as Division).Denominator.Copy());
				}
				else
				{
					nums.Add(exp.Copy());
				}
			}
			return new Division
			{
				Numerator = nums.Count == 1 ? nums[0] : new Product(nums),
				Denominator = dens.Count == 1 ? dens[0] : new Product(dens)
			};
		}

		public override Expression ToStandardForm()
		{ // Standardform is a product of sums and reduced ValueExpressions
			Product result = Copy() as Product;
			do
			{
				for(int i = 0; i < result.Factors.Count; i++)
				{
					result.Factors[i] = result.Factors[i].Unpack().ToStandardForm(); //Debug test1 slow here
				}
				while(result.Factors.Any(s => s is Product))
				{
					Product product = result.Factors.First(s => s is Product) as Product;
					result = result.RemoveChild(product) as Product;
					result = result.Merge(product) as Product;
				}

				if(result.Factors.Any(f => f is Division))
				{
					Product num = new Product();
					Product den = new Product();
					foreach(Expression factor in result.Factors)
					{
						if(factor is Division)
						{
							num.AddFactor((factor as Division).Numerator);
							den.AddFactor((factor as Division).Denominator);
						}
						else
						{
							num.AddFactor(factor);
						}
					}
					return new Division(num, den).ToStandardForm();
				}

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

				newFactors.Sort(Compare);

				switch(ProductOrder)
				{
					default:
					case Product_Order._2SC:
						if(s_count != 0)
						{
							newFactors.Insert(0, new S_Block(s_count));
						}
						if(const_count != 1)
						{
							newFactors.Insert(0, new ConstExpression(const_count));
						}
						break;
					case Product_Order._S2C:
						if(const_count != 1)
						{
							newFactors.Insert(0, new ConstExpression(const_count));
						}
						if(s_count != 0)
						{
							newFactors.Insert(0, new S_Block(s_count));
						}
						break;
					case Product_Order._2CS:
						if(s_count != 0)
						{
							newFactors.Add(new S_Block(s_count));
						}
						if(const_count != 1)
						{
							newFactors.Insert(0, new ConstExpression(const_count));
						}
						break;
				}
				result.Factors = newFactors;
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

		protected internal bool Equal(Product other)
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

		protected internal bool Equal(ValueExpression other)
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

		public override bool IsConst()
		{
			return Factors.All(f => f.IsConst());
		}

		public override bool AllValuesSet()
		{
			return Factors.All(f => f.AllValuesSet());
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Contains(Expression other)
		{
			if(other is S_Block)
			{
				int s_count = 0;
				Factors.ForEach(e => { if(e is S_Block) s_count += (e as S_Block).Exponent; });
				return s_count >= (other as S_Block).Exponent;
			}
			else if(other is Product)
			{
				foreach(Expression exp in (other as Product).Factors)
				{
					int count = (other as Product).Factors.Count(f => f.ContainsFactor(exp));
					if(Factors.Count(f => f.ContainsFactor(exp)) < count) return false;
				}
				return true;
			}
			return Factors.Contains(other);
		}

		public override bool Contains(Func<Expression, bool> predicate)
		{
			return Factors.Any(predicate);
		}

		public override bool DeepContains(Expression other)
		{
			foreach(Expression factor in Factors)
			{
				if(factor.DeepContains(other))
				{
					return true;
				}
			}
			return Equals(other);
		}

		public override bool DeepContains(Func<Expression, bool> predicate)
		{
			bool b = Factors.Any(s => s.DeepContains(predicate));
			if(b)
				return b;
			else
				return predicate(this);
		}

		public override bool ContainsFactor(Expression other)
		{
			if(Equals(other) || Contains(other))
				return true;
			else if(other is Product)
				return (other as Product).Factors.All(f => (other as Product).Factors.Count(e => e.ContainsFactor(f)) == Factors.Count(e => e.ContainsFactor(f)));
			else
				return Factors.Any(f => f.ContainsFactor(other));
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
					if(newDen is Product)
					{
						newResult.AddRange((newDen as Product).Factors);
					}
					else
					{
						newResult.Add(newDen);      // Since this is a product, the denominators can be added multiple times
					}
				}
				else if(exp is Sum)
				{
					newResult = exp.GetDenominators(newResult);
				}
			}
			return newResult;
		}

		public override List<Expression> GetCommonFactors(List<Expression> oldFactors = null) // Does not handle constants & division
		{
			if(oldFactors == null)
			{
				return Factors;
			}
			else
			{
				List<Expression> result = new List<Expression>();
				foreach(Expression exp in oldFactors)
				{
					if(Factors.Contains(exp))
						result.Add(exp);
				}
				return result;
			}
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
				return this;
			}
		}

		public override Expression FactorOut(Expression factor) // if factor is product this will fail
		{														// Also, it doesnt handle split S_Blocks
			if(factor.Equals(1))
				return this;
			else if(factor.Equals(this))
				return 1;
			else if(!(factor is S_Block) && !(factor is Product && (factor as Product).Factors.All(f => Factors.Any(f2 => f2.ContainsFactor(f)))) && !Factors.Any(f => f.ContainsFactor(factor)))
				return null;
			Product result = Copy() as Product;
			if(factor is S_Block)
			{
				int s_count = 0;
				result.Factors.ForEach(e =>
				{
					if(e is S_Block)
					{
						s_count += (e as S_Block).Exponent;
					}

				});
				if((factor as S_Block).Exponent > s_count) return null;
				s_count = (factor as S_Block).Exponent;
				while(s_count > 0)
				{
					int first_s_index = result.Factors.FindIndex(f => f is S_Block);
					if((result.Factors[first_s_index] as S_Block).Exponent > s_count)
					{
						(result.Factors[first_s_index] as S_Block).Exponent -= s_count;
						//result.Factors.RemoveAt(first_s_index);
						s_count = 0;
					}
					else
					{
						s_count -= (result.Factors[first_s_index] as S_Block).Exponent;
						result.Factors.RemoveAt(first_s_index);
					}
				}
			}
			else if(factor is Product)
			{
				foreach(Expression exp in (factor as Product).Factors)
				{
					Expression temp = result.FactorOut(exp);
					Debug.Assert(temp is Product);
					result = temp as Product;
				}
			}
			else
			{
				int index = result.Factors.FindIndex(f => f.ContainsFactor(factor));
				if(index == -1)
				{
					return null;
				}
				if(result.Factors[index].Equals(factor))
				{
					result.Factors.Remove(factor);
				}
				else
				{
					result.Factors[index] = result.Factors[index].FactorOut(factor);
				}
			}
			return result;
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
		
		public override Impedance EvaluateImpedance(double frequency)
		{
			return Numerator.EvaluateImpedance(frequency) / Denominator.EvaluateImpedance(frequency);
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

		public override string EvaluateLaTeX()
		{
			return $"\\frac{{{Numerator.EvaluateLaTeX()}}}{{{Denominator.Evaluate()}}}";
		}

		public override Expression EvaluateToConst()
		{
			if(!AllValuesSet())
				return null;
			Expression num = Numerator.EvaluateToConst();
			Expression den = Denominator.EvaluateToConst();
			if(den is ConstExpression && num is ConstExpression)
			{
				return new ConstExpression((num as ConstExpression).Value / (den as ConstExpression).Value);
			}
			//if(den is ConstExpression)
			//{
			//	return (new Product(num, new ConstExpression(1 / (den as ConstExpression).Value))).EvaluateToConst();
			//}
			//if(num is ConstExpression)
			//{
			//	return (new Product(num, new ConstExpression(1 / (den as ConstExpression).Value))).EvaluateToConst();
			//}
			Division result = new Division
			{
				Numerator = num,
				Denominator = den
			};
			return result;
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
				copy.Denominator = (copy.Denominator as Product).RemoveChild(factor);
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

		public override bool DeepContains(Expression other)
		{
			bool b = Numerator.DeepContains(other) || Denominator.DeepContains(other);
			if(b)
				return b;
			else
				return Numerator.Equals(other) || Denominator.Equals(other);
		}

		public override bool DeepContains(Func<Expression, bool> predicate)
		{
			bool b = Numerator.DeepContains(predicate) || Denominator.DeepContains(predicate);
			if(b)
				return b;
			else
				return predicate(Numerator) || predicate(Denominator);
		}

		public override bool ContainsFactor(Expression other)
		{
			if(Equals(other))
				return true;
			else
				return Numerator.ContainsFactor(other);
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
		{
			if(Numerator.IsConst() && Denominator.IsConst())
			{
				ConstExpression num = Numerator.ToStandardForm() as ConstExpression;
				ConstExpression den = Denominator.ToStandardForm() as ConstExpression;
				return new ConstExpression(num.Value / den.Value);
			}
			Division copy = Copy() as Division;
			do
			{
				copy.Numerator = copy.Numerator.Unpack().ToStandardForm();
				copy.Denominator = copy.Denominator.Unpack().ToStandardForm();

				if(copy.Numerator is Division || copy.Denominator is Division)
				{
					if(copy.Numerator is Division && copy.Denominator is Division)
					{
						Division oldNum = copy.Numerator as Division;
						copy.Numerator = oldNum.Numerator.Multiply((copy.Denominator as Division).Denominator);
						copy.Denominator = (copy.Denominator as Division).Numerator.Multiply(oldNum.Denominator);
					}
					else if(copy.Numerator is Division)
					{
						copy.Denominator = copy.Denominator.Multiply((copy.Numerator as Division).Denominator);
						copy.Numerator = (copy.Numerator as Division).Numerator;
					}
					else
					{
						copy.Numerator = copy.Numerator.Multiply((copy.Denominator as Division).Denominator);
						copy.Denominator = (copy.Denominator as Division).Numerator;
					}
					copy.Numerator = copy.Numerator.Unpack().ToStandardForm();
					copy.Denominator = copy.Denominator.Unpack().ToStandardForm();
				}

				List<Expression> numFactors;
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
				if(copy.Denominator.Equals(1)) return copy.Numerator.ToStandardForm();

				
				List<Expression> dens = new List<Expression>();
				
				dens = copy.Numerator.GetDenominators(dens);
				dens = copy.Denominator.GetDenominators(dens);
				
				foreach(Expression factor in dens)
				{
					copy.Numerator = copy.Numerator.Multiply(factor);
					copy.Denominator = copy.Denominator.Multiply(factor);
					//copy.Numerator = copy.Numerator * factor;
					//copy.Denominator = copy.Denominator * factor;
				}
				if(dens.Count > 0)
				{
					copy.Numerator = copy.Numerator.ToStandardForm();
					copy.Denominator = copy.Denominator.ToStandardForm();
				}
			} while(copy.Numerator.ContainsFraction() || copy.Denominator.ContainsFraction());
			if(copy.Denominator.Equals(1))
			{
				return copy.Numerator.ToStandardForm();
			}
			if(copy.Numerator is Product && baseExpression)
			{
				copy.Numerator = (copy.Numerator as Product).ToSum().ToStandardForm();
			}
			if(copy.Denominator is Product)
			{
				copy.Denominator = (copy.Denominator as Product).ToSum().ToStandardForm();
			}
			if(copy.Denominator.Equals(copy.Numerator))
			{
				return 1;
			}
			List<Expression> commonFactors = copy.Numerator.GetCommonFactors();
			commonFactors = copy.Denominator.GetCommonFactors(commonFactors);
			if((commonFactors?.Count ?? 0) > 0)
			{
				foreach(Expression factor in commonFactors)
				{
					copy.Numerator = copy.Numerator.FactorOut(factor);
					copy.Denominator = copy.Denominator.FactorOut(factor);
				}
			}
			return copy.Unpack();
		}

		public override bool IsConst()
		{
			return Numerator.IsConst() && Denominator.IsConst();
		}

		public override bool AllValuesSet()
		{
			return Numerator.AllValuesSet() && Denominator.AllValuesSet();
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

		public override List<Expression> GetCommonFactors(List<Expression> oldFactors = null)
		{
			return Numerator.GetCommonFactors(oldFactors);
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

		public override Expression FactorOut(Expression factor)
		{
			Debug.Assert(false);
			throw new InvalidOperationException();
		}
	}

	public abstract class ValueExpression : Expression
	{
		public override Expression EvaluateToConst()
		{
			return Copy();
		}

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

		public override bool Contains(Expression other)
		{
			if(other is ValueExpression)
				return Equals(other);
			else
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

		public override bool DeepContains(Expression other)
		{
			return Contains(other);
		}

		public override bool DeepContains(Func<Expression, bool> predicate)
		{
			return predicate(this);
		}

		public override bool ContainsFactor(Expression other)
		{
			return Contains(other);
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

		protected internal bool Equal(ValueExpression other)
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

		public override List<Expression> GetCommonFactors(List<Expression> oldFactors = null)
		{
			if(oldFactors == null || oldFactors.Contains(this))
				return new List<Expression>() { this };
			else
				return new List<Expression>();
		}

		public abstract bool Is1();
		
		public override Expression FactorOut(Expression factor)
		{
			if(factor.Equals(1))
				return this;
			if(factor.Equals(this))
				return 1;
			else
			{
				Debug.Assert(false);
				return null;
			}
		}
	}

	public class ComponentExpression : ValueExpression
	{
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
			Debug.Assert(false);
			return null;
		}

		public override string Evaluate()
		{
			if(Value != null)
				return Value;
			else if(component?.Name != null)
				return component.Name;
			else return "";
		}

		public override string EvaluateLaTeX()
		{
			return Evaluate();
		}

		public override Expression EvaluateToConst()
		{
			if(!AllValuesSet())
				return null;
			else
				return new ConstExpression(component.GetValue());
		}

		public override Expression Copy()
		{
			ComponentExpression copy = new ComponentExpression(component)
			{
				Value = this.Value
			};
			return copy;
		}

		public override bool Is1()
		{
			return false;
		}

		public override bool IsConst()
		{
			return false;
		}

		public override bool AllValuesSet()
		{
			return component != null;
		}
	}

	public class ConstExpression : ValueExpression
	{
		public double Value;

		public ConstExpression(double val)
		{
			Value = val;
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			return new Impedance(Value);
		}

		public override string Evaluate()
		{
			return Value.ToString();
		}

		public override string EvaluateLaTeX()
		{
			return Value.ToString();
		}
		
		public override Expression Copy()
		{
			return new ConstExpression(Value);
		}

		public override bool Is1()
		{
			return Value == 1;
		}

		public override bool IsConst()
		{
			return true;
		}

		public override bool AllValuesSet()
		{
			return true;
		}
	}

	public class S_Block : ValueExpression
	{
		public int Exponent { get; set; }

		public S_Block(int exponent = 1)
		{
			Exponent = exponent;
		}

		public override Impedance EvaluateImpedance(double frequency)
		{
			switch(Exponent % 4)
			{
				default:
				case 0:
					return new Impedance(Math.Pow(2 * Math.PI * frequency, Exponent), 0);
				case 1:
					return new Impedance(0, Math.Pow(2 * Math.PI * frequency, Exponent));
				case 2:
					return new Impedance(-Math.Pow(2 * Math.PI * frequency, Exponent), 0);
				case 3:
					return new Impedance(0, -Math.Pow(2 * Math.PI * frequency, Exponent));
			}
		}

		public override string Evaluate()
		{
			if(Exponent < 0)
				return $"s^{Exponent}";
			if(Exponent == 0)
				return "1";
			else if(Exponent == 1)
				return "s";
			else
				return $"s^{Exponent}";
		}

		public override string EvaluateLaTeX()
		{
			if(Exponent < 0)
				return $"s^{{{Exponent}}}";
			if(Exponent == 0)
				return "1";
			else if(Exponent == 1)
				return "s";
			else
				return $"s^{{{Exponent}}}";
		}
		
		public override Expression ToStandardForm()
		{
			if(Exponent == 0)
				return new ConstExpression(1);
			else
				return this;
		}

		public override bool Contains(Expression other)
		{
			if(other is S_Block)
				return (other as S_Block).Exponent <= Exponent;
			else
				return false;
		}

		public override List<Expression> GetCommonFactors(List<Expression> oldFactors = null)
		{
			if(oldFactors == null)
				return new List<Expression>() { this };
			int s_count = 0;
			oldFactors.ForEach(f =>
			{
				if(f is S_Block)
				{
					s_count += (f as S_Block).Exponent;
				}
			});
			if(Exponent == 0)
				return oldFactors;
			else if(s_count == 0)
				return new List<Expression>();
			if(s_count > Exponent)
				return new List<Expression>() { this };
			else
				return new List<Expression>() { new S_Block(s_count) };
			
		}

		public override Expression Copy()
		{
			return new S_Block(Exponent);
		}

		public override bool Is1()
		{
			return Exponent == 0;
		}

		public override bool IsConst()
		{
			return false;
		}

		public override bool AllValuesSet()
		{
			return true;
		}

		public override Expression FactorOut(Expression factor)
		{
			if(factor is S_Block)
			{
				return new S_Block(Exponent - (factor as S_Block).Exponent);
			}
			else
			{
				Debug.Assert(false);
				return null;
			}
		}

		public static S_Block operator ^(S_Block s1, int exp)
		{
			return new S_Block(s1.Exponent * exp);
		}
	}
}