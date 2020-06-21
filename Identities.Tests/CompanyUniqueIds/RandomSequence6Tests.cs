using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Architect.Identities.Tests.CompanyUniqueIds
{
	public sealed class RandomSequence6Tests
	{
#pragma warning disable CS0618 // Type or member is obsolete -- Obsolete intended to protect against non-test usage
		private static RandomSequence6 SimulateRandomSequenceWithValue(ulong value) => RandomSequence6.CreatedSimulated(value);
#pragma warning restore CS0618 // Type or member is obsolete

		[Fact]
		public void Create_Regularly_ShouldBeNonzero()
		{
			var result = RandomSequence6.Create();

			Assert.NotEqual(0UL, result);
		}

		[Fact]
		public void Create_Regularly_ShouldHaveHighEntropyInLow6Bits()
		{
			var results = new List<ulong>();
			for (var i = 0; i < 100; i++)
				results.Add(RandomSequence6.Create());

			var sumValuesPerByte = new int[6];
			foreach (var result in results)
			{
				Span<byte> bytes = stackalloc byte[8];
				BinaryPrimitives.WriteUInt64BigEndian(bytes, result);
				for (var i = 0; i < 6; i++)
					sumValuesPerByte[i] += bytes[2 + i];
			}

			var averageValuesPerByte = new int[sumValuesPerByte.Length];
			for (var i = 0; i < sumValuesPerByte.Length; i++)
				averageValuesPerByte[i] = sumValuesPerByte[i] / results.Count;

			// Each byte should have an average value close to 127
			foreach (var value in averageValuesPerByte)
				Assert.True(value >= 102 && value <= 152);

			// The average of the averages should be very close to 127
			var totalAverage = averageValuesPerByte.Average();
			Assert.True(totalAverage >= 120 && totalAverage <= 134);
		}

		[Fact]
		public void Create_Regularly_ShouldLeaveHigh2BytesZero()
		{
			var result = RandomSequence6.Create();

			Assert.Equal(0UL, result >> (64 - 16));
		}

		[Fact]
		public void CastToUlong_Regularly_ShouldLeave2HighBytesZero()
		{
			var result = SimulateRandomSequenceWithValue(UInt64.MaxValue >> 16);

			Assert.Equal(0UL, result >> (64 - 16));
		}

		[Fact]
		public void CastToUlong_Regularly_ShouldMatchRandomData()
		{
			var randomValue = (ulong)UInt32.MaxValue >> 16;
			var result = SimulateRandomSequenceWithValue(randomValue);

			Assert.Equal(randomValue, result);
		}

		[Theory]
		[InlineData(UInt64.MaxValue >> 32, UInt32.MaxValue, (ulong)UInt32.MaxValue + UInt32.MaxValue)]
		[InlineData(UInt64.MaxValue >> 32, 5, (ulong)UInt32.MaxValue + 5)]
		[InlineData((UInt64.MaxValue >> 32) - 101, UInt32.MaxValue, (ulong)UInt32.MaxValue + UInt32.MaxValue - 101)]
		[InlineData((UInt64.MaxValue >> 32) - 101, 500, (ulong)UInt32.MaxValue + 500 - 101)]
		public void Add4RandomBits_WithoutOverflow_ShouldReturnExpectedResult(ulong initialValue, uint increment, ulong expectedResult)
		{
			var left = SimulateRandomSequenceWithValue(initialValue);
			var right = SimulateRandomSequenceWithValue(increment);
			var result = (ulong)left.Add4RandomBytes(right);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void Add4RandomBits_RateLimitTimesWithoutOverflow_ShouldReturnDistinctResults()
		{
			var results = new List<ulong>();

			var left = SimulateRandomSequenceWithValue(UInt64.MaxValue >> 32);
			var right = SimulateRandomSequenceWithValue(UInt32.MaxValue);

			results.Add(left);

			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
			{
				left = left.Add4RandomBytes(right);
				results.Add(left);
			}

			Assert.Equal(1 + (int)CompanyUniqueIdGenerator.RateLimitPerTimestamp, results.Distinct().Count());
		}

		[Fact]
		public void Add4RandomBits_RateLimitTimesWithoutOverflow_ShouldReturnIncrementalResults()
		{
			var results = new List<ulong>();

			var left = SimulateRandomSequenceWithValue(UInt64.MaxValue >> 32);
			var right = SimulateRandomSequenceWithValue(UInt32.MaxValue);

			results.Add(left);

			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
			{
				left = left.Add4RandomBytes(right);
				results.Add(left);
			}

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Theory]
		[InlineData(UInt64.MaxValue >> 16, UInt32.MaxValue, UInt32.MaxValue)]
		[InlineData(UInt64.MaxValue >> 16, 5, 5)]
		[InlineData((UInt64.MaxValue >> 16) - 101, UInt32.MaxValue, UInt32.MaxValue - 101)]
		[InlineData((UInt64.MaxValue >> 16) - 101, 500, 500 - 101)]
		public void Add4RandomBits_WithOverflow_ShouldReturnExpectedResult(ulong initialValue, uint increment, ulong expectedResult)
		{
			var left = SimulateRandomSequenceWithValue(initialValue);
			var right = SimulateRandomSequenceWithValue(increment);
			var result = (ulong)left.Add4RandomBytes(right);

			Assert.Equal(0UL, result >> (64 - 16)); // High 2 bytes are zero
			Assert.Equal(expectedResult, result); // Expected value
		}

		[Fact]
		public void Add4RandomBits_RateLimitTimesWithOverflow_ShouldReturnDistinctResults()
		{
			var results = new List<ulong>();

			var left = SimulateRandomSequenceWithValue(UInt64.MaxValue >> 16);
			var right = SimulateRandomSequenceWithValue(UInt32.MaxValue);

			results.Add(left);

			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
			{
				left = left.Add4RandomBytes(right);
				results.Add(left);
			}

			Assert.Equal(1 + (int)CompanyUniqueIdGenerator.RateLimitPerTimestamp, results.Distinct().Count());
		}

		[Fact]
		public void Add4RandomBits_RateLimitTimesWithOverflow_ShouldReturnIncrementalResultsExceptAfterFirstIncrement()
		{
			var results = new List<ulong>();

			var left = SimulateRandomSequenceWithValue(UInt64.MaxValue >> 16);
			var right = SimulateRandomSequenceWithValue(UInt32.MaxValue);

			results.Add(left);

			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
			{
				left = left.Add4RandomBytes(right);
				results.Add(left);
			}

			// After the first increment, we overflow and get a smaller result
			Assert.True(results[1] < results[0]);

			// The rest should be incremental
			for (var i = 2; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}
	}
}
