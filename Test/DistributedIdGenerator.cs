using System;
using System.Buffers.Binary;
using System.Threading;
using Architect.Identities;

// ReSharper disable once CheckNamespace
namespace Test
{
	/// <summary>
	/// Used to implement <see cref="DistributedId"/> in a testable way.
	/// </summary>
	internal sealed class DistributedIdGenerator
	{
		/// <summary>
		/// The maximum ID value that can be generated, and the maximum value to fit in 28 digits.
		/// </summary>
		internal const decimal MaxValue = 99999_99999_99999_99999_99999_999m;

		static DistributedIdGenerator()
		{
			if (!Environment.Is64BitOperatingSystem)
				throw new NotSupportedException($"{nameof(DistributedId)} is not supported on non-64-bit operating systems. It uses 64-bit instructions that must be atomic.");

			if (!BitConverter.IsLittleEndian)
				throw new NotSupportedException($"{nameof(DistributedId)} is not supported on big-endian architectures. The decimal-binary conversions have not been tested.");
		}

		private static DateTime GetUtcNow() => DateTime.UtcNow;

		/// <summary>
		/// A single application instance will aim to create no more than this many IDs on a single timestamp.
		/// </summary>
		internal const uint RateLimitPerTimestamp = 225;

		private Func<DateTime> Clock { get; }
		/// <summary>
		/// Can be invoked to cause the current thread sleep for the given number of milliseconds.
		/// </summary>
		private Action<int> SleepAction { get; }

		/// <summary>
		/// The previous UTC timestamp (in milliseconds since the epoch) on which an ID was created (or 0 initially).
		/// </summary>
		private ulong PreviousCreationTimestamp { get; set; }
		/// <summary>
		/// The number of contiguous IDs created thus far on the <see cref="PreviousCreationTimestamp"/>.
		/// </summary>
		private uint CurrentTimestampCreationCount { get; set; }
		/// <summary>
		/// The random sequence used during the previous ID creation.
		/// </summary>
		private RandomSequence6 PreviousRandomSequence { get; set; }

		/// <summary>
		/// A lock object used to govern access to the mutable properties.
		/// </summary>
		private readonly object _lockObject = new object();

		internal DistributedIdGenerator(Func<DateTime>? utcClock = null, Action<int>? sleepAction = null)
		{
			this.Clock = utcClock ?? GetUtcNow;
			this.SleepAction = sleepAction ?? Thread.Sleep;
		}

		public decimal CreateId()
		{
			var (timestamp, randomSequence) = this.CreateValues();
			return this.CreateCore(timestamp, randomSequence);
		}

		/// <summary>
		/// <para>
		/// Locking.
		/// </para>
		/// <para>
		/// Creates the values required to create an ID.
		/// </para>
		/// </summary>
		private (ulong Timestamp, RandomSequence6 RandomSequence) CreateValues()
		{
			// The random number generator is likely to lock, so doing this outside of our own lock is likely to increase throughput
			var randomSequence = CreateRandomSequence();

			lock (this._lockObject)
			{
				var timestamp = this.GetTimestamp();

				// If the clock has not advanced since the previous invocation
				if (timestamp == this.PreviousCreationTimestamp)
				{
					// If we can create more contiguous values, advance the count and create the next value
					if (this.TryCreateIncrementalRandomSequence(this.PreviousRandomSequence, randomSequence, out var largerRandomSequence))
					{
						this.PreviousRandomSequence = largerRandomSequence;
						this.CurrentTimestampCreationCount++;
						return (timestamp, largerRandomSequence);
					}
					// Otherwise, sleep until the clock has advanced
					else
					{
						timestamp = this.AwaitUpdatedClockValue();
					}
				}

				// Update the previous timestamp and reset the counter
				this.PreviousCreationTimestamp = timestamp;
				this.CurrentTimestampCreationCount = 1U;
				this.PreviousRandomSequence = randomSequence;

				return (timestamp, randomSequence);
			}
		}

