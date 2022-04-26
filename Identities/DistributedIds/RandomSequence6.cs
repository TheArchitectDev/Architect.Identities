using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A sequence of 6 bytes of pseudorandom data.
	/// This type is castable to a ulong with the high 16 bits set to zero.
	/// </para>
	/// <para>
	/// This type supports the creation of new values by adding bits of pseudorandom data to an existing value.
	/// As such, statistically, values may lean towards higher values, and entropy may not be the full 48 bits, depending on use.
	/// </para>
	/// <para>
	/// A newly created instance will have 48 bits of random data.
	/// By calling <see cref="TryAddRandomBits(RandomSequence6, out RandomSequence6)"/>, a value with most of the parameter value's bits added is created.
	/// </para>
	/// <para>
	/// The random data originates from a cryptographically-secure pseudorandom number generator (CSPRNG).
	/// </para>
	/// <para>
	/// Although technically an instance can be created using the default constructor,
	/// all public operations (e.g. cast or <see cref="TryAddRandomBits(RandomSequence6, out RandomSequence6)"/>) will throw for such an instance.
	/// </para>
	/// </summary>
	internal readonly struct RandomSequence6
	{
		private const ulong MaxValue = UInt64.MaxValue >> 16;
		/// <summary>
		/// The number of bits added by the <see cref="TryAddRandomBits(RandomSequence6, out RandomSequence6)"/> operation.
		/// </summary>
		internal const int AdditionalBitCount = 41;
		/// <summary>
		/// A mask to keep the low <see cref="AdditionalBitCount"/> bits of a ulong, in order to add additional random bits to an existing value.
		/// </summary>
		private const ulong AdditionalBitMask = UInt64.MaxValue >> (64 - AdditionalBitCount);

		/// <summary>
		/// A pseudorandom value with the most significant 2 bytes set to zero.
		/// </summary>
		private ulong Value => this._value == 0UL
			? ThrowCreateOnlyThroughCreateMethodException()
			: this._value;
		private readonly ulong _value;

		private static ulong ThrowCreateOnlyThroughCreateMethodException() => throw new InvalidOperationException($"Create this only through {nameof(RandomSequence6)}.{nameof(Create)}.");

		/// <summary>
		/// Constructs a new randomized instance.
		/// </summary>
		/// <param name="_">A dummy parameter to distinguish this from the struct's mandatory default constructor.</param>
		private RandomSequence6(byte _)
		{
			Span<byte> bytes = stackalloc byte[8];
			var low6Bytes = bytes[..6]; // Fill the low 6 bytes from little-endian perspective (i.e. the left 6)
			RandomNumberGenerator.Fill(low6Bytes);

			// Use little endian to ensure that the 2 zero bytes on the right are considered the most significant
			this._value = BinaryPrimitives.ReadUInt64LittleEndian(bytes);

			if (this._value == 0UL) this._value = 1UL; // Avoid 0, which we use to protect against incorrectly created instances

			System.Diagnostics.Debug.Assert(this.Value != 0UL, "The data does not look randomized.");
			System.Diagnostics.Debug.Assert(bytes.EndsWith(new byte[2]), "The high 2 bytes should have been zero.");
			System.Diagnostics.Debug.Assert(this.Value >> (64 - 16) == 0UL, "The high 16 bits should have been zero.");
		}

		/// <summary>
		/// Constructs a new instance that contains the given value.
		/// </summary>
		private RandomSequence6(ulong value)
		{
			if (value == 0UL || value >> (64 - 16) != 0UL)
				throw new ArgumentException("The value must be a randomized, non-zero value with the high 2 bytes set to zero.");

			this._value = value;
		}

		/// <summary>
		/// Generates a new 6-byte pseudorandom value.
		/// </summary>
		public static RandomSequence6 Create()
		{
			return new RandomSequence6(_: default);
		}

		/// <summary>
		/// Simulates an instance with the given value.
		/// For testing purposes only.
		/// </summary>
		/// <param name="value">A value with the high 2 bytes set to zero.</param>
		[Obsolete("For testing purposes only.")]
		internal static RandomSequence6 CreatedSimulated(ulong value)
		{
			return new RandomSequence6(value);
		}

		/// <summary>
		/// <para>
		/// Returns true and outputs a new instance that contains the current one's value with random data from the given one added to it.
		/// If the result would overflow, this method returns false instead.
		/// </para>
		/// </summary>
		public bool TryAddRandomBits(RandomSequence6 additionalRandomSource, out RandomSequence6 result)
		{
			var value = this.Value;
			var randomIncrement = additionalRandomSource.Value;

			randomIncrement &= AdditionalBitMask;

			if (randomIncrement == 0UL) randomIncrement = 1UL; // Avoid incrementing by 0, which would introduce a collision

			if (randomIncrement > MaxValue - value) // Addition would cause overflow
			{
				result = this;
				return false;
			}

			unchecked // Cannot overflow UInt64 here anyway
			{
				value += randomIncrement;
			}

			result = new RandomSequence6(value);
			return true;
		}

		/// <summary>
		/// Converts the struct to a ulong filled with pseudorandom data, except that the high 2 bytes are set to zero.
		/// </summary>
		public static implicit operator ulong(RandomSequence6 sequence) => sequence.Value;
	}
}
