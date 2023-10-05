using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Architect.Identities.Encodings;
using Binary128 =
#if NET7_0_OR_GREATER
	System.UInt128;
#else
	System.Decimal;
#endif

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A sequence of 75 bits of pseudorandom data.
	/// </para>
	/// <para>
	/// This type supports the creation of new values by adding bits of pseudorandom data to an existing value.
	/// As such, statistically, values may lean towards higher values, and entropy may not be the full 75 bits, depending on use.
	/// </para>
	/// <para>
	/// A newly created instance will have 75 bits of random data.
	/// By calling <see cref="TryAddRandomBits(RandomSequence75, out RandomSequence75)"/>, a value with most of the parameter value's bits added is created.
	/// </para>
	/// <para>
	/// The random data originates from a cryptographically-secure pseudorandom number generator (CSPRNG).
	/// </para>
	/// <para>
	/// Although technically an instance can be created using the default constructor,
	/// all public operations (e.g. cast or <see cref="TryAddRandomBits(RandomSequence75, out RandomSequence75)"/>) will throw for such an instance.
	/// </para>
	/// </summary>
	internal readonly struct RandomSequence75
	{
		internal const ulong MaxHighValue = UInt64.MaxValue >> (64 - 11);
		internal static readonly Binary128 MaxValue =
#if NET7_0_OR_GREATER
			new Binary128(upper: UInt64.MaxValue >> (128 - 75), lower: UInt64.MaxValue);
#else
			new decimal(lo: ~0, mid: ~0, hi: (int)MaxHighValue, isNegative: false, scale: 0);
#endif

		/// <summary>
		/// The number of bits added by the <see cref="TryAddRandomBits(RandomSequence75, out RandomSequence75)"/> operation.
		/// </summary>
		internal const int AdditionalBitCount = 58;
		/// <summary>
		/// A mask to keep the low <see cref="AdditionalBitCount"/> bits of a ulong, in order to add additional random bits to an existing value.
		/// </summary>
		private static readonly ulong AdditionalBitMask = UInt64.MaxValue >> (64 - AdditionalBitCount);

		/// <summary>
		/// Contains the value's high 11 bits as its LSB.
		/// </summary>
		private ulong High { get; }
		/// <summary>
		/// Contains the value's low 64 bits.
		/// </summary>
		private ulong Low { get; }

		private static ulong ThrowCreateOnlyThroughCreateMethodException() => throw new InvalidOperationException($"Create this only through {nameof(RandomSequence75)}.{nameof(Create)}.");

		/// <summary>
		/// Constructs a new randomized instance.
		/// </summary>
		/// <param name="_">A dummy parameter to distinguish this from the struct's mandatory default constructor.</param>
		private RandomSequence75(byte _)
		{
			Span<byte> bytes = stackalloc byte[16];
			RandomNumberGenerator.Fill(bytes);

			var ulongs = MemoryMarshal.Cast<byte, ulong>(bytes);
			var high = ulongs[0] >> (64 - 11); // 11 LSB populated
			var low = ulongs[1]; // All 64 bits populated

			// Avoid 0, which we use to protect against incorrectly created instances
			if ((high | low) == 0UL)
				low = 1UL;

			this.High = high;
			this.Low = low;

			System.Diagnostics.Debug.Assert(this.High != 0UL || this.Low != 0UL, "The data does not look randomized.");
			System.Diagnostics.Debug.Assert(this.High >> 11 == 0UL, "The high 53 bits should have been zero.");
		}

		/// <summary>
		/// Constructs a new instance that contains the given value.
		/// </summary>
		private RandomSequence75(ulong high, ulong low)
		{
			if ((high | low) == 0UL || high > MaxHighValue)
				throw new ArgumentException("The value must be a randomized, non-zero value with the high 53 bits set to zero.");

			this.High = high;
			this.Low = low;
		}

		/// <summary>
		/// Generates a new 75-bit pseudorandom value.
		/// </summary>
		public static RandomSequence75 Create()
		{
			return new RandomSequence75(_: default);
		}

		/// <summary>
		/// Simulates an instance with the given value.
		/// For testing purposes only.
		/// </summary>
		/// <param name="value">A value with the high 53 bits set to zero.</param>
		[Obsolete("For testing purposes only.")]
		internal static RandomSequence75 CreatedSimulated(Binary128 value)
		{
			var (high, low) = GetHighAndLow(value);
			return CreatedSimulated(high: high, low: low);
		}

		/// <summary>
		/// Simulates an instance with the given value.
		/// For testing purposes only.
		/// </summary>
		/// <param name="high">A value with the high 53 bits set to zero.</param>
		[Obsolete("For testing purposes only.")]
		internal static RandomSequence75 CreatedSimulated(ulong high, ulong low)
		{
			return new RandomSequence75(high: high, low: low);
		}

		private Binary128 GetValue()
		{
			if (this.High == 0UL && this.Low == 0UL)
				ThrowCreateOnlyThroughCreateMethodException();

#if NET7_0_OR_GREATER
			return new Binary128(upper: this.High, lower: this.Low);
#else
			return new Binary128(
				lo: (int)(this.Low & UInt32.MaxValue),
				mid: (int)(this.Low >> 32),
				hi: (int)this.High,
				isNegative: false,
				scale: 0);
#endif
		}

		private static (ulong, ulong) GetHighAndLow(Binary128 value)
		{
#if NET7_0_OR_GREATER
			var high = (ulong)(value >> 64);
			var low = (ulong)value;
			return (high, low);
#else
			var decimals = MemoryMarshal.CreateSpan(ref value, length: 1);
			var ints = MemoryMarshal.Cast<decimal, int>(decimals);
			var lo = (ulong)DecimalStructure.GetLo(ints);
			var mid = (ulong)DecimalStructure.GetMid(ints);
			var hi = (ulong)DecimalStructure.GetHi(ints);
			var low = (mid << 32) | lo;
			return (hi, low);
#endif
		}

		public ulong GetHigh12Bits()
		{
			if ((this.High | this.Low) == 0UL)
				ThrowCreateOnlyThroughCreateMethodException();

			var result = this.High << 1; // 11 bits
			result |= this.Low >> 63; // +1 bit
			return result;
		}

		public ulong GetLow63Bits()
		{
			if ((this.High | this.Low) == 0UL)
				ThrowCreateOnlyThroughCreateMethodException();

			var result = this.Low & (UInt64.MaxValue >> 1); // 63 bits
			return result;
		}

		/// <summary>
		/// <para>
		/// Returns true and outputs a new instance that contains the current one's value with random data from the given one added to it.
		/// If the result would overflow, this method returns false instead.
		/// </para>
		/// </summary>
		public bool TryAddRandomBits(RandomSequence75 additionalRandomSource, out RandomSequence75 result)
		{
			var value = this.GetValue();
			var randomIncrement = additionalRandomSource.Low;

			randomIncrement &= AdditionalBitMask;

			// Avoid incrementing by 0, which would introduce a collision
			if (randomIncrement == 0UL)
				randomIncrement = 1UL;

			if (randomIncrement > MaxValue - value) // Addition would overflow our intended maximum
			{
				result = this;
				return false;
			}

			unchecked // Cannot overflow here anyway
			{
				value += randomIncrement;
			}

			var (high, low) = GetHighAndLow(value);
			result = new RandomSequence75(high: high, low: low);
			return true;
		}
	}
}
