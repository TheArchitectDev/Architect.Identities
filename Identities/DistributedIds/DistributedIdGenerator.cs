using System;
using System.Buffers.Binary;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// Used to implement <see cref="DistributedId"/> in a testable way.
	/// </summary>
	internal sealed class DistributedIdGenerator : IDistributedIdGenerator
	{
		/// <summary>
		/// The maximum ID value that can be generated, and the maximum value to fit in 28 digits.
		/// </summary>
		internal const decimal MaxValue = 99999_99999_99999_99999_99999_999m;

		static DistributedIdGenerator()
		{
			if (!Environment.Is64BitOperatingSystem)
				throw new PlatformNotSupportedException($"{nameof(DistributedId)} is not supported on non-64-bit operating systems. It uses 64-bit instructions that must be atomic.");

			if (!BitConverter.IsLittleEndian)
				throw new PlatformNotSupportedException($"{nameof(DistributedId)} is not supported on big-endian architectures. The decimal-binary conversions have not been tested.");
		}

		private static DateTime GetUtcNow() => DateTime.UtcNow;

		/// <summary>
		/// On average, a single application instance can create this many IDs on a single timestamp.
		/// </summary>
		internal const ushort AverageRateLimitPerTimestamp = 1 << (48 - RandomSequence6.AdditionalBitCount); // 64 for 42 bits, 128 for 41 bits

		/// <summary>
		/// The custom epoch helps ensure 28-character IDs, avoiding 27-character ones.
		/// </summary>
		internal static readonly DateTime Epoch = new DateTime(1900, 01, 01, 00, 00, 00, DateTimeKind.Utc);

		/// <summary>
		/// Can be invoked to get the current UTC datetime.
		/// </summary>
		private Func<DateTime> Clock { get; }
		/// <summary>
		/// Can be invoked to cause the current thread sleep for the given number of milliseconds.
		/// </summary>
		private Action<int> SleepAction { get; }

		/// <summary>
		/// The previous UTC timestamp (in milliseconds since the epoch) on which an ID was created (or 0 initially).
		/// </summary>
		internal ulong PreviousCreationTimestamp { get; private set; }
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

				// If the clock has not advanced beyond the last used timestamp, then we must make an effort to continue where we left off
				if (timestamp <= this.PreviousCreationTimestamp)
				{
					// If we succeed in creating another, greater value for the previous timestamp, return that
					if (this.TryCreateIncrementalRandomSequence(this.PreviousRandomSequence, randomSequence, out var incrementedRandomSequence))
					{
						timestamp = this.PreviousCreationTimestamp;
						this.PreviousRandomSequence = incrementedRandomSequence;
						return (timestamp, incrementedRandomSequence);
					}

					// If we have room to advance the timestamp while staying in the past, do so
					if (this.PreviousCreationTimestamp - timestamp < 999)
					{
						timestamp = this.PreviousCreationTimestamp + 1;
					}
					// Otherwise, sleep until the clock has advanced
					else
					{
						timestamp = this.AwaitUpdatedClockValue(minimumTimestamp: this.PreviousCreationTimestamp - 998);

						// We should generally be able to advance 1 millisecond while just barely staying in the past now
						// However, it is possible that the clock was adjusted backwards too much, in which case we will simply reset to the returned timestamp
						if (this.PreviousCreationTimestamp - timestamp < 999)
							timestamp = this.PreviousCreationTimestamp + 1;
					}
				}

				// Update the previous timestamp
				this.PreviousCreationTimestamp = timestamp;
				this.PreviousRandomSequence = randomSequence;

				return (timestamp, randomSequence);
			}
		}

		/// <summary>
		/// Returns the UTC timestamp in milliseconds since the epoch, but 1000 milliseconds in the past.
		/// By returning a timestamp in the past, we can add up to 999 milliseconds while still staying in the past.
		/// This allows us to burst-generate more IDs without throttling, and without using future timestamps.
		/// </summary>
		private ulong GetTimestamp()
		{
			// The custom epoch ensures that the resulting ID is always 28 digits long (whereas the UnixEpoch could cause 27-digit IDs)
			var utcNow = this.Clock();
			var millisecondsSinceEpoch = (ulong)(utcNow - Epoch).TotalMilliseconds;

			// In order to allow 1000 milliseconds' worth of IDs to be generated without delays, we return timestamps 1 second in the past by default
			// This lets us add up to 1000 milliseconds without moving into the future
			return millisecondsSinceEpoch - 1000;
		}

		/// <summary>
		/// <para>
		/// Sleeps until the clock has reached at least the given <paramref name="minimumTimestamp"/> and then returns the timestamp.
		/// </para>
		/// <para>
		/// May cause the current thread to sleep.
		/// </para>
		/// <para>
		/// If the clock is adjusted backwards more than 100 milliseconds, this method will consider the clock reset and simply return the current timestamp.
		/// </para>
		/// </summary>
		internal ulong AwaitUpdatedClockValue(ulong minimumTimestamp)
		{
			ulong timestamp;
			do
			{
				this.SleepAction(1);
				timestamp = this.GetTimestamp();

				// If the clock was adjusted backwards more than 100 milliseconds, consider the clock to be completely reset
				if (timestamp <= minimumTimestamp - 100)
					return timestamp;
			} while (timestamp < minimumTimestamp);
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
		private static RandomSequence6 CreateRandomSequence()
		{
			return RandomSequence6.Create();
		}

		/// <summary>
		/// <para>
		/// Pure function.
		/// </para>
		/// <para>
		/// Creates a new 48-bit random sequence based on the given previous one and new one.
		/// Adds new randomness while maintaining the incremental property.
		/// </para>
		/// <para>
		/// Returns true on success or false on overflow.
		/// </para>
		/// </summary>
		private bool TryCreateIncrementalRandomSequence(RandomSequence6 previousRandomSequence, RandomSequence6 newRandomSequence, out RandomSequence6 incrementedRandomSequence)
		{
			return previousRandomSequence.TryAddRandomBits(newRandomSequence, out incrementedRandomSequence);
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
