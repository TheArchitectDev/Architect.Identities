using System;
using Xunit;

namespace Architect.Identities.Tests.IdGenerators.Fluids
{
	public sealed class FluidTests
	{
		[Fact]
		public void Construct_Parameterless_ShouldHoldZeroValue()
		{
			var fluid = new Fluid();
			Assert.Equal(0UL, (ulong)fluid);
		}

		[Fact]
		public void Construct_WithZeroUlong_ShouldSucceed()
		{
			_ = new Fluid(0UL);
		}

		[Fact]
		public void Construct_WithZeroLong_ShouldSucceed()
		{
			_ = new Fluid(0L);
		}

		[Fact]
		public void Construct_WithLong_ShouldHoldThatValue()
		{
			var fluid = new Fluid(123L);
			Assert.StrictEqual(123L, (long)(ulong)fluid);
		}

		[Fact]
		public void Construct_WithUlong_ShouldHoldThatValue()
		{
			var fluid = new Fluid(123UL);
			Assert.StrictEqual(123UL, (ulong)fluid);
		}

		[Fact]
		public void NonGenericEquals_WithArgumentNull_ShouldReturnFales()
		{
			var fluid = new Fluid(123L);
			Assert.False(fluid.Equals(null));
		}

		[Fact]
		public void NonGenericEquals_WithRightArgumentNull_ShouldReturnFales()
		{
			var fluid = new Fluid(123L);
			Assert.NotEqual(fluid, (object)null);
		}

		[Fact]
		public void NonGenericEquals_WithLeftArgumentNull_ShouldReturnFales()
		{
			var fluid = new Fluid(123L);
			Assert.NotEqual((object)null, fluid);
		}

		[Fact]
		public void NonGenericEquals_BetweenMatchingUlongAndLongFluid_ShouldReturnTrue()
		{
			var fluid1 = new Fluid(123L);
			var fluid2 = new Fluid(123UL);
			Assert.True(fluid1.Equals((object)fluid2));
		}

		[Fact]
		public void GenericEquals_BetweenMatchingUlongAndLongFluid_ShouldReturnTrue()
		{
			var fluid1 = new Fluid(123L);
			var fluid2 = new Fluid(123UL);
			Assert.True(fluid1.Equals(fluid2));
		}

		[Fact]
		public void GenericEquals_BetweenZeroFluids_ShouldReturnTrue()
		{
			var fluid1 = new Fluid(0L);
			var fluid2 = new Fluid(0UL);
			Assert.True(fluid1.Equals(fluid2));
		}

		[Fact]
		public void GenericEquals_BetweenDefaultAndZeroFluids_ShouldReturnTrue()
		{
			var fluid1 = default(Fluid);
			var fluid2 = new Fluid(0);
			Assert.True(fluid1.Equals(fluid2));
		}

		[Fact]
		public void EqualityOperator_BetweenZeroFluids_ShouldReturnTrue()
		{
			var fluid1 = new Fluid(0L);
			var fluid2 = new Fluid(0UL);
			Assert.True(fluid1 == fluid2);
		}
		
		[Fact]
		public void EqualityOperator_BetweenDefaultAndZeroFluids_ShouldReturnTrue()
		{
			var fluid1 = new Fluid(default);
			var fluid2 = new Fluid(0);
			Assert.True(fluid1 == fluid2);
		}
		
		[Fact]
		public void EqualityOperator_BetweenDefaultFluidAndDefaultLong_ShouldReturnTrue()
		{
			var fluid = default(Fluid);
			var longValue = default(long);
			Assert.True(fluid == longValue);
		}

		[Fact]
		public void EqualityOperator_BetweenMatchingUlongAndLongFluid_ShouldReturnTrue()
		{
			var fluid1 = new Fluid(123L);
			var fluid2 = new Fluid(123UL);
			Assert.True(fluid1 == fluid2);
		}

		[Fact]
		public void NonEqualityOperator_BetweenMatchingUlongAndLongFluid_ShouldReturnFalse()
		{
			var fluid1 = new Fluid(123L);
			var fluid2 = new Fluid(123UL);
			Assert.False(fluid1 != fluid2);
		}

