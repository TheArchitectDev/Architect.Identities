using System.Buffers.Binary;
using System.Globalization;
using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	public sealed class DistributedIdGeneratorTests
	{
		private static readonly int SafeRateLimitPerTimestamp = DistributedIdGenerator.AverageRateLimitPerTimestamp / 2;
		private static readonly int ExceedingRateLimitPerTimestamp = DistributedIdGenerator.AverageRateLimitPerTimestamp * 3 / 2;
		private static readonly DateTime FixedUtcDateTime = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc);
		private static readonly ulong FixedTimestamp = GetTimestamp(FixedUtcDateTime);
		private static readonly ulong EpochTimestamp = 0UL;
		private static readonly RandomSequence48 FixedRandomSequence48 = SimulateRandomSequenceWithValue(1UL << 40);
		private static readonly RandomSequence48 MaxRandomSequence48 = SimulateRandomSequenceWithValue(RandomSequence48.MaxValue);

		private static ulong GetTimestamp(DateTime utcDateTime) => (ulong)(utcDateTime - DateTime.UnixEpoch).TotalMilliseconds;

#pragma warning disable CS0618 // Type or member is obsolete -- Obsolete intended to protect against non-test usage
		private static RandomSequence48 SimulateRandomSequenceWithValue(ulong value) => RandomSequence48.CreatedSimulated(value);
