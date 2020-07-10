using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Architect.Identities.Tests.CompanyUniqueIds
{
	public sealed class CompanyUniqueIdGeneratorTests
	{
		private static readonly DateTime FixedUtcDateTime = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc);
		private static readonly ulong FixedTimestamp = GetTimestamp(FixedUtcDateTime);
		private static readonly ulong EpochTimestamp = 0UL;
		private static readonly RandomSequence6 FixedRandomSequence6 = SimulateRandomSequenceWithValue(1UL << 40);

		private static ulong GetTimestamp(DateTime utcDateTime) => (ulong)(utcDateTime - DateTime.UnixEpoch).TotalMilliseconds;

#pragma warning disable CS0618 // Type or member is obsolete -- Obsolete intended to protect against non-test usage
		private static RandomSequence6 SimulateRandomSequenceWithValue(ulong value) => RandomSequence6.CreatedSimulated(value);
#pragma warning restore CS0618 // Type or member is obsolete

		/// <summary>
		/// A generator with the default dependencies.
		/// </summary>
		private CompanyUniqueIdGenerator DefaultIdGenerator { get; } = new CompanyUniqueIdGenerator();

		private static ulong ExtractTimestampComponent(decimal id)
		{
			// The timestamp portion is the first 6 bytes, so start by extracting the first 8 bytes
			var idBytes = Decimal.GetBits(id);
			var hi = idBytes[2];
			var mid = idBytes[1];
			Span<byte> hiAndMidBytes = stackalloc byte[8];
			BinaryPrimitives.WriteInt32BigEndian(hiAndMidBytes, hi);
			BinaryPrimitives.WriteInt32BigEndian(hiAndMidBytes[4..], mid);
			var hiTwoThirds = BinaryPrimitives.ReadUInt64BigEndian(hiAndMidBytes);

			// The 2 low bytes belong to the random data portion, so shift them off
			var milliseconds = hiTwoThirds >> 16; // 16 bits

			return milliseconds;
		}

		/// <summary>
		/// Extracts the random sequence component from the given ID.
		/// Returns a ulong with the high 2 bits set to zero.
		/// </summary>
		private static ulong ExtractRandomSequenceComponent(decimal id)
		{
			// The random portion is the last 6 bytes, so start by extracting the last 8 bytes
			var idBytes = Decimal.GetBits(id);
			var mid = idBytes[1];
			var lo = idBytes[0];
			Span<byte> midAndLoBytes = stackalloc byte[8];
			BinaryPrimitives.WriteInt32BigEndian(midAndLoBytes, mid);
			BinaryPrimitives.WriteInt32BigEndian(midAndLoBytes[4..], lo);
			var lowTwoThirds = BinaryPrimitives.ReadUInt64BigEndian(midAndLoBytes);

			// The high 2 bytes belong to the timestamp portion, so shift them off
			var randomSequence = lowTwoThirds << 16 >> 16;

			return randomSequence;
		}

		[Fact]
		public void CreateCore_Regularly_ShouldUseEpochToCalculateMilliseconds()
		{
			var id = this.DefaultIdGenerator.CreateCore(EpochTimestamp, FixedRandomSequence6);

			var milliseconds = ExtractTimestampComponent(id);

			Assert.Equal(0UL, milliseconds);
		}

		[Fact]
		public void CreateCore_WithOverflowingDateTime_ShouldThrow()
		{
			const int dateTimeBitCount = 45;

			var firstOverflowingDateTime = DateTime.UnixEpoch.AddMilliseconds(1UL << dateTimeBitCount);
			var firstOverflowingTimestamp = GetTimestamp(firstOverflowingDateTime);

			var maxPermittedDateTime = firstOverflowingDateTime.AddMilliseconds(-1);
			var maxPermittedTimestamp = GetTimestamp(maxPermittedDateTime);

			this.DefaultIdGenerator.CreateCore(maxPermittedTimestamp, FixedRandomSequence6); // Should not throw
			Assert.Throws<InvalidOperationException>(() => this.DefaultIdGenerator.CreateCore(firstOverflowingTimestamp, randomSequence: default));
		}

		[Fact]
		public void CreateCore_Regularly_ShouldStoreTimestampMillisecondsInHigh6Bytes()
		{
			var id = this.DefaultIdGenerator.CreateCore(FixedTimestamp, FixedRandomSequence6);

			var milliseconds = ExtractTimestampComponent(id);

			Assert.Equal(FixedTimestamp, milliseconds);
		}

		[Fact]
		public void CreateCore_Regularly_ShouldStoreRandomSequenceInLow6Bytes()
		{
			const ulong fixedRandomSequence = UInt64.MaxValue >> 16; // The high 2 bytes should be zero and will remain unused
			var fixedRandomSequence6 = SimulateRandomSequenceWithValue(fixedRandomSequence);

			var id = this.DefaultIdGenerator.CreateCore(EpochTimestamp, fixedRandomSequence6);

			var resultingRandomSequence = ExtractRandomSequenceComponent(id);

			Assert.Equal(fixedRandomSequence, resultingRandomSequence);
		}

		/// <summary>
		/// This tests checks that we do not accidentally output the padding (i.e. cut off actual random bytes).
		/// </summary>
		[Fact]
		public void CreateCore_Regularly_ShouldCutOffPadding()
		{
			const ulong fixedRandomSequence = UInt64.MaxValue >> 16; // The high 2 bytes should be zero and will remain unused
			var fixedRandomSequence6 = SimulateRandomSequenceWithValue(fixedRandomSequence);

			var id = this.DefaultIdGenerator.CreateCore(EpochTimestamp, fixedRandomSequence6);

			var resultingRandomSequence = ExtractRandomSequenceComponent(id); // Note: High 2 bytes belong to the timestamp component
			Span<byte> randomSequence = stackalloc byte[8];
			BinaryPrimitives.WriteUInt64BigEndian(randomSequence, resultingRandomSequence);
			randomSequence = randomSequence[2..]; // High 2 bytes belong to the timestamp component

			Assert.False(randomSequence.StartsWith(new byte[2]));
			Assert.False(randomSequence.EndsWith(new byte[2]));
		}

		[Fact]
		public void AwaitUpdatedClockValue_Regularly_ShouldSleepUntilClockHasChanged()
		{
			var clockValues = new[] { /* Initial time */ FixedUtcDateTime, /* After first sleep */ FixedUtcDateTime, /* After second sleep */ FixedUtcDateTime.AddHours(1) };
			var clockValueIndex = 0;

			var sumSleepMilliseconds = 0;

			var generator = new CompanyUniqueIdGenerator(utcClock: () => clockValues[clockValueIndex++], sleepAction: milliseconds => sumSleepMilliseconds += milliseconds);

			// Populate the previous timestamp
			generator.CreateId();

			generator.AwaitUpdatedClockValue();

			Assert.Equal(2, sumSleepMilliseconds);
		}

		/// <summary>
		/// We do not care if the clock went forward or backward, as long as it has changed.
		/// The risk of collisions is extremely low, so there is no reason to protect against clock rewinds.
		/// </summary>
		[Fact]
		public void AwaitUpdatedClockValue_WithRewindingClock_ShouldStopSleepingEvenOnRewind()
		{
			var clockValues = new[] { /* Initial time */ FixedUtcDateTime.AddMilliseconds(1), /* After first sleep */ FixedUtcDateTime.AddMilliseconds(1), /* After second sleep */ FixedUtcDateTime };
			var clockValueIndex = 0;

			var sumSleepMilliseconds = 0;

			var generator = new CompanyUniqueIdGenerator(utcClock: () => clockValues[clockValueIndex++], sleepAction: milliseconds => sumSleepMilliseconds += milliseconds);

			// Populate the previous timestamp
			generator.CreateId();

			generator.AwaitUpdatedClockValue();

			Assert.Equal(2, sumSleepMilliseconds);
		}

		[Fact]
		public void CreateId_InYear3000_ShouldSucceed()
		{
			var generator = new CompanyUniqueIdGenerator(utcClock: () => new DateTime(3000, 01, 01, 0, 0, 0, DateTimeKind.Utc));

			var id = generator.CreateId();

			Assert.True(id < CompanyUniqueIdGenerator.MaxValue);
		}

		[Fact]
		public void CreateId_InYear4000_ShouldOverflow()
		{
			var generator = new CompanyUniqueIdGenerator(utcClock: () => new DateTime(4000, 01, 01, 0, 0, 0, DateTimeKind.Utc));

			Assert.Throws<InvalidOperationException>(() => generator.CreateId());
		}

		[Fact]
		public void CreateId_ManyTimes_ShouldCreateUniqueValues()
		{
			var results = new List<decimal>();
			for (var i = 0; i < 2000; i++)
				results.Add(this.DefaultIdGenerator.CreateId());

			Assert.Equal(results.Count, results.Distinct().Count());
		}

		[Fact]
		public void CreateId_SubsequentlyBeyondRateLimit_ShouldReturnIncrementalValues()
		{
			var generator = new CompanyUniqueIdGenerator();

			var results = new List<decimal>();
			for (var i = 0; i < 10 + CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
				results.Add(generator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnSameTimestampWithinRateLimit_ShouldReturnIncrementalValues()
		{
			var generator = new CompanyUniqueIdGenerator(utcClock: () => FixedUtcDateTime);

			var results = new List<decimal>();
			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
				results.Add(generator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnIncrementalValues()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new CompanyUniqueIdGenerator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

			var results = new List<decimal>();
			for (var i = 0; i < 100; i++)
				results.Add(generator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnSameTimestampWithinRateLimit_ShouldReturnIdenticalTimestampComponents()
		{
			var generator = new CompanyUniqueIdGenerator(utcClock: () => FixedUtcDateTime);

			var results = new List<ulong>();
			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
				results.Add(ExtractTimestampComponent(generator.CreateId()));

			for (var i = 1; i < results.Count; i++)
				Assert.Equal(results[i], results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnIncrementalTimestampComponents()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new CompanyUniqueIdGenerator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

			var results = new List<ulong>();
			for (var i = 0; i < 100; i++)
				results.Add(ExtractTimestampComponent(generator.CreateId()));

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		/// <summary>
		/// Test correct behavior where the clock is increasing (such as just 1 tick) but not increasing by a full millisecond.
		/// </summary>
		[Fact]
		public void CreateId_SubsequentlyOnSameMillisecondButDifferentTick_ShouldReturnIncrementalRandomSequenceComponents()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new CompanyUniqueIdGenerator(utcClock: () => dateTime = dateTime.AddTicks(1));

			var results = new List<ulong>();
			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
				results.Add(ExtractRandomSequenceComponent(generator.CreateId()));

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		/// <summary>
		/// On the same timestamp, within the rate limit, we produce the same random sequence as before, incremented by a smaller random sequence.
		/// </summary>
		[Fact]
		public void CreateId_SubsequentlyOnSameTimestampWithinRateLimit_ShouldReturnIncrementalRandomSequenceComponents()
		{
			var generator = new CompanyUniqueIdGenerator(utcClock: () => FixedUtcDateTime);

			var results = new List<ulong>();
			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
				results.Add(ExtractRandomSequenceComponent(generator.CreateId()));

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		/// <summary>
		/// On different timestamps, we produce random sequences that are simply 6 random bytes, with no relationship whatsoever.
		/// </summary>
		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnRandomSequenceComponentsThatArePurelyRandom()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new CompanyUniqueIdGenerator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

			var results = new List<ulong>();
			for (var i = 0; i < 100; i++)
				results.Add(ExtractRandomSequenceComponent(generator.CreateId()));

			var equalCount = 0;
			var incrementalCount = 0;
			for (var i = 1; i < results.Count; i++)
			{
				if (results[i] == results[i - 1]) equalCount++;
				if (results[i] > results[i - 1]) incrementalCount++;
			}

			Assert.Equal(0, equalCount);
			Assert.True(incrementalCount < 95); // On average, 50% will be incremental
		}

		[Fact]
		public void CreateId_WithinRateLimit_ShouldNotSleep()
		{
			var sleepCount = 0;
			var generator = new CompanyUniqueIdGenerator(sleepAction: _ => sleepCount++);

			for (var i = 0; i < CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
				this.DefaultIdGenerator.CreateId();

			Assert.Equal(0, sleepCount);
		}

		[Fact]
		public void CreateId_OnePlusRateLimitTimes_ShouldSleepOneMillisecond()
		{
			var clockValue = FixedUtcDateTime; // The clock value normally stays the same
			var sumSleepMilliseconds = 0;
			var generator = new CompanyUniqueIdGenerator(utcClock: () => clockValue, sleepAction: milliseconds =>
			{
				sumSleepMilliseconds += milliseconds;
				clockValue = clockValue.AddMilliseconds(milliseconds); // The clock advances when we sleep
			});

			for (var i = 0; i < 1 + CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
				generator.CreateId();

			Assert.Equal(1, sumSleepMilliseconds);
		}

		[Fact]
		public void CreateId_OnePlusTwiceRateLimitTimes_ShouldSleepTwoMillisecond()
		{
			var clockValue = FixedUtcDateTime; // The clock value normally stays the same
			var sumSleepMilliseconds = 0;
			var generator = new CompanyUniqueIdGenerator(utcClock: () => clockValue, sleepAction: milliseconds =>
			{
				sumSleepMilliseconds += milliseconds;
				clockValue = clockValue.AddMilliseconds(milliseconds); // The clock advances when we sleep
			});

			for (var i = 0; i < 1 + 2 * CompanyUniqueIdGenerator.RateLimitPerTimestamp; i++)
				generator.CreateId();

			Assert.Equal(2, sumSleepMilliseconds);
		}

		/// <summary>
		/// The resulting IDs, when <see cref="Decimal.ToString()"/> is invoked on them, should conform to <see cref="NumberStyles.None"/>.
		/// </summary>
		[Fact]
		public void CreateId_Regularly_ShouldProvideIdWhereToStringHasNumberFormatNone()
		{
			var id = this.DefaultIdGenerator.CreateId();

			var idString = id.ToString();

			Assert.True(idString.All(chr => chr >= '0' && chr <= '9'));
		}
	}
}