		[Fact]
		public void EqualityOperator_AgainstMatchingUlong_ShouldReturnTrue()
		{
			var fluid = new Fluid(123);
			Assert.True(fluid == 123UL);
		}

		[Fact]
		public void EqualityOperator_AgainstMatchingLong_ShouldReturnTrue()
		{
			var fluid = new Fluid(123);
			Assert.True(fluid == 123L);
		}

		[Fact]
		public void EqualityOperator_AgainstMismatchingUlong_ShouldReturnFalse()
		{
			var fluid = new Fluid(123);
			Assert.False(fluid == 456UL);
		}

		[Fact]
		public void EqualityOperator_AgainstMismatchingLong_ShouldReturnFalse()
		{
			var fluid = new Fluid(123);
			Assert.False(fluid == 456L);
		}

		[Fact]
		public void EqualityOperator_AgainstMismatchingInt_ShouldReturnFalse()
		{
			var fluid = new Fluid(123);
			Assert.False(fluid == 0);
		}

		[Fact]
		public void NonEqualityOperator_AgainstMismatchingInt_ShouldReturnTrue()
		{
			var fluid = new Fluid(123);
			Assert.True(fluid != 0);
		}

		[Fact]
		public void EqualityOperator_AgainstMatchingInt_ShouldReturnTrue()
		{
			var fluid = new Fluid();
			Assert.True(fluid == 0);
		}

		[Fact]
		public void EqualityOperator_AgainstNegativeInt_ShouldReturnFalse()
		{
			var fluid = new Fluid();
			Assert.False(fluid == -1);
		}

		[Fact]
		public void CompareTo_AgainstEqualValue_ShouldReturn0()
		{
			var fluid1 = new Fluid(1);
			var fluid2 = new Fluid(1);
			Assert.True(fluid1.CompareTo(fluid2) == 0);
		}

		[Fact]
		public void CompareTo_AgainstGreaterValue_ShouldReturnMinus1()
		{
			var fluid1 = new Fluid(1);
			var fluid2 = new Fluid(2);
			Assert.True(fluid1.CompareTo(fluid2) == -1);
		}

		[Fact]
		public void CompareTo_AgainstSmallerValue_ShouldReturnPlus1()
		{
			var fluid1 = new Fluid(2);
			var fluid2 = new Fluid(1);
			Assert.True(fluid1.CompareTo(fluid2) == 1);
		}

		[Fact]
		public void HasValue_WithZeroValue_ShouldReturnFalse()
		{
			var fluid = new Fluid();
			Assert.False(fluid.HasValue);
		}

		[Fact]
		public void HasValue_WithNonZeroValue_ShouldReturnTrue()
		{
			var fluid = new Fluid(1);
			Assert.True(fluid.HasValue);
		}

		[Fact]
		public void ImplicitCastToUlong_Regularly_ShouldSucceed()
		{
			var fluid = new Fluid(UInt64.MaxValue);
			ulong _ = fluid;
		}

		[Fact]
		public void ImplicitCastToLong_Regularly_ShouldSucceed()
		{
			var fluid = new Fluid(Int64.MaxValue);
			long _ = fluid;
		}

		[Fact]
		public void CastToLong_WithGreaterThanLongMaxValue_ShouldThrowOverflowException()
		{
			var fluid = new Fluid(Int64.MaxValue + 1UL);
			Assert.Throws<OverflowException>(() => (long)fluid);
		}

		[Fact]
		public void ImplicitCastFromUlong_WithZeroValue_ShouldSucceed()
		{
			Fluid _ = 0UL;
		}

		[Fact]
		public void ImplicitCastFromLong_WithZeroValue_ShouldSucceed()
		{
			Fluid _ = 0L;
		}

		[Fact]
		public void CastFromLong_WithNegativeValue_ShouldThrowArgumentException()
		{
			Assert.Throws<ArgumentException>(() => (Fluid)(-1L));
		}
	}
}
