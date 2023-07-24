using System.Buffers.Binary;
using Xunit;

namespace Architect.Identities.Tests.DistributedId128s
{
	public sealed class DistributedId128GeneratorTests
	{
		private static readonly DateTime FixedUtcDateTime = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc);
		private static readonly ulong FixedTimestamp = GetTimestamp(FixedUtcDateTime);
		private static readonly ulong EpochTimestamp = 0UL;
		private static readonly RandomSequence75 FixedRandomSequence6 = SimulateRandomSequenceWithValue(high: 1UL << 10, low: 0UL);
		private static readonly RandomSequence75 MaxRandomSequence75 = SimulateRandomSequenceWithValue(high: RandomSequence75.MaxHighValue, low: UInt64.MaxValue);

		private static ulong GetTimestamp(DateTime utcDateTime) => (ulong)(utcDateTime - DateTime.UnixEpoch).TotalMilliseconds;

#pragma warning disable CS0618 // Type or member is obsolete -- Obsolete intended to protect against non-test usage
		private static RandomSequence75 SimulateRandomSequenceWithValue(ulong high, ulong low) => RandomSequence75.CreatedSimulated(high: high, low: low);
#pragma warning restore CS0618 // Type or member is obsolete

		/// <summary>
		/// A generator with the default dependencies.
		/// </summary>
		private DistributedId128Generator DefaultIdGenerator { get; } = new DistributedId128Generator();

		private static ulong ExtractTimestampComponent(Guid id)
		{
			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);

