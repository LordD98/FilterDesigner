using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FilterDesigner.UnitTests
{
	[TestClass]
	public class EqualsTests    // Incomplete
	{
		[TestMethod]
		public void ToSum_EvaluateEqual()
		{
			Expression s = Expression.S;
			Product exp1 = s * s as Product;
			Expression exp2 = exp1.ToSum();

			string result1 = exp1.Evaluate();
			string result2 = exp2.Evaluate();
			Assert.AreEqual("s*s", result1);
			Assert.AreEqual("s*s", result2);
		}

		[TestMethod]
		public void BasicEqual_ReturnsTrue()
		{
			Expression s = Expression.S;

			Assert.IsTrue((s * s).Equals(s * s));
			Assert.IsTrue(s.Equals(s));
			Assert.IsTrue(new ConstExpression(1).Equals(1));
			Assert.IsTrue(new ConstExpression(1).Equals(new ConstExpression(1)));
			Assert.IsTrue(new ConstExpression(2).Equals(new ConstExpression(2)));
		}

		[TestMethod]
		public void BasicEqual_ReturnsFalse()
		{
			Expression s = Expression.S;

			Assert.IsFalse((s * s).Equals(s));
			Assert.IsFalse(new ConstExpression(1).Equals(s));
			Assert.IsFalse(s.Equals(new ConstExpression(1)));
			Assert.IsFalse(s.Equals(1));
			Assert.IsFalse(new ConstExpression(1).Equals(new ConstExpression(2)));
		}

		[TestMethod]
		public void NestedFraction_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = 1 / (1 / s);

			bool result = exp1.Equals(s);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void UnreducedFraction_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = (s * s + s + s + 1) / (s + 1);
			Expression exp2 = s + 1;

			bool result1 = exp1.Equals(exp2);
			bool result2 = exp2.Equals(exp1);

			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
		}

		[TestMethod]
		public void DoubleFraction_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = (s * s * s + s * s + s * 2 * s + 2 * s + 3 * s * s + 3 * s + s * 2 * 3 + 2 * 3) / (s + 2);
			Expression exp2 = (s * s * s + s * s + 3 * s * s + s * 3 + s * 3 * s + 3 * s + 3 * s * 3 + 3 * 3) / (s + 3);

			bool result1 = exp1.Equals(exp2);
			bool result2 = exp2.Equals(exp1);

			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
		}

		[TestMethod]
		public void ProductEqual_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = (s * s + s + s + 1);
			Expression exp2 = (s + 1) * (s + 1);

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void ValueExpressionProduct_ReturnsTrue()
		{
			ValueExpression s = Expression.S;
			Product p1 = 3 * s * 2 as Product;
			Product p2 = s * 6 as Product;

			bool result1 = p1.Equals(p2);
			bool result2 = p2.Equals(p1);

			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
		}

		[TestMethod]
		public void CommutedProductEqual_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = 2 * s * 4 * s;
			Expression exp2 = s * 2 * 4 * s;

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void CommutedSumEqual_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = s * s + s + s + 1;
			Expression exp2 = s + s * s + 1 + s;

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void ProductsEqual_ReturnsTrue()
		{
			Expression s = Expression.S;
			Product exp1 = 3 * (s + 1) * 2 as Product;
			Product exp2 = (s + 1) * 6 as Product;

			bool result1 = exp1.Equals(exp2);
			bool result2 = exp2.Equals(exp1);

			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
		}

		[TestMethod]
		public void ProductsEqual_ReturnsFalse()
		{
			Expression s = Expression.S;
			Product exp1 = s * s * 1 as Product;
			Product exp2 = (s + 1) * 3 as Product;

			bool result1 = exp1.Equals(exp2);
			bool result2 = exp2.Equals(exp1);

			Assert.IsFalse(result1);
			Assert.IsFalse(result2);
		}

	}

	[TestClass]
	public class ToStandardFormTests
	{
		[TestMethod]
		public void ProductsEqual_ReturnsTrue()
		{
			Expression s = Expression.S;
			Product exp1 = s * s * s as Product;
			S_Block s3 = new S_Block(3);

			bool result1 = exp1.Equals(s3);
			bool result2 = s3.Equals(exp1);

			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
		}

		[TestMethod]
		public void DivisionProductSum_EvaluateEqual()
		{
			Expression s = Expression.S;
			Expression exp = 1 / (2 * (1 + 1 / s));
			Expression expected = s / (2 * s + 2);
			
			exp = exp.ToStandardForm();

			Assert.AreEqual(exp.Evaluate(), expected.Evaluate());
		}

		[TestMethod]
		public void SumSortSummands_EvaluateEqual()
		{
			Component C2 = new Capacitor("C2");
			Expression s = Expression.S;
			Expression exp = 1 + s + s*4*s*s + 3*s + s*1/C2.GetExpression()*s;
			string expected = "s^3*(4+C2)+s*4+1";

			exp = exp.ToStandardForm();

			Assert.AreEqual(exp.Evaluate(), expected);
		}
	}

	[TestClass]
	public class OtherTests
	{
		[TestMethod]
		public void ToSum_EvaluateEqual()
		{
			Expression s = Expression.S;
			Expression exp1 = s * s + s + s + 1;
			Product exp2 = (s + 1) * (s + 1) as Product;
			Expression exp3 = exp2.ToSum(); 

			string result1 = exp1.ToStandardForm().Evaluate();
			string result2 = exp3.Evaluate();

			Assert.AreEqual(result1, "s^2+s*2+1");
			Assert.AreEqual(result2, "s^2+s+s+1");
		}

		[TestMethod]
		public void Contains_ReturnsTrue()
		{
			Expression s = Expression.S;
			Product p1 = s * s * 4 * s * s;
			Product p2 = (s + 1) * (s + s) * 3 * s;

			bool result1 = p1.Contains(s * s);
			bool result2 = p1.Contains(s * s * s * s);
			bool result3 = p1.Contains(new S_Block(2));
			bool result4 = p1.Contains(new S_Block(3));
			bool result5 = p1.Contains(new S_Block(4));

			bool result6 = p2.Contains(3 * s * (s + 1));

			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
			Assert.IsTrue(result3);
			Assert.IsTrue(result4);
			Assert.IsTrue(result5);
			Assert.IsTrue(result6);
		}

		[TestMethod]
		public void IsConst_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = (1 + 1 / new ConstExpression(3)) / (1 * (3 * (4 * new ConstExpression(7))));
			Expression exp2 = new ConstExpression(3) * (1 * (3 * (4 * new ConstExpression(7))));
			Expression exp3 = 1 + 1 / new ConstExpression(3) + 1 + 6 * (3 * (4 * new ConstExpression(7)));
			
			bool result1 = exp1.IsConst();
			bool result2 = exp2.IsConst();
			bool result3 = exp3.IsConst();
			bool result4 = exp1.ToStandardForm() is ConstExpression;
			bool result5 = exp2.ToStandardForm() is ConstExpression;
			bool result6 = exp3.ToStandardForm() is ConstExpression;
			double result7 = (exp1.ToStandardForm() as ConstExpression).Value;
			double result8 = (exp2.ToStandardForm() as ConstExpression).Value;
			double result9 = (exp3.ToStandardForm() as ConstExpression).Value;


			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
			Assert.IsTrue(result3);
			Assert.IsTrue(result4);
			Assert.IsTrue(result5);
			Assert.IsTrue(result6);
		}

		[TestMethod]
		public void ToStandardForm_Const_AreEqual()
		{
			Expression exp1 = (1 + 1 / new ConstExpression(3)) / (1 * (3 * (4 * new ConstExpression(7))));
			Expression exp2 = new ConstExpression(3) * (1 * (3 * (4 * new ConstExpression(7))));
			Expression exp3 = 1 + 1 / new ConstExpression(3) + 1 + 6 * (3 * (4 * new ConstExpression(7)));

			double result1 = (exp1.ToStandardForm() as ConstExpression).Value;
			double result2 = (exp2.ToStandardForm() as ConstExpression).Value;
			double result3 = (exp3.ToStandardForm() as ConstExpression).Value;

			Assert.AreEqual(result1, 1.0 / 63.0);
			Assert.AreEqual(result2, 252);
			Assert.AreEqual(result3, 7.0/3.0 + 504);
		}

		[TestMethod]
		public void FactorOut_Product_AreEqual()
		{
			Expression s = Expression.S;
			Product p1 = s * s * 4 * s * s;
			Product p2 = 2 * s * 5 * s * s;
			Expression expected1 = 4 * s;
			Expression expected2 = 10;

			Expression result1 = p1.FactorOut(new S_Block(3));
			Expression result2 = p2.FactorOut(new S_Block(3));

			Assert.IsTrue(expected1.Equals(result1));
			Assert.IsTrue(expected2.Equals(result2));
		}

		[TestMethod]
		public void FactorOut_Sum_AreEqual()
		{
			Expression s = Expression.S;
			Sum s1 = s * s + s * 4 * s * s;
			Sum s2 = 2 * s * s + 5 * s * s;
			Expression expected1 = 1 + 4 * s;
			Expression expected2 = 7;

			Expression result1 = s1.FactorOut(new S_Block(2));
			Expression result2 = s2.FactorOut(new S_Block(2));

			Assert.AreEqual(expected1, result1);
			Assert.AreEqual(expected2, result2);
		}
	}
}
