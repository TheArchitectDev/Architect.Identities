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
		internal RandomSequence6 PreviousRandomSequence { get; set; }

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
			var randomSequence = CreateRandomSequence();

			Start:

			lock (this._lockObject)
			{
				// We start from timestamps 1000 milliseconds in the past
				// This lets us burst-generate more IDs, by adding anywhere between 0 through 999 milliseconds without moving into the future
				const ulong leeway = 1000;
				var timestamp = this.GetCurrentTimestamp() - leeway;

				// If the clock has not advanced beyond the last used timestamp, then we must make an effort to continue where we left off
				if (timestamp <= this.PreviousCreationTimestamp)
				{
					// If we succeed in creating another, greater random value to use with the previous timestamp, return that
					if (this.TryCreateIncrementalRandomSequence(this.PreviousRandomSequence, randomSequence, out var incrementedRandomSequence))
					{
						timestamp = this.PreviousCreationTimestamp;
						this.PreviousRandomSequence = incrementedRandomSequence;
						return (timestamp, incrementedRandomSequence);
					}

					// We cannot increase the random portion without overflowing, so we must increase the timestamp somehow

					// We may generate for timestamps in the past, i.e. no greater than (now - 1000ms) + 999ms
					var maxPermissibleTimestamp = timestamp + leeway - 1;

					// If the previous timestamp can be incremented while staying in the past, do so
					if (this.PreviousCreationTimestamp < maxPermissibleTimestamp)
					{
						timestamp = this.PreviousCreationTimestamp + 1;
					}
					// Otherwise, sleep and restart
					// In the edge case where the clock was turned back by more than our leeway, sleeping would take too long, so simply fall through and lose our incremental property, using the new, smaller timestamp
					else if (this.PreviousCreationTimestamp - maxPermissibleTimestamp <= leeway)
					{
						goto SleepAndRestart;
					}
				}

				// Update the last used values
				this.PreviousCreationTimestamp = timestamp;
				this.PreviousRandomSequence = randomSequence;

				return (timestamp, randomSequence);
			}

			SleepAndRestart:

			this.SleepAction(1); // Ideally outside the lock
			goto Start;
		}

		/// <summary>
		/// Returns the UTC timestamp in milliseconds since some epoch.
		/// </summary>
		private ulong GetCurrentTimestamp()
		{
			// The custom epoch ensures that the resulting ID is always 28 digits long (whereas the UnixEpoch could cause 27-digit IDs)
			var utcNow = this.Clock();
			var millisecondsSinceEpoch = (ulong)(utcNow - Epoch).TotalMilliseconds;

			return millisecondsSinceEpoch;
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