			var milliseconds = BinaryPrimitives.ReadUInt64BigEndian(bytes) >> (64 - 48);
			return milliseconds;
		}

		/// <summary>
		/// Extracts the version indicator from the given ID.
		/// Returns a byte with the top 4 bits set to the version, with the rest being irrelevant.
		/// </summary>
		private static byte ExtractVersion(Guid id)
		{
			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);

			var version = bytes[6];
			return version;
		}

		/// <summary>
		/// Extracts the variant indicator from the given ID.
		/// Returns a byte whose top bit should be set to zero, with the rest then being irrelevant.
		/// </summary>
		private static byte ExtractVariant(Guid id)
		{
			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);

			var variant = bytes[8];
			return variant;
		}

		/// <summary>
		/// Extracts the random sequence component from the given ID.
		/// Returns a high ulong with the top 53 bits set to zero, and a low ulong.
		/// </summary>
		private static (ulong High, ulong Low) ExtractRandomSequenceComponent(Guid id)
		{
			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);

			var high = (BinaryPrimitives.ReadUInt64BigEndian(bytes) >> 1) & 0b11111111111UL; // 11 bits

			var low = BinaryPrimitives.ReadUInt64BigEndian(bytes) << 63; // 1 LSB from high half
			low |= BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]); // 63 LSB from low half (top bit should never be set)

			return (high, low);
		}

		[Fact]
		public void CreateCore_Regularly_ShouldUseEpochToCalculateMilliseconds()
		{
			var id = this.DefaultIdGenerator.CreateCore(EpochTimestamp, FixedRandomSequence6);

			var milliseconds = ExtractTimestampComponent(id);

			Assert.Equal(0UL, milliseconds);
		}

		[Fact]
		public void CreateCore_Regularly_ShouldStoreTimestampMillisecondsInTop48Bits()
		{
			var id = this.DefaultIdGenerator.CreateCore(FixedTimestamp, FixedRandomSequence6);

			var milliseconds = ExtractTimestampComponent(id);

			Assert.Equal(FixedTimestamp, milliseconds);
		}

		[Fact]
		public void CreateCore_Regularly_ShouldStoreRandomSequenceInExpectedBits()
		{
			const ulong highRandomSequence = UInt64.MaxValue >> (64 - 11);
			const ulong lowRandomSequence = UInt64.MaxValue;
			var fixedRandomSequence6 = SimulateRandomSequenceWithValue(high: highRandomSequence, low: lowRandomSequence);

			var id = this.DefaultIdGenerator.CreateCore(EpochTimestamp, fixedRandomSequence6);

			var (highResult, lowResult) = ExtractRandomSequenceComponent(id);

			Assert.Equal(highRandomSequence, highResult);
			Assert.Equal(lowRandomSequence, lowResult);
		}

		[Fact]
		public void CreateId_WithExhaustedRandomSequence_ShouldSleepUntilClockHasAdvanced()
		{
			var clockValues = new[]
			{
				/* First ID */ FixedUtcDateTime,
				/* Second ID */ FixedUtcDateTime, // Cannot increment with maxed out randomness and same timestamp, so sleep
				/* Second ID */ FixedUtcDateTime.AddMicroseconds(999), // Sleep again (microsecond increase only, but not a whole millisecond)
				/* Second ID */ FixedUtcDateTime.AddMilliseconds(1), // Success
				/* Final assertion */ FixedUtcDateTime.AddMilliseconds(2),
			};
			var clockValueIndex = 0;
			DateTime GetClockValue() => clockValues[clockValueIndex++];

			var sumSleepMilliseconds = 0;

			var generator = new DistributedId128Generator(utcClock: GetClockValue, sleepAction: milliseconds => sumSleepMilliseconds += milliseconds);

			var firstId = generator.CreateId();

			generator.PreviousRandomSequence = MaxRandomSequence75; // Ensure that no more random bits can be added

			var secondId = generator.CreateId();

			Assert.True(secondId > firstId); // Generator should increment as normal
			Assert.Equal(2, sumSleepMilliseconds); // Generated should have slept the expected number of times
			Assert.Equal(FixedUtcDateTime.AddMilliseconds(2), GetClockValue()); // Clock should have been queried the expected number of times
		}

		/// <summary>
		/// If the clock is adjusted backwards too far, we give up waiting and start anew.
		/// </summary>
		[Fact]
		public void CreateId_WithRewindingClock_ShouldSleepOrResetAsExpected()
		{
			var clockValues = new[]
			{
				/* First ID */ FixedUtcDateTime, // Will use timestamp FixedUtcDateTime
				/* Second ID */ FixedUtcDateTime.AddMilliseconds(-1), // Will use timestamp FixedUtcDateTime-1ms
				/* Third ID */ FixedUtcDateTime.AddMilliseconds(-999), // Requires FixedUtcDateTime+1ms, so will sleep for 1000 ms (barely permitted)
				/* Third ID after sleep */ FixedUtcDateTime.AddMilliseconds(-1000), // Would have to sleep for 1001 (more than permitted), so will give up and reset to a smaller ID
				/* Final assertion */ FixedUtcDateTime.AddDays(-1),
			};
			var clockValueIndex = 0;
			DateTime GetClockValue() => clockValues[clockValueIndex++];

			var sumSleepMilliseconds = 0;

			var generator = new DistributedId128Generator(utcClock: GetClockValue, sleepAction: milliseconds => sumSleepMilliseconds += milliseconds);

			var firstId = generator.CreateId();

			generator.PreviousRandomSequence = MaxRandomSequence75; // Ensure that no more random bits can be added

			var secondId = generator.CreateId();

			Assert.Equal(2, sumSleepMilliseconds); // Generated should have slept the expected number of times
			Assert.True(secondId < firstId); // Generator could increment as normal
			Assert.Equal(FixedUtcDateTime.AddDays(-1), GetClockValue()); // Clock should have been queried the expected number of times
		}

		/// <summary>
		/// Convertible to a DECIMAL(38) with no leading zeros already in year 2023.
		/// </summary>
		[Fact]
		public void CreateId_InYear2023_ShouldConsistOf38Digits()
		{
			var generator = new DistributedId128Generator(utcClock: () => new DateTime(2023, 01, 01, 0, 0, 0, DateTimeKind.Utc));

			var id = generator.CreateId();

			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);

			var numericId = new UInt128(upper: BinaryPrimitives.ReadUInt64BigEndian(bytes), lower: BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]));
			var length = numericId.ToString().Length;

			Assert.Equal(38, length);
		}

		/// <summary>
		/// Convertible to a DECIMAL(38) until year 4000+.
		/// </summary>
		[Fact]
		public void CreateId_InYear4000_ShouldFitInDecimal38()
		{
			var generator = new DistributedId128Generator(utcClock: () => new DateTime(4000, 01, 01, 0, 0, 0, DateTimeKind.Utc));

			var id = generator.CreateId();

			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);

			var result = new UInt128(upper: BinaryPrimitives.ReadUInt64BigEndian(bytes), lower: BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]));

			Assert.True(result < DistributedId128Generator.MaxValueToFitInDecimal38);
		}

		/// <summary>
		/// Convertible to two signed longs that are positive until year 6000+.
		/// </summary>
		[Fact]
		public void CreateId_InYear6000_ShouldHaveBit0AndBit64SetToZero()
		{
			var generator = new DistributedId128Generator(utcClock: () => new DateTime(6000, 01, 01, 0, 0, 0, DateTimeKind.Utc));

			var id = generator.CreateId();

			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);

			var left = BinaryPrimitives.ReadUInt64BigEndian(bytes);
			var right = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);

			Assert.Equal(0UL, left >> 63);
			Assert.Equal(0UL, right >> 63);
		}

		[Fact]
		public void CreateId_InYear9999_ShouldSucceed()
		{
			var generator = new DistributedId128Generator(utcClock: () => new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc));

			var id = generator.CreateId();

			Span<byte> bytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, bytes);

			var result = new UInt128(upper: BinaryPrimitives.ReadUInt64BigEndian(bytes), lower: BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]));

			// Should have succeeded, but should not fit in DECIMAL(38)
			Assert.True(result > DistributedId128Generator.MaxValueToFitInDecimal38);
		}

		[Fact]
		public void CreateId_ManyTimes_ShouldCreateUniqueValues()
		{
			var results = new List<Guid>();
			for (var i = 0; i < 2000; i++)
				results.Add(this.DefaultIdGenerator.CreateId());

			Assert.Equal(results.Count, results.Distinct().Count());
		}

		[Fact]
		public void CreateId_ManyTimes_ShouldCreateValuesWithIncrementalStringRepresentation()
		{
			var results = new List<Guid>();
			for (var i = 0; i < 2000; i++)
				results.Add(this.DefaultIdGenerator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i].ToString().CompareTo(results[i - 1].ToString()) > 0);
		}

		[Fact]
		public void CreateId_SubsequentlyBeyondRateLimit_ShouldReturnIncrementalValues()
		{
			var generator = new DistributedId128Generator();

			var results = new List<Guid>();
			for (var i = 0; i < 1_000_100; i++)
				results.Add(generator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);

			var dateTime = FixedUtcDateTime;
			generator = new DistributedId128Generator(utcClock: () => dateTime, sleepAction: _ => dateTime = dateTime.AddMilliseconds(1));

			results.Clear();
			for (var i = 0; i < 1_000_100; i++)
				results.Add(generator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnSameTimestampWithinRateLimit_ShouldReturnIncrementalValues()
		{
			var didSleep = false;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime, sleepAction: _ => { didSleep = true; dateTime = dateTime.AddMilliseconds(1); });

			var results = new List<Guid>();

			for (var x = 0; x < 20; x++) // Because probability
			{
				didSleep = false;
				results.Clear();

				for (var i = 0; i < 2000; i++)
					results.Add(generator.CreateId());

				if (!didSleep) break;
			}

			Assert.Equal(2000, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnIncrementalValues()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

			var results = new List<Guid>();
			for (var i = 0; i < 100; i++)
				results.Add(generator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnSameTimestampWithinRateLimit_ShouldReturnIdenticalTimestampComponents()
		{
			var didChangeTimestamp = false;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime, sleepAction: _ => { didChangeTimestamp = true; dateTime = dateTime.AddMilliseconds(1); });

			var results = new List<ulong>();

			int x;
			for (x = 0; x < 20; x++) // Because probability
			{
				didChangeTimestamp = false;
				dateTime = dateTime.AddMilliseconds(1);
				results.Clear();

				ulong? previousTimestampComponent = null;
				for (var i = 0; i < 2000; i++)
				{
					var id = generator.CreateId();
					var timestampComponent = ExtractTimestampComponent(id);

					if (previousTimestampComponent is not null && timestampComponent != previousTimestampComponent)
						didChangeTimestamp = true;
					previousTimestampComponent = timestampComponent;

					results.Add(ExtractTimestampComponent(id));
				}

				if (!didChangeTimestamp) break;
			}

			Assert.Equal(2000, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.Equal(results[i - 1], results[i]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnIncrementalTimestampComponents()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

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
			var didChangeTimestamp = false;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime = dateTime.AddTicks(1), sleepAction: _ => { didChangeTimestamp = true; dateTime = dateTime.AddMilliseconds(1); });

			var results = new List<(ulong, ulong)>();

			for (var x = 0; x < 20; x++) // Because probability
			{
				didChangeTimestamp = false;
				dateTime = dateTime.AddMilliseconds(1);
				results.Clear();

				ulong? previousTimestampComponent = null;
				for (var i = 0; i < 2000; i++)
				{
					var id = generator.CreateId();
					var timestampComponent = ExtractTimestampComponent(id);

					if (previousTimestampComponent is not null && timestampComponent != previousTimestampComponent)
						didChangeTimestamp = true;
					previousTimestampComponent = timestampComponent;

					results.Add(ExtractRandomSequenceComponent(id));
				}

				if (!didChangeTimestamp) break;
			}

			Assert.Equal(2000, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i].CompareTo(results[i - 1]) > 0, $"Results should have been incremental: {results[i - 1]}, {results[i]}");
		}

		/// <summary>
		/// On the same timestamp, within the rate limit, we produce the same random sequence as before, incremented by a smaller random sequence.
		/// </summary>
		[Fact]
		public void CreateId_SubsequentlyOnSameTimestampWithinRateLimit_ShouldReturnIncrementalRandomSequenceComponents()
		{
			var didChangeTimestamp = false;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime, sleepAction: _ => { didChangeTimestamp = true; dateTime = dateTime.AddMilliseconds(1); });

			var results = new List<(ulong, ulong)>();

			for (var x = 0; x < 20; x++) // Because probability
			{
				didChangeTimestamp = false;
				dateTime = dateTime.AddMilliseconds(1);
				results.Clear();

				ulong? previousTimestampComponent = null;
				for (var i = 0; i < 2000; i++)
				{
					var id = generator.CreateId();
					var timestampComponent = ExtractTimestampComponent(id);

					if (previousTimestampComponent is not null && timestampComponent != previousTimestampComponent)
						didChangeTimestamp = true;
					previousTimestampComponent = timestampComponent;

					results.Add(ExtractRandomSequenceComponent(id));
				}

				if (!didChangeTimestamp) break;
			}

			Assert.Equal(2000, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i].CompareTo(results[i - 1]) > 0, $"Results should have been incremental: {results[i - 1]}, {results[i]}");
		}

		/// <summary>
		/// On different timestamps, we produce random sequences that are simply random bytes, with no relationship whatsoever.
		/// </summary>
		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnRandomSequenceComponentsThatArePurelyRandom()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

			var results = new List<(ulong, ulong)>();
			for (var i = 0; i < 100; i++)
				results.Add(ExtractRandomSequenceComponent(generator.CreateId()));

			var equalCount = 0;
			var incrementalCount = 0;
			for (var i = 1; i < results.Count; i++)
			{
				if (results[i] == results[i - 1]) equalCount++;
				if (results[i].CompareTo(results[i - 1]) > 0) incrementalCount++;
			}

			Assert.Equal(0, equalCount);
			Assert.True(incrementalCount < 95); // On average, 50% will be incremental
		}

		[Fact]
		public void CreateId_SubsequentlyWithinRateLimit_ShouldNotSleep()
		{
			var didSleep = false;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime, sleepAction: _ => { didSleep = true; dateTime = dateTime.AddMilliseconds(1); });

			for (var x = 0; x < 20; x++) // Because probability
			{
				didSleep = false;

				for (var i = 0; i < 1_000_000; i++)
					this.DefaultIdGenerator.CreateId();

				if (!didSleep) break;
			}

			Assert.False(didSleep);
		}

		[Fact]
		public void CreateId_SubsequentlyOnSameTimestamp_ShouldReachExpectedRateLimit()
		{
			const int minimumRate = 750_000;
			const int maximumExpectedRate = 1_500_000;

			var sleepCount = 0;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedId128Generator(utcClock: () => dateTime, sleepAction: _ => { sleepCount++; dateTime = dateTime.AddMilliseconds(1); });

			for (var i = 0; i < 10_000_000; i++)
				generator.CreateId();

			var rate = 10_000_000 / sleepCount;

			Assert.True(rate >= minimumRate);
			Assert.True(rate <= maximumExpectedRate);
		}
	}
}
