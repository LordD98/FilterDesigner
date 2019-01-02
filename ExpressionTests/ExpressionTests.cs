using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FilterDesigner.UnitTests
{
	[TestClass]
	public class ExpressionTests
	{
		[TestMethod]
		public void Equals_ToSum_EvaluateEqual()
		{
			Expression s = new ValueExpression("s");
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
			Expression s = new ValueExpression("s");

			Assert.IsTrue((s * s).Equals(s * s));
			Assert.IsTrue((s).Equals(s));
			Assert.IsTrue(new ValueExpression(1).Equals(1));
			Assert.IsTrue(new ValueExpression(1).Equals(new ValueExpression(1)));
		}

		[TestMethod]
		public void Equals_BasicEqual_ReturnsFalse()
		{
			Expression s = new ValueExpression("s");

			Assert.IsFalse((s * s).Equals(s));
			Assert.IsFalse(new ValueExpression(1).Equals(s));
			Assert.IsFalse(s.Equals(new ValueExpression(1)));
			Assert.IsFalse(s.Equals(1));
		}

		[TestMethod]
		public void Equals_DoubleFraction_ReturnsTrue()
		{
			Expression s = new ValueExpression("s");
			Expression exp1 = 1 / (1/s);

			bool result = exp1.Equals(s);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Equals_UnreducedFraction_ReturnsTrue()
		{
			Expression s = new ValueExpression("s");
			Expression exp1 = (s * s + s + s + 1) / (s + 1);
			Expression exp2 = s + 1;

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Equals_ProductEqual_ReturnsTrue()
		{
			Expression s = new ValueExpression("s");
			Expression exp1 = (s * s + s + s + 1);
			Expression exp2 = (s + 1)*(s + 1);

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Equals_CommutedProductEqual_ReturnsTrue()
		{
			Expression s = new ValueExpression("s");
			Expression exp1 = 2 * s * 4 * s;
			Expression exp2 = s * 2 * 4 * s;

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void Equals_CommutedSumEqual_ReturnsTrue()
		{
			Expression s = new ValueExpression("s");
			Expression exp1 = s * s + s + s + 1;
			Expression exp2 = s + s * s + 1 + s;

			bool result = exp1.Equals(exp2);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void ToSum_EvaluateEqual()
		{
			Expression s = new ValueExpression("s");
			Expression exp1 = s * s + s + s + 1;
			Product exp2 = (s + 1) * (s + 1) as Product;
			Expression exp3 = exp2.ToSum(); 

			string result1 = exp1.Evaluate();
			string result2 = exp3.Evaluate();

			Assert.AreEqual(result1, "s*s+s+s+1");
			Assert.AreEqual(result2, "s*s+s+s+1");
		}

		[TestMethod]
		public void _()
		{

		}
	}
}
