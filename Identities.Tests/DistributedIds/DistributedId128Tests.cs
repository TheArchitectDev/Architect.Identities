using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	public sealed class DistributedId128Tests
	{
		[Theory]
		[InlineData("9223372036854775808")] // 2^63
		[InlineData("18446744073709551615")] // UInt64.MaxValue
		[InlineData("170141183460469231731687303715884105728")] // 2^127
		[InlineData("340282366920938463463374607431768211455")] // UInt128.MaxValue
		public void Split_WithTopBitOfAnyHalfSet_ShouldThrow(string uint128IdString)
		{
			var id = UInt128.Parse(uint128IdString);
			Assert.Throws<ArgumentException>(() => DistributedId128.Split(id));
		}

		[Theory]
		[InlineData("0", 0L, 0L)]
		[InlineData("1", 0L, 1L)]
		[InlineData("2147483647", 0L, Int32.MaxValue)] // Int32.MaxValue
		[InlineData("18446744073709551616", 1L, 0L)] // UInt64.MaxValue + 1
		[InlineData("9223372036854775807", 0L, Int64.MaxValue)] // 2^63 - 1
		[InlineData("170141183460469231713240559642174554112", Int64.MaxValue, 0L)] // (2^63 - 1) << 64
		[InlineData("170141183460469231722463931679029329919", Int64.MaxValue, Int64.MaxValue)] // (2^63 - 1) << 64 + (2^63 - 1)
		public void Split_WithTopBitsOfHalvesUnset_ShouldReturnExpectedResult(string uint128IdString, long expectedUpper, long expectedLower)
		{
			var id = UInt128.Parse(uint128IdString);

			var (upper, lower) = DistributedId128.Split(id);

			Assert.Equal(expectedUpper, upper);
			Assert.Equal(expectedLower, lower);
		}

		[Theory]
		[InlineData(-1L, 0L)]
		[InlineData(0L, -1L)]
		[InlineData(Int32.MinValue, 0L)]
		[InlineData(0L, Int32.MinValue)]
		[InlineData(-1L, -1L)]
		[InlineData(Int32.MinValue, Int32.MinValue)]
		public void Join_WithNegativeValue_ShouldThrow(long upper, long lower)
		{
			Assert.Throws<ArgumentException>(() =>  DistributedId128.Join(upper, lower));
		}

		[Theory]
		[InlineData("0", 0L, 0L)]
		[InlineData("1", 0L, 1L)]
		[InlineData("2147483647", 0L, Int32.MaxValue)] // Int32.MaxValue
		[InlineData("18446744073709551616", 1L, 0L)] // UInt64.MaxValue + 1
		[InlineData("9223372036854775807", 0L, Int64.MaxValue)] // 2^63 - 1
		[InlineData("170141183460469231713240559642174554112", Int64.MaxValue, 0L)] // (2^63 - 1) << 64
		[InlineData("170141183460469231722463931679029329919", Int64.MaxValue, Int64.MaxValue)] // (2^63 - 1) << 64 + (2^63 - 1)
		public void Join_AfterSplit_ShouldReverse(string expectedUint128IdString, long upper, long lower)
		{
			var expectedResult = UInt128.Parse(expectedUint128IdString);

			var result = DistributedId128.Join(upper, lower);

			Assert.Equal(expectedResult, result);
		}
	}
}