#pragma warning restore CS0618 // Type or member is obsolete

		/// <summary>
		/// A generator with the default dependencies.
		/// </summary>
		private DistributedIdGenerator DefaultIdGenerator { get; } = new DistributedIdGenerator();

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

		/// <summary>
		/// This detail is so important that we have a unit test on the constant. :)
		/// </summary>
		[Fact]
		public void Class_Regularly_ShouldClaimExpectedAverageRateLimit()
		{
			Assert.Equal(128, DistributedIdGenerator.AverageRateLimitPerTimestamp);
		}

		[Fact]
		public void CreateCore_Regularly_ShouldUseEpochToCalculateMilliseconds()
		{
			var id = this.DefaultIdGenerator.CreateCore(EpochTimestamp, FixedRandomSequence48);

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

			this.DefaultIdGenerator.CreateCore(maxPermittedTimestamp, FixedRandomSequence48); // Should not throw
			Assert.Throws<InvalidOperationException>(() => this.DefaultIdGenerator.CreateCore(firstOverflowingTimestamp, randomSequence: default));
		}

		[Fact]
		public void CreateCore_Regularly_ShouldStoreTimestampMillisecondsInHigh6Bytes()
		{
			var id = this.DefaultIdGenerator.CreateCore(FixedTimestamp, FixedRandomSequence48);

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
		public void CreateId_WithExhaustedRandomSequenceButRoomToIncrementTimestamp_ShouldIncrementTimestamp()
		{
			var clockValues = new[]
			{
				/* First ID */ FixedUtcDateTime,
				/* Second ID */ FixedUtcDateTime, // Should increment timestamp
				/* Final assertion */ FixedUtcDateTime.AddDays(1),
			};
			var clockValueIndex = 0;
			DateTime GetClockValue() => clockValues[clockValueIndex++];

			var sumSleepMilliseconds = 0;

			var generator = new DistributedIdGenerator(utcClock: GetClockValue, sleepAction: milliseconds => sumSleepMilliseconds += milliseconds);

			var firstId = generator.CreateId();

			generator.PreviousRandomSequence = MaxRandomSequence48; // Ensure that no more random bits can be added

			var secondId = generator.CreateId();

			Assert.True(secondId > firstId); // Generator should increment as normal
			Assert.Equal(0, sumSleepMilliseconds); // Generated should not have slept: was able to simply increase the timestamp
			Assert.Equal(FixedUtcDateTime.AddDays(1), GetClockValue()); // Clock should have been queried the expected number of times
		}

		[Fact]
		public void CreateId_WithExhaustedRandomSequenceAndExhaustedTimestamp_ShouldSleepUntilClockHasAdvanced()
		{
			var clockValues = new[]
			{
				/* First ID */ FixedUtcDateTime,
				// From here on, add -999 milliseconds to exhaust the timestamp, so that we are forced to sleep, because we cannot increment AND stay in the past
				/* Second ID */ FixedUtcDateTime.AddMilliseconds(-999), // Not permitted to use FixedUtcDateTime-999ms, so sleep
				/* Second ID */ FixedUtcDateTime.AddMilliseconds(-999).AddMicroseconds(999), // Sleep again (microsecond increase only, but not a whole millisecond)
				/* Second ID */ FixedUtcDateTime.AddMilliseconds(-999).AddMilliseconds(1), // Success
				/* Final assertion */ FixedUtcDateTime.AddMilliseconds(2),
			};
			var clockValueIndex = 0;
			DateTime GetClockValue() => clockValues[clockValueIndex++];

			var sumSleepMilliseconds = 0;

			var generator = new DistributedIdGenerator(utcClock: GetClockValue, sleepAction: milliseconds => sumSleepMilliseconds += milliseconds);

			var firstId = generator.CreateId();

			generator.PreviousRandomSequence = MaxRandomSequence48; // Ensure that no more random bits can be added

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
				/* First ID */ FixedUtcDateTime, // Will use timestamp FixedUtcDateTime-1000ms
				/* Second ID */ FixedUtcDateTime.AddMilliseconds(-998), // Will use timestamp FixedUtcDateTime-999ms
				/* Third ID */ FixedUtcDateTime.AddMilliseconds(-998 - 1000), // Not permitted to use FixedUtcDateTime-998ms (now considered the future), so will sleep for 1000 ms (barely permitted)
				/* Third ID after sleep */ FixedUtcDateTime.AddMilliseconds(-998 - 1001), // Would have to sleep for 1001 (more than permitted), so will give up and reset to a smaller ID
				/* Final assertion */ FixedUtcDateTime.AddDays(-1),
			};
			var clockValueIndex = 0;
			DateTime GetClockValue() => clockValues[clockValueIndex++];

			var sumSleepMilliseconds = 0;

			var generator = new DistributedIdGenerator(utcClock: GetClockValue, sleepAction: milliseconds => sumSleepMilliseconds += milliseconds);

			var firstId = generator.CreateId();

			generator.PreviousRandomSequence = MaxRandomSequence48; // Ensure that no more random bits can be added

			var secondId = generator.CreateId();

			generator.PreviousRandomSequence = MaxRandomSequence48; // Ensure that no more random bits can be added

			var thirdId = generator.CreateId();

			Assert.True(secondId > firstId); // Generator should increment as normal
			Assert.True(thirdId < firstId); // Generator should have avoided sleeping for too long and used the smaller timestamp instead
			Assert.Equal(1, sumSleepMilliseconds); // Generated should have slept the expected number of times
			Assert.Equal(FixedUtcDateTime.AddDays(-1), GetClockValue()); // Clock should have been queried the expected number of times
		}

		[Fact]
		public void CreateId_InYear3000_ShouldSucceed()
		{
			var generator = new DistributedIdGenerator(utcClock: () => new DateTime(3000, 01, 01, 0, 0, 0, DateTimeKind.Utc));

			var id = generator.CreateId();

			Assert.True(id < DistributedIdGenerator.MaxValue);
		}

		[Fact]
		public void CreateId_InYear4000_ShouldOverflow()
		{
			var generator = new DistributedIdGenerator(utcClock: () => new DateTime(4000, 01, 01, 0, 0, 0, DateTimeKind.Utc));

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
			var generator = new DistributedIdGenerator();

			var results = new List<decimal>();
			for (var i = 0; i < 2 * ExceedingRateLimitPerTimestamp; i++)
				results.Add(generator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);

			var dateTime = FixedUtcDateTime;
			generator = new DistributedIdGenerator(utcClock: () => dateTime = dateTime.AddTicks(1), sleepAction: _ => dateTime = dateTime.AddMilliseconds(1));

			results.Clear();
			for (var i = 0; i < 1_000_000; i++)
				results.Add(generator.CreateId());

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnSameTimestampWithinRateLimit_ShouldReturnIncrementalValues()
		{
			var didSleep = false;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime, sleepAction: _ => { didSleep = true; dateTime = dateTime.AddMilliseconds(1); });

			var results = new List<decimal>();

			for (var x = 0; x < 20; x++) // Because probability
			{
				didSleep = false;
				results.Clear();

				for (var i = 0; i < SafeRateLimitPerTimestamp; i++)
					results.Add(generator.CreateId());

				if (!didSleep) break;
			}

			Assert.Equal(SafeRateLimitPerTimestamp, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnIncrementalValues()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

			var results = new List<decimal>();
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
			var generator = new DistributedIdGenerator(utcClock: () => dateTime, sleepAction: _ => { didChangeTimestamp = true; dateTime = dateTime.AddMilliseconds(1); });

			var results = new List<ulong>();

			int x;
			for (x = 0; x < 20; x++) // Because probability
			{
				didChangeTimestamp = false;
				dateTime = dateTime.AddMilliseconds(1);
				results.Clear();

				ulong? previousTimestampComponent = null;
				for (var i = 0; i < SafeRateLimitPerTimestamp; i++)
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

			Assert.Equal(SafeRateLimitPerTimestamp, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.Equal(results[i - 1], results[i]);
		}

		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnIncrementalTimestampComponents()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

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
			var generator = new DistributedIdGenerator(utcClock: () => dateTime = dateTime.AddTicks(1), sleepAction: _ => { didChangeTimestamp = true; dateTime = dateTime.AddMilliseconds(1); });

			var results = new List<ulong>();

			for (var x = 0; x < 20; x++) // Because probability
			{
				didChangeTimestamp = false;
				dateTime = dateTime.AddMilliseconds(1);
				results.Clear();

				ulong? previousTimestampComponent = null;
				for (var i = 0; i < SafeRateLimitPerTimestamp; i++)
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

			Assert.Equal(SafeRateLimitPerTimestamp, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1], $"Results should have been incremental: {results[i - 1]}, {results[i]}");
		}

		/// <summary>
		/// On the same timestamp, within the rate limit, we produce the same random sequence as before, incremented by a smaller random sequence.
		/// </summary>
		[Fact]
		public void CreateId_SubsequentlyOnSameTimestampWithinRateLimit_ShouldReturnIncrementalRandomSequenceComponents()
		{
			var didChangeTimestamp = false;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime, sleepAction: _ => { didChangeTimestamp = true; dateTime = dateTime.AddMilliseconds(1); });

			var results = new List<ulong>();
			
			for (var x = 0; x < 20; x++) // Because probability
			{
				didChangeTimestamp = false;
				dateTime = dateTime.AddMilliseconds(1);
				results.Clear();

				ulong? previousTimestampComponent = null;
				for (var i = 0; i < SafeRateLimitPerTimestamp; i++)
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

			Assert.Equal(SafeRateLimitPerTimestamp, results.Count);

			for (var i = 1; i < results.Count; i++)
				Assert.True(results[i] > results[i - 1], $"Results should have been incremental: {results[i - 1]}, {results[i]}");
		}

		/// <summary>
		/// On different timestamps, we produce random sequences that are simply 6 random bytes, with no relationship whatsoever.
		/// </summary>
		[Fact]
		public void CreateId_SubsequentlyOnDifferentTimestamps_ShouldReturnRandomSequenceComponentsThatArePurelyRandom()
		{
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime = dateTime.AddMilliseconds(1));

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
		public void CreateId_SubsequentlyWithinRateLimit_ShouldNotSleep()
		{
			var didSleep = false;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime, sleepAction: _ => { didSleep = true; dateTime = dateTime.AddMilliseconds(1); });

			for (var x = 0; x < 20; x++) // Because probability
			{
				didSleep = false;

				for (var i = 0; i < SafeRateLimitPerTimestamp; i++)
					this.DefaultIdGenerator.CreateId();

				if (!didSleep) break;
			}

			Assert.False(didSleep);
		}

		[Fact]
		public void CreateId_SubsequentlyOnSameTimestamp_ShouldReachExpectedRateLimit()
		{
			const int minimumRate = 100;
			const int maximumExpectedRate = 150;

			var sleepCount = 0;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime, sleepAction: _ => { sleepCount++; dateTime = dateTime.AddMilliseconds(1); });

			// Spend the burst capacity, to avoid skewing the result
			for (var i = 0; i < 128_000; i++)
				generator.CreateId();

			sleepCount = 0;

			for (var i = 0; i < 1_000_000; i++)
				generator.CreateId();

			var rate = 1_000_000 / sleepCount;

			Assert.True(rate > minimumRate);
			Assert.True(rate < maximumExpectedRate);
		}

		[Fact]
		public void CreateId_SubsequentlyWithinBurstCapacity_ShouldNotSleep()
		{
			var sleepCount = 0;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime, sleepAction: _ => { sleepCount++; dateTime = dateTime.AddMilliseconds(1); });

			// Should be able to burst-generate about 128 * 1000 IDs without sleeping
			for (var i = 0; i < 100_000; i++)
				generator.CreateId();

			Assert.Equal(0, sleepCount);
		}

		[Fact]
		public void CreateId_SubsequentlyToBurstCapacity_ShouldRecoverBurstCapacityAfterOneSecond()
		{
			var sleepCount = 0;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime, sleepAction: _ => { sleepCount++; dateTime = dateTime.AddMilliseconds(1); });

			// Should be able to burst-generate about 128 * 1000 IDs without sleeping
			for (var i = 0; i < 128_000; i++)
				generator.CreateId();

			sleepCount = 0;
			dateTime = dateTime.AddSeconds(1);

			// Should be able to burst-generate about 128 * 1000 IDs without sleeping
			for (var i = 0; i < 100_000; i++)
				generator.CreateId();

			Assert.Equal(0, sleepCount);
		}

		[Fact]
		public void CreateId_SubsequentlyToBurstCapacity_IsNotExpectedToRecoverBurstCapacityInHalfASecond()
		{
			var sleepCount = 0;
			var dateTime = FixedUtcDateTime;
			var generator = new DistributedIdGenerator(utcClock: () => dateTime, sleepAction: _ => { sleepCount++; dateTime = dateTime.AddMilliseconds(1); });

			// Should be able to burst-generate about 128 * 1000 IDs without sleeping
			for (var i = 0; i < 128_000; i++)
				generator.CreateId();

			sleepCount = 0;
			dateTime = dateTime.AddSeconds(0.5);

			for (var i = 0; i < 128_000; i++)
				generator.CreateId();

			// With burst capacity half restored, this should require about half a second's worth of sleep to generate a second's worth of IDs
			Assert.True(sleepCount > 400);
			Assert.True(sleepCount < 600);
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