		/// <summary>
		/// Returns the UTC timestamp in milliseconds since the epoch.
		/// </summary>
		private ulong GetTimestamp()
		{
			var utcNow = this.Clock();
			var millisecondsSinceEpoch = (ulong)(utcNow - DateTime.UnixEpoch).TotalMilliseconds;
			return millisecondsSinceEpoch;
		}

		/// <summary>
		/// <para>
		/// Sleeps until the clock has changed onto another millisecond and then returns that timestamp.
		/// </para>
		/// <para>
		/// May cause the current thread to sleep.
		/// </para>
		/// <para>
		/// Intended for use inside lock. Reads object state, but does not mutate it.
		/// </para>
		/// </summary>
		internal ulong AwaitUpdatedClockValue()
		{
			ulong timestamp;
			do
			{
				this.SleepAction(1);
			} while ((timestamp = this.GetTimestamp()) == this.PreviousCreationTimestamp);
			return timestamp;
		}

		/// <summary>
		/// <para>
		/// Pure function (although the random number generator may use locking internally).
		/// </para>
		/// <para>
		/// Returns a new 48-bit (6-byte) random sequence.
		/// </para>
		/// </summary>
		private RandomSequence6 CreateRandomSequence()
		{
			return RandomSequence6.Create();
		}

		private bool TryCreateIncrementalRandomSequence(RandomSequence6 previousRandomSequence, RandomSequence6 newRandomSequence, out RandomSequence6 largerRandomSequence)
		{
			return previousRandomSequence.TryAddRandomBits(newRandomSequence, out largerRandomSequence);
		}

		/// <summary>
		/// <para>
		/// Pure function.
		/// </para>
		/// <para>
		/// Creates a new ID based on the given values.
		/// </para>
		/// </summary>
		/// <param name="timestamp">The UTC timestamp in milliseconds since the epoch.</param>
		/// <param name="randomSequence">A random sequence whose 2 low bytes are zeros. This is checked to ensure that the caller has understood what will be used.</param>
		internal decimal CreateCore(ulong timestamp, RandomSequence6 randomSequence)
		{
			// 93 bits fit into 28 decimals
			// 96 bits: [3 unused bits] [45 time bits] [48 random bits]

			Span<byte> bytes = stackalloc byte[2 + 12 + 2]; // Bits: 16 padding (to treat the left half as ulong) + 96 useful + 16 padding (to treat the right half as ulong)

			// Populate the left half with the timestamp
			{
				// The 64-bit timestamp's 19 high bits must be zero, leaving the low 45 bits to be used
				if (timestamp >> 45 != 0UL)
					throw new InvalidOperationException($"{nameof(DistributedId)} has run out of available time bits."); // Year 3084

				// Write the time component into the first 8 bytes (64 bits: 16 padding to write a ulong, 3 unused, 45 used)
				BinaryPrimitives.WriteUInt64BigEndian(bytes, timestamp);
			}

			bytes = bytes[2..]; // Disregard the left padding

			// Populate the right half with the random data
			{
				var randomSequenceWithHighPadding = (ulong)randomSequence;
				System.Diagnostics.Debug.Assert(randomSequenceWithHighPadding >> (64 - 16) == 0, "The high 2 bytes should have been zero.");
				var randomSequenceWithLowPadding = randomSequenceWithHighPadding << 16;
				System.Diagnostics.Debug.Assert((ushort)randomSequenceWithLowPadding == 0, "The low 2 bytes should have been zero.");

				BinaryPrimitives.WriteUInt64BigEndian(bytes[^8..], randomSequenceWithLowPadding);
			}

			bytes = bytes[..^2]; // Disregard the right padding

			var id = new decimal(
				lo: BinaryPrimitives.ReadInt32BigEndian(bytes[8..12]),
				mid: BinaryPrimitives.ReadInt32BigEndian(bytes[4..8]),
				hi: BinaryPrimitives.ReadInt32BigEndian(bytes[0..4]),
				isNegative: false,
				scale: 0);

			System.Diagnostics.Debug.Assert(id <= MaxValue, "Overflowed the expected decimal digits."); // 2^93 (93 bits) fits in 10^28 (28 decimal digits)

			return id;
		}
	}
}
