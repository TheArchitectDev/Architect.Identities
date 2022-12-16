using System.Buffers.Binary;
using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	public sealed class RandomSequence6Tests
	{
		private static readonly int SafeRateLimitPerTimestamp = DistributedIdGenerator.AverageRateLimitPerTimestamp / 2;

#pragma warning disable CS0618 // Type or member is obsolete -- Obsolete intended to protect against non-test usage
		private static RandomSequence6 SimulateRandomSequenceWithValue(ulong value) => RandomSequence6.CreatedSimulated(value);
#pragma warning restore CS0618 // Type or member is obsolete

		[Fact]
		public void Create_Regularly_ShouldBeNonzero()
		{
			var result = RandomSequence6.Create();

			Assert.NotEqual(0UL, result);
		}

		/// <summary>
		/// Unfortunately non-deterministic.
		/// </summary>
		[Fact]
		public void Create_Regularly_ShouldHaveHighEntropyInLow5Bytes()
		{
			var results = new List<ulong>();
			for (var i = 0; i < 100; i++)
				results.Add(RandomSequence6.Create());

			var sumValuesPerByte = new int[5];
			Span<byte> bytes = stackalloc byte[8];
			foreach (var result in results)
			{
				BinaryPrimitives.WriteUInt64BigEndian(bytes, result);
				for (var i = 0; i < sumValuesPerByte.Length; i++)
					sumValuesPerByte[i] += bytes[8 - sumValuesPerByte.Length + i];
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
		public void Create_Regularly_ShouldLeaveHigh16BitsZero()
		{
			var result = RandomSequence6.Create();

			Assert.Equal(0UL, result >> (64 - 16));
		}

		[Fact]
		public void CastToUlong_Regularly_ShouldLeave2HighBytesZero()
		{
			var result = SimulateRandomSequenceWithValue(UInt64.MaxValue >> 16);

			var ulongValue = (ulong)result;

			Assert.Equal(0UL, ulongValue >> (64 - 16));
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
		public void TryAddRandomBits_WithoutOverflow_ShouldProduceExpectedResult(ulong initialValue, uint increment, ulong expectedResult)
		{
			var left = SimulateRandomSequenceWithValue(initialValue);
			var right = SimulateRandomSequenceWithValue(increment);
			var didSucceed = left.TryAddRandomBits(right, out var result);

			Assert.True(didSucceed);
			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void TryAddRandomBits_WithOverflow_ShouldReturnFalse()
		{
			var left = SimulateRandomSequenceWithValue((UInt64.MaxValue >> 16) - 2UL);
			var right = SimulateRandomSequenceWithValue(3UL);
			var didSucceed = left.TryAddRandomBits(right, out _);

			Assert.False(didSucceed);
		}

		[Fact]
		public void TryAddRandomBits_WithinRateLimitTimesWithoutOverflow_ShouldReturnDistinctResults()
		{
			var results = new List<ulong>();

			for (var x = 0; x < 20; x++) // Because probability
			{
				var left = SimulateRandomSequenceWithValue(UInt64.MaxValue >> 32);
				var right = SimulateRandomSequenceWithValue(UInt32.MaxValue);

				results.Add(left);

				var didAddBits  = true;

				for (var i = 0; i < SafeRateLimitPerTimestamp && didAddBits; i++)
				{
					didAddBits = left.TryAddRandomBits(right, out left);
					results.Add(left);
				}

				if (didAddBits) break;
			}

			Assert.Equal(1 + SafeRateLimitPerTimestamp, results.Distinct().Count());
		}

		[Fact]
		public void TryAddRandomBits_WithinRateLimitTimesWithoutOverflow_ShouldReturnIncrementalResults()
		{
			var results = new List<ulong>();

			for (var x = 0; x < 20; x++) // Because probability
			{
				var left = SimulateRandomSequenceWithValue(UInt64.MaxValue >> 32);
				var right = SimulateRandomSequenceWithValue(UInt32.MaxValue);

				results.Add(left);

				var didAddBits = true;

				for (var i = 0; i < SafeRateLimitPerTimestamp && didAddBits; i++)
				{
					didAddBits = left.TryAddRandomBits(right, out left);
					results.Add(left);
				}

				if (didAddBits) break;
			}

			Assert.Equal(1 + SafeRateLimitPerTimestamp, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}
	}
}
