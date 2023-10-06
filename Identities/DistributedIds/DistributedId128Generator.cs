using System;
using System.Buffers.Binary;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// Used to implement <see cref="DistributedId"/> in a testable way.
	/// </summary>
	internal sealed class DistributedId128Generator : IDistributedId128Generator
	{
		/// <summary>
		/// Bits 0000_0111.
		/// </summary>
		private const byte VersionMarkerByte = 0b_0000_0111;
		/// <summary>
		/// Bits 0111 starting at the 48th top bit.
		/// </summary>
		private const ulong VersionMarker = (ulong)VersionMarkerByte << (64 - 48 - 4); // Shift left to move from bit 60 to bit 48

#if NET7_0_OR_GREATER
		/// <summary>
		/// The maximum ID value to fit in 38 digits.
		/// </summary>
		internal static readonly UInt128 MaxValueToFitInDecimal38 = UInt128.Parse("99999999999999999999999999999999999999");
#endif

		static DistributedId128Generator()
		{
			if (!Environment.Is64BitOperatingSystem)
				throw new PlatformNotSupportedException($"{nameof(DistributedId)} is not supported on non-64-bit operating systems. It uses 64-bit instructions that must be atomic.");

			if (!BitConverter.IsLittleEndian)
				throw new PlatformNotSupportedException($"{nameof(DistributedId)} is not supported on big-endian architectures. The binary conversions have not been tested.");
		}

		private static DateTime GetUtcNow()
		{
			return DateTime.UtcNow;
		}

		/// <summary>
		/// <para>
		/// The custom epoch helps ensure 38-digit IDs, avoiding 37-digit ones.
		/// </para>
		/// <para>
		/// The IDs stay at 38 digits until the year 4000+, fitting in a DECIMAL(38).
		/// </para>
		/// <para>
		/// The IDs stay within 127 bits until the year 6000+, fitting in two signed longs that are positive. (The 64th bit is always 0.)
		/// </para>
		/// </summary>
		internal static readonly DateTime Epoch = new DateTime(1700, 01, 01, 00, 00, 00, DateTimeKind.Utc);

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
		internal RandomSequence75 PreviousRandomSequence { get; set; }

		/// <summary>
		/// A lock object used to govern access to the mutable properties.
		/// </summary>
		private readonly object _lockObject = new object();

		internal DistributedId128Generator(Func<DateTime>? utcClock = null, Action<int>? sleepAction = null)
		{
			this.Clock = utcClock ?? GetUtcNow;
			this.SleepAction = sleepAction ?? Thread.Sleep;
		}

#if NET7_0_OR_GREATER
		public UInt128 CreateId()
		{
			return this.CreateGuid().ToUInt128();
		}
#endif

		public Guid CreateGuid()
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
		private (ulong Timestamp, RandomSequence75 RandomSequence) CreateValues()
		{
			var randomSequence = CreateRandomSequence();

		Start:

			lock (this._lockObject)
			{
				var timestamp = this.GetCurrentTimestamp();

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

					// In the unlikely event that we cannot increase the random portion without overflowing, we must wait for the timestamp to increase
					// In the edge case where the clock was turned back by too much, sleeping would take too long, so simply fall through and lose our incremental property, using the new, smaller timestamp
					if (timestamp + 1000 > this.PreviousCreationTimestamp)
						goto SleepAndRestart;
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
			var utcNow = this.Clock();
			var millisecondsSinceEpoch = (ulong)(utcNow - Epoch).TotalMilliseconds;

			return millisecondsSinceEpoch;
		}

		/// <summary>
		/// <para>
		/// Pure function (although the random number generator may use locking internally).
		/// </para>
		/// <para>
		/// Returns a new 75-bit random sequence.
		/// </para>
		/// </summary>
		private static RandomSequence75 CreateRandomSequence()
		{
			return RandomSequence75.Create();
		}

		/// <summary>
		/// <para>
		/// Pure function.
		/// </para>
		/// <para>
		/// Creates a new 75-bit random sequence based on the given previous one and new one.
		/// Adds new randomness while maintaining the incremental property.
		/// </para>
		/// <para>
		/// Returns true on success or false on overflow.
		/// </para>
		/// </summary>
		private bool TryCreateIncrementalRandomSequence(RandomSequence75 previousRandomSequence, RandomSequence75 newRandomSequence, out RandomSequence75 incrementedRandomSequence)
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
		internal Guid CreateCore(ulong timestamp, RandomSequence75 randomSequence)
		{
			Span<byte> resultBytes = stackalloc byte[16];

			// Bits 0-47: Timestamp (48 bits, of which top 1 bit remains 0 until year 6000+, to fit in 2 signed longs)
			// Bits 48-51: Version marker (4 bits)
			// Bits 52-63: Randomness (12 bits)
			var leftHalf = timestamp << (64 - 48);
			leftHalf |= VersionMarker;
			leftHalf |= randomSequence.GetHigh12Bits();

			// Bit 64: 0 (1 bit variant indicator, pretending to be legacy Apollo variant (0), to fit in 2 signed longs)
			// Bits 65-127: Randomness (63 bits)
			var rightHalf = randomSequence.GetLow63Bits();

			BinaryPrimitives.WriteUInt64BigEndian(resultBytes, leftHalf);
			BinaryPrimitives.WriteUInt64BigEndian(resultBytes[8..], rightHalf);

			BinaryIdEncoder.TryDecodeGuid(resultBytes, out var result);

			System.Diagnostics.Debug.Assert(result != default, "A non-default value should have been generated.");
			System.Diagnostics.Debug.Assert((leftHalf | rightHalf) != 0UL, "A non-default value should have been generated.");
			System.Diagnostics.Debug.Assert(leftHalf >> (64 - 48) == timestamp, "The first component should have been the timestamp.");
			System.Diagnostics.Debug.Assert((leftHalf >> (64 - 48 - 4) & 0b_1111UL) == VersionMarkerByte, "The second component should have been the expected version marker.");
			System.Diagnostics.Debug.Assert(rightHalf >> 63 == 0UL, "The first variant indicator bit should have been zero.");

			return result;
		}
	}
}
