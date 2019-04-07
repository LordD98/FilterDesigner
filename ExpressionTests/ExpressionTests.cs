using System;
using System.Collections.Generic;
using System.Linq;
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
			
			exp = exp.ToStandardForm(true);

			Assert.AreEqual(exp.Evaluate(), expected.Evaluate());
		}

		[TestMethod]
		public void DivisionReduce_Evaluates1()
		{
			Component_Order oldCompOrder = Expression.ComponentOrder;
			Product_Order oldProdOrder = Expression.ProductOrder;
			Sum_Order oldSumOrder = Expression.SumOrder;
			Expression.ComponentOrder = Component_Order.RCL;
			Expression.ProductOrder = Product_Order._2SC;
			Expression.SumOrder = Sum_Order.S2_S;
			Expression C = new ComponentExpression("C");
			Expression s = Expression.S;
			Expression exp1 = s * C * (1 / (s * C));
			Expression exp2 = exp1.ToStandardForm(true);

			Assert.IsTrue(exp1.Equals(1) && exp2.Equals(1));
			Assert.AreEqual(exp2.Evaluate(), "1");

			Expression.ComponentOrder = oldCompOrder;
			Expression.ProductOrder = oldProdOrder;
			Expression.SumOrder = oldSumOrder;
		}

		[TestMethod]
		public void DivisionReduceCommonFactors()
		{
			Component_Order oldCompOrder = Expression.ComponentOrder;
			Product_Order oldProdOrder = Expression.ProductOrder;
			Sum_Order oldSumOrder = Expression.SumOrder;
			Expression.ComponentOrder = Component_Order.RCL;
			Expression.ProductOrder = Product_Order._2SC;
			Expression.SumOrder = Sum_Order.S2_S;
			Expression C = new ComponentExpression("C");
			Expression s = Expression.S;
			Expression s3 = new S_Block(3);
			Expression s6 = new S_Block(6);
			Expression exp1 = (s6 * C + s3 * C * C)/(s * s * C * C * C * s3 + s * s6 * C * C * C);
			Expression exp2 = exp1.ToStandardForm(true);
			
			Assert.AreEqual(exp2.Evaluate(), "(s^3+C)/(s^4*C*C+s^2*C*C)");

			Expression.ComponentOrder = oldCompOrder;
			Expression.ProductOrder = oldProdOrder;
			Expression.SumOrder = oldSumOrder;
		}

		[TestMethod]
		public void Division_EvaluateEqual()
		{
			Component_Order oldCompOrder = Expression.ComponentOrder;
			Product_Order oldProdOrder = Expression.ProductOrder;
			Sum_Order oldSumOrder = Expression.SumOrder;
			Expression.ComponentOrder = Component_Order.RCL;
			Expression.ProductOrder = Product_Order._2SC;
			Expression.SumOrder = Sum_Order.S2_S;
			Expression R = new ComponentExpression("R");
			Expression C = new ComponentExpression("C");
			Expression L = new ComponentExpression("L");
			Expression s = Expression.S;
			Division exp1 = (s * C * (1 / R + 1 / (s * L)) * 1 / (s * C) + s * C * (1 / R + 1 / (s * L)) * 1 / (1 / R + 1 / (s * L))) / (s * C * (1 / R + 1 / (s * L)));
			string expected = "(s^2*R*C*L+s*L+R)/(s^2*C*L+s*R*C)";
			Expression exp2 = exp1.ToStandardForm(true);

			Assert.AreEqual(exp1, exp2);
			Assert.AreEqual(exp2.Evaluate(), expected);

			Expression.ComponentOrder = oldCompOrder;
			Expression.ProductOrder = oldProdOrder;
			Expression.SumOrder = oldSumOrder;
		}

		[TestMethod]
		public void SumSortSummands_EvaluateEqual()
		{
			Expression s = Expression.S;
			Expression C2 = new ComponentExpression("C2");
			Expression exp = 1 + s + s*4*s*s + 3*s + s*(s*C2)*s;
			string expected = "s^3*(4+C2)+4*s+1";

			exp = exp.ToStandardForm(true);

			Assert.AreEqual(exp.Evaluate(), expected);
		}

		[TestMethod]
		public void ToCommodDen_EvaluateEqual()
		{
			Component_Order oldCompOrder = Expression.ComponentOrder;
			Product_Order oldProdOrder = Expression.ProductOrder;
			Sum_Order oldSumOrder = Expression.SumOrder;
			Expression.ComponentOrder = Component_Order.RCL;
			Expression.ProductOrder = Product_Order._2SC;
			Expression.SumOrder = Sum_Order.S2_S;
			Expression R = new ComponentExpression("R");
			Expression C = new ComponentExpression("C");
			Expression L = new ComponentExpression("L");
			Expression s = Expression.S;
			Expression exp1 = L + R / s + 1 / (s * C);
			Expression exp2 = (s * C * L + R * C + 1) / (s * C);

			Expression exp3 = exp1.ToCommonDenominator();
			exp3 = exp3.ToStandardForm(true);

			Assert.AreEqual(exp3.Evaluate(), exp2.Evaluate());

			Expression.ComponentOrder = oldCompOrder;
			Expression.ProductOrder = oldProdOrder;
			Expression.SumOrder = oldSumOrder;
		}

		[TestMethod]
		public void ToCommonDenDifferentDens_EvaluateEqual()
		{
			Component_Order oldCompOrder = Expression.ComponentOrder;
			Product_Order oldProdOrder = Expression.ProductOrder;
			Sum_Order oldSumOrder = Expression.SumOrder;
			Expression s = Expression.S;
			ComponentExpression R = new ComponentExpression("R");
			ComponentExpression L = new ComponentExpression("L");
			ComponentExpression C = new ComponentExpression("C");
			Expression exp1 = 1 / (s * C) + 1 / (1 / R + 1 / (s * L));

			Expression.SumOrder = Sum_Order.S2_S;
			Expression.ProductOrder = Product_Order._2SC;
			Expression.ComponentOrder = Component_Order.RCL;
			Expression exp2 = exp1.ToCommonDenominator().ToStandardForm(true);
			Expression.ComponentOrder = Component_Order.RLC;
			Expression exp3 = exp2.ToStandardForm(true);
			Expression.SumOrder = Sum_Order.S_S2;
			Expression.ComponentOrder = Component_Order.RCL;
			Expression exp4 = exp2.ToStandardForm(true);
			Expression.ComponentOrder = Component_Order.RLC;
			Expression exp5 = exp2.ToStandardForm(true);
			string expected1 = "(s^2*R*C*L+s*L+R)/(s^2*C*L+s*R*C)";
			string expected2 = "(s^2*R*L*C+s*L+R)/(s^2*L*C+s*R*C)";
			string expected3 = "(R+s*L+s^2*R*C*L)/(s*R*C+s^2*C*L)";
			string expected4 = "(R+s*L+s^2*R*L*C)/(s*R*C+s^2*L*C)";

			Assert.AreEqual(exp2.Evaluate(), expected1);
			Assert.AreEqual(exp3.Evaluate(), expected2);
			Assert.AreEqual(exp4.Evaluate(), expected3);
			Assert.AreEqual(exp5.Evaluate(), expected4);
			Expression.ComponentOrder = oldCompOrder;
			Expression.ProductOrder = oldProdOrder;
			Expression.SumOrder = oldSumOrder;
		}

		[TestMethod]
		public void Const_AreEqual()
		{
			Expression exp1 = (1 + 1 / new ConstExpression(3)) / (1 * (3 * (4 * new ConstExpression(7))));
			Expression exp2 = new ConstExpression(3) * (1 * (3 * (4 * new ConstExpression(7))));
			Expression exp3 = 1 + 1 / new ConstExpression(3) + 1 + 6 * (3 * (4 * new ConstExpression(7)));

			double result1 = (exp1.ToStandardForm(true) as ConstExpression).Value;
			double result2 = (exp2.ToStandardForm(true) as ConstExpression).Value;
			double result3 = (exp3.ToStandardForm(true) as ConstExpression).Value;

			Assert.AreEqual(result1, 1.0 / 63.0);
			Assert.AreEqual(result2, 252);
			Assert.AreEqual(result3, 7.0 / 3.0 + 504);
		}

		[TestMethod]
		public void ComplicatedBug_Test1()
		{
			Expression s = Expression.S;
			ComponentExpression C1 = new ComponentExpression("C1");
			ComponentExpression R1 = new ComponentExpression("R1");
			ComponentExpression R2 = new ComponentExpression("R2");
			ComponentExpression R3 = new ComponentExpression("R3");
			Expression exp1 = ((1 / (s * C1)) / (R2 + 1 / (s * C1))) * ((1 / (1 / (R2 + 1 / (s * C1)) + 1 / (R3))) / (R1 + 1 / (1 / (R2 + 1 / (s * C1)) + 1 / (R3))));
			Expression exp2 = R3 / (s * (R3 * R2 * C1 + R1 * R3 * C1 + R1 * R2 * C1) + R1 + R3);
			Expression exp3 = exp1.ToStandardForm(true);

			Assert.AreEqual(exp1, exp2);
			Assert.AreEqual(exp1, exp3);
			Assert.AreEqual(exp2, exp3);
			Assert.AreEqual(exp3.Evaluate(), exp2.Evaluate());
		}

		[TestMethod]
		public void ComplicatedBug_Test2()
		{
			Expression s = Expression.S;
			ComponentExpression C1 = new ComponentExpression("C1"); 
			ComponentExpression R1 = new ComponentExpression("R1"); 
			ComponentExpression R2 = new ComponentExpression("R2"); 
			ComponentExpression R3 = new ComponentExpression("R3");
			Expression exp1 = (1 / (s * C1)) / (R2 + 1 / (s * C1)) * (1 / (1 / (R2 + 1 / (s * C1)) + 1 / (R3))) / (R1 + 1 / (1 / (R2 + 1 / (s * C1)) + 1 / (R3)));
			Expression exp2 = R3 / (s * (R3 * R2 * C1 + R1 * R3 * C1 + R1 * R2 * C1) + R1 + R3);
			Expression exp3 = exp1.ToStandardForm(true);

			Assert.AreEqual(exp1, exp2);
			Assert.AreEqual(exp1, exp3);
			Assert.AreEqual(exp2, exp3);
			Assert.AreEqual(exp3.Evaluate(), exp2.Evaluate());
		}

		[TestMethod]
		public void ComplicatedBug_Test3()
		{
			Expression s = Expression.S;
			Expression s2 = new S_Block(2);
			Expression s3 = new S_Block(3);
			Expression s4 = new S_Block(4);
			Expression exp1 = (1 / s) / (1 + 1 / s) * (1 / (1 / (1 + 1 / s) + 1)) / (1 + 1 / (1 / (1 + 1 / s) + 1));
			Expression exp2 = 1 / (s * 1 * (1 * 1 + 1 * 1 + 1 * 1) + 1 + 1);
			Expression exp3 = exp1.ToStandardForm(true);
			Expression exp4 = (2 * s3 + 3 * s2 + s) / (6 * s4 + 13 * s3 + 9 * s2 + 2 * s);
			Assert.AreEqual(exp1, exp2);
			Assert.AreEqual(exp1, exp3);
			Assert.AreEqual(exp2, exp3);
			Assert.AreEqual(exp3, exp4);
		}

		[TestMethod]
		public void ComplicatedBug_Test4()
		{
			Expression s = Expression.S;
			Expression s2 = new S_Block(2);
			Expression s3 = new S_Block(3);
			Expression s4 = new S_Block(4);
			ComponentExpression C1 = new ComponentExpression("C1");
			ComponentExpression R1 = new ComponentExpression("R1");
			ComponentExpression R2 = new ComponentExpression("R2");
			ComponentExpression R3 = new ComponentExpression("R3");

			Expression exp1 = (1 / (s * C1)) / (R2 + 1 / (s * C1)) * 1 / (1 / (R2 + 1 / (s * C1)) + 1 / (R3));
			Expression exp2 = R3 / (s * (R3 * C1 + R2 * C1) + 1);
			Expression exp3 = (s * C1 * R2 * R3 + R3) / (s2 * (C1 * C1 * R2 * R3 + C1 * C1 * R2 * R2) + s * (C1 * R2 + C1 * R2 + C1 * R3) + 1);

			Expression res1 = exp1.ToStandardForm(true);

			Assert.AreEqual(exp1, exp2);
			Assert.AreEqual(res1, exp2);
			Assert.AreNotEqual(res1.Evaluate(), exp3.Evaluate());
			Assert.AreEqual(res1.Evaluate(), exp2.Evaluate());
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

			string result1 = exp1.ToStandardForm(true).Evaluate();
			string result2 = exp3.Evaluate();

			Assert.AreEqual(result1, "s^2+2*s+1");
			Assert.AreEqual(result2, "s^2+s+s+1");
		}

		[TestMethod]
		public void Contains_ReturnsTrue()
		{
			Expression s = Expression.S;
			Product p1 = s * s * 4 * s * s as Product;
			Product p2 = (s + 1) * (s + s) * 3 * s as Product;

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
		public void Contains_S_ReturnsTrue()
		{
			S_Block s = Expression.S;
			ComponentExpression R = new ComponentExpression("R");
			Expression exp1 = (s ^ 3) * (3*(s ^ 3) + 2*s) + s * (5*(s^3) + 7*s);
			Expression exp2 = 14 + R * 3 + 3*(2+R*(1+(s^2)));
			
			bool result1 = exp1.Contains(s);
			bool result2 = exp1.Contains(p => p is S_Block);
			bool result3 = exp1.DeepContains(s);
			bool result4 = exp1.DeepContains(p => p is S_Block);
			bool result5 = exp2.Contains(s);
			bool result6 = exp2.DeepContains(s);

			Assert.IsFalse(result1);
			Assert.IsFalse(result2);
			Assert.IsTrue(result3);
			Assert.IsTrue(result4);
			Assert.IsFalse(result5);
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
			bool result4 = exp1.ToStandardForm(true) is ConstExpression;
			bool result5 = exp2.ToStandardForm(true) is ConstExpression;
			bool result6 = exp3.ToStandardForm(true) is ConstExpression;
			double result7 = (exp1.ToStandardForm(true) as ConstExpression).Value;
			double result8 = (exp2.ToStandardForm(true) as ConstExpression).Value;
			double result9 = (exp3.ToStandardForm(true) as ConstExpression).Value;


			Assert.IsTrue(result1);
			Assert.IsTrue(result2);
			Assert.IsTrue(result3);
			Assert.IsTrue(result4);
			Assert.IsTrue(result5);
			Assert.IsTrue(result6);
		}

		[TestMethod]
		public void FactorOut_Product_AreEqual()
		{
			S_Block s = Expression.S;
			Product p1 = s * s * 4 * s * s as Product;
			Product p2 = 2 * s * 5 * s * s as Product;
			Product p3 = (s ^ 2) * s as Product;
			Product p4 = s * (s ^ 2) as Product;
			Expression expected1 = 4 * s;
			Expression expected2 = 10;
			Expression expected3 = s;
			Expression expected4 = s;

			Expression result1 = p1.FactorOut(s ^ 3);
			Expression result2 = p2.FactorOut(s ^ 3);
			Expression result3 = p3.FactorOut(s ^ 2);
			Expression result4 = p4.FactorOut(s ^ 2);

			Assert.AreEqual(expected1, result1);
			Assert.AreEqual(expected2, result2);
			Assert.AreEqual(expected3, result3);
			Assert.AreEqual(expected4, result4);
		}

		[TestMethod]
		public void FactorOut_Sum_AreEqual()
		{
			S_Block s = Expression.S;
			Expression C1 = new ComponentExpression("C1");
			Expression C2 = new ComponentExpression("C2");
			Sum s1 = s * s + s * 4 * (s^2);
			Sum s2 = 2 * s * s + 5 * s * s;
			Sum s3 = C2 * C1 + C2 * C2;
			Sum s4 = (s ^ 4) * 4 * C2 * C1 * C2 + (s ^ 2) * (C2 * C1 + C2 * C2);
			Product p1 = (s ^ 2) * s3 as Product;
			Expression expected1 = 1 + 4 * s;
			Expression expected2 = 7;
			Expression expected3 = C1 + C2;
			Expression expected4 = (s ^ 2) * (C1 + C2);
			Expression expected5 = (s ^ 4) * 4 * C1 * C2 + (s ^ 2) * (C1 + C2);

			Expression result1 = s1.FactorOut(s^2);
			Expression result2 = s2.FactorOut(s^2);
			Expression result3 = s3.FactorOut(new ComponentExpression("C2"));
			Expression result4 = p1.FactorOut(new ComponentExpression("C2"));
			Expression result5 = s4.FactorOut(new ComponentExpression("C2"));

			Assert.AreEqual(expected1, result1);
			Assert.AreEqual(expected2, result2);
			Assert.AreEqual(expected3, result3);
			Assert.AreEqual(expected4, result4);
			Assert.AreEqual(expected5, result5);
		}

		[TestMethod]
		public void GetCommonFactors_Sum_AreEqual()
		{
			S_Block s = Expression.S;
			Expression C = new ComponentExpression("C");
			Sum s1 = s * s * 4 * s * s + s * 2 * s as Sum;
			Sum s2 = 2 * s * C * s + C * C *  C * s as Sum;
			Sum s3 = new S_Block(3) + C * new S_Block(2) as Sum;
			Sum s4 = (s ^ 6) * C + (s ^ 3) * C * C;
			Sum s5 = (s ^ 7) * C * C * C + (s ^ 5) * C * C;
			List<Expression> expected1 = new List<Expression>() { s*s };
			List<Expression> expected2 = new List<Expression>() { s, C };
			List<Expression> expected3 = new List<Expression>() { new S_Block(2) };
			List<Expression> expected4 = new List<Expression>() { s^3, C };
			List<Expression> expected5 = new List<Expression>() { s^3, C };

			List<Expression> actual1 = s1.GetCommonFactors();
			List<Expression> actual2 = s2.GetCommonFactors();
			List<Expression> actual3 = s3.GetCommonFactors();
			List<Expression> actual4 = s4.GetCommonFactors();
			List<Expression> actual5 = s5.GetCommonFactors(actual4);

			Assert.IsTrue(expected1.All(e => actual1.Count(f => f.Equals(e)) == expected1.Count(f => f.Equals(e))));
			Assert.IsTrue(expected2.All(e => actual2.Count(f => f.Equals(e)) == expected2.Count(f => f.Equals(e))));
			Assert.IsTrue(expected3.All(e => actual3.Count(f => f.Equals(e)) == expected3.Count(f => f.Equals(e))));
			Assert.IsTrue(expected4.All(e => actual4.Count(f => f.Equals(e)) == expected4.Count(f => f.Equals(e))));
			Assert.IsTrue(expected5.All(e => actual5.Count(f => f.Equals(e)) == expected5.Count(f => f.Equals(e))));
		}

		[TestMethod]
		public void ToCommonDenominator_Product_EvaluateEqual()
		{
			Expression s = Expression.S;
			Expression exp1 = ((s + 1) * (s + 3)) / (s * s + 2);
			Expression exp2 = (s + 1) * ((s + 3) / (s * s + 2));

			Expression exp3 = exp2.ToCommonDenominator();

			string result = "((s+1)(s+3))/(s*s+2)";

			Assert.IsTrue(exp1.Equals(exp2));
			Assert.IsTrue(exp2.Equals(exp1));

			Assert.AreEqual(exp1.Evaluate(), result);
			Assert.AreEqual(exp3.Evaluate(), result);
		}

		[TestMethod]
		public void Multiply_ProductDivision_EvaluateEqual()
		{
			Expression s = Expression.S;
			Expression exp1 = (s + 1)*((2) / (s + 3));
			Expression exp2 = (3 + s);

			Expression exp3 = exp1 * exp2;
			string result = "(s+1)*2";

			Assert.AreEqual(exp3.Evaluate(), result);
		}

		[TestMethod]
		public void ComponentCompare_Test()
		{
			Component_Order oldOrder = Expression.ComponentOrder;
			Expression R1 = new ComponentExpression("R1");
			Expression R2 = new ComponentExpression("R2");
			Expression C1 = new ComponentExpression("C1");
			Expression C2 = new ComponentExpression("C2");
			Expression L1 = new ComponentExpression("L1");
			Expression L2 = new ComponentExpression("L2");

			Assert.AreEqual(Expression.Compare(R1, R2), 0);
			Assert.AreEqual(Expression.Compare(C1, C2), 0);
			Assert.AreEqual(Expression.Compare(L1, L2), 0);

			Expression.ComponentOrder = Component_Order.RCL;
			Assert.AreEqual(Expression.Compare(R1, L1), -1);
			Assert.AreEqual(Expression.Compare(R1, C1), -1);
			Assert.AreEqual(Expression.Compare(L1, C1), 1);
			Expression.ComponentOrder = Component_Order.RLC;
			Assert.AreEqual(Expression.Compare(R1, L1), -1);
			Assert.AreEqual(Expression.Compare(R1, C1), -1);
			Assert.AreEqual(Expression.Compare(L1, C1), -1);

			Expression.ComponentOrder = oldOrder;
		}

		[TestMethod]
		public void Component_SetValueStr_Equal()
		{
			Resistor R = new Resistor("R1");
			Inductor L = new Inductor("L1");
			Capacitor C = new Capacitor("C1");

			R.SetValueStr("12.3456709");
			Assert.AreEqual(R.Resistance, 12.3456709);
			R.SetValueStr("12.3456709\x2126");	// Deprecated Unicode Character
			Assert.AreEqual(R.Resistance, 12.3456709);
			R.SetValueStr("12.3456709\u03A9");
			Assert.AreEqual(R.Resistance, 12.3456709);
			R.SetValueStr("1e-2");
			Assert.AreEqual(R.Resistance, 1e-2);
			R.SetValueStr("1e-2mOhm");
			Assert.AreEqual(R.Resistance, 1e-5);
			C.SetValueStr("1n");
			Assert.AreEqual(C.Capacitance, 1e-9);
			L.SetValueStr("1µH");
			Assert.AreEqual(L.Inductance, 1e-6);
			C.SetValueStr("19.23e-2*10^21nF");
			Assert.AreEqual(C.Capacitance, 1.923e11);
			L.SetValueStr("10^8");
			Assert.AreEqual(L.Inductance, 1e8);
			L.SetValueStr("5e2\u00B7"+"10^8");
			Assert.AreEqual(L.Inductance, 5e10);
			R.SetValueStr("4.1e2·10^9Ω");
			Assert.AreEqual(R.Resistance, 4.1e11);
			R.SetValueStr("5.6e2·10^3Ω");	// Different (deprecated) symbol
			Assert.AreEqual(R.Resistance, 5.6e5);
		}

		[TestMethod]
		public void PolynomialDivision_AreEqual()
		{
			S_Block s = Expression.S;
			ComponentExpression L1 = new ComponentExpression("L1");
			ComponentExpression C1 = new ComponentExpression("C1");
			ComponentExpression C2 = new ComponentExpression("C2");
			Sum s1 = (s ^ 3) * L1 * C1 * C2 + s * (C1 + C2) as Sum;
			Sum s2 = (s ^ 2) + s * (C1 + C2) + C1 * C2 as Sum;
			Sum expected1 = (s ^ 2) * L1 * C1 * C2 + C1 + C2;
			Expression expected2 = s;
			Sum expected3 = s + C2 as Sum;

			Sum result1 = Expression.PolynomialDivision(s1, s) as Sum;
			Expression result2 = Expression.PolynomialDivision(s1, expected1);
			Sum result3 = Expression.PolynomialDivision(s2, s + C1) as Sum;

			Assert.AreEqual(result1, expected1);
			Assert.AreEqual(result2, expected2);
			Assert.AreEqual(result3, expected3);
		}
	}
}
