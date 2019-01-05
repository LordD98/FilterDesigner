using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FilterDesigner.UnitTests
{
	[TestClass]
	public class ExpressionTests	// Incomplete
	{
		[TestMethod]
		public void Equals_ToSum_EvaluateEqual()
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
		public void Equals_BasicEqual_ReturnsTrue()
		{
			Expression s = Expression.S;

			Assert.IsTrue((s * s).Equals(s * s));
			Assert.IsTrue(s.Equals(s));
			Assert.IsTrue(new ConstExpression(1).Equals(1));
			Assert.IsTrue(new ConstExpression(1).Equals(new ConstExpression(1)));
			Assert.IsTrue(new ConstExpression(2).Equals(new ConstExpression(2)));
		}

		[TestMethod]
		public void Equals_BasicEqual_ReturnsFalse()
		{
			Expression s = Expression.S;

			Assert.IsFalse((s * s).Equals(s));
			Assert.IsFalse(new ConstExpression(1).Equals(s));
			Assert.IsFalse(s.Equals(new ConstExpression(1)));
			Assert.IsFalse(s.Equals(1));
			Assert.IsFalse(new ConstExpression(1).Equals(new ConstExpression(2)));
		}

		[TestMethod]
		public void Equals_NestedFraction_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = 1 / (1/s);

			bool result = exp1.Equals(s);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Equals_UnreducedFraction_ReturnsTrue()
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
		public void Equals_DoubleFraction_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = (s*s*s + s*s + s*2*s + 2*s + 3*s*s + 3*s + s*2*3 + 2*3) / (s + 2);
			Expression exp2 = (s*s*s + s*s + 3*s*s + s*3 + s*3*s + 3*s + 3*s*3 + 3*3) / (s + 3);

			bool result1 = exp1.Equals(exp2);
			bool result2 = exp2.Equals(exp1);

			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
		}

		[TestMethod]
		public void Equals_ProductEqual_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = (s * s + s + s + 1);
			Expression exp2 = (s + 1)*(s + 1);

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Equals_ValueExpressionProduct_ReturnsTrue()
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
		public void Equals_CommutedProductEqual_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = 2 * s * 4 * s;
			Expression exp2 = s * 2 * 4 * s;

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Equals_CommutedSumEqual_ReturnsTrue()
		{
			Expression s = Expression.S;
			Expression exp1 = s * s + s + s + 1;
			Expression exp2 = s + s * s + 1 + s;

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Equals_ProductsEqual_ReturnsTrue()
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
		public void Equals_ProductsEqual_ReturnsFalse()
		{
			Expression s = Expression.S;
			Product exp1 = s * s * 1 as Product;
			Product exp2 = (s + 1) * 3 as Product;

			bool result1 = exp1.Equals(exp2);
			bool result2 = exp2.Equals(exp1);

			Assert.IsFalse(result1);
			Assert.IsFalse(result2);
		}

		[TestMethod]
		public void ToStandardForm_ProductsEqual_ReturnsTrue()
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
		public void ToStandardForm_DivisionProductSum_EvaluateEqual()
		{
			Expression s = Expression.S;
			Expression exp = 1 / (2 * (1 + 1 / s));
			Expression expected = s / (2 * s + 2);
			
			exp = exp.ToStandardForm();

			Assert.AreEqual(exp.Evaluate(), expected.Evaluate());
		}

		[TestMethod]
		public void ToStandardForm_SumSortSummands_EvaluateEqual()
		{
			Component C2 = new Capacitor("C2");
			Expression s = Expression.S;
			Expression exp = 1 + s + s*4*s*s + 3*s + s*C2.GetExpression()*s;
			string expected = "4*s^3+s^2*C2+(1+3)*s+1";

			exp = exp.ToStandardForm();

			Assert.AreEqual(exp.Evaluate(), expected);
		}

		[TestMethod]
		public void ToSum_EvaluateEqual()
		{
			Expression s = Expression.S;
			Expression exp1 = s * s + s + s + 1;
			Product exp2 = (s + 1) * (s + 1) as Product;
			Expression exp3 = exp2.ToSum(); 

			string result1 = exp1.ToStandardForm().Evaluate();
			string result2 = exp3.Evaluate();

			Assert.AreEqual(result1, "s^2+s+s+1");
			Assert.AreEqual(result2, "s^2+s+s+1");
		}
	}
}
