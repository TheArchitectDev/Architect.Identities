using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A sequence of 6 pseudorandom bytes, castable to a ulong with the high 2 bytes set to zero.
	/// </para>
	/// <para>
	/// The random data originates from a cryptographically-secure pseudorandom number generator (CSPRNG).
	/// </para>
	/// <para>
	/// Although technically an instance can be created using the default constructor, all public operations (e.g. cast or <see cref="Add4RandomBytes(RandomSequence6)"/>) will throw for such an instance.
	/// </para>
	/// </summary>
	internal readonly struct RandomSequence6
	{
		/// <summary>
		/// A pseudorandom value with the high 2 bytes set to zero.
		/// </summary>
		private ulong Value => this._value == 0UL
			? throw new InvalidOperationException($"Create this only through {nameof(RandomSequence6)}.{nameof(Create)}.")
			: this._value;
		private readonly ulong _value;

		/// <summary>
		/// Constructs a new randomized instance.
		/// </summary>
		/// <param name="_">A dummy parameter to distinguish this from the struct's required default constructor.</param>
		private RandomSequence6(byte _)
		{
			Span<byte> bytes = stackalloc byte[8];
			var low6Bytes = bytes[..6]; // Fill the low 6 bytes from little-endian perspective (i.e. the left 6)
			RandomNumberGenerator.Fill(low6Bytes);

			// Use little endian to ensure that the 2 bytes on the right are considered the most significant
			this._value = BinaryPrimitives.ReadUInt64LittleEndian(bytes);

			System.Diagnostics.Debug.Assert(this.Value != 0UL, "The data does not look randomized.");
			System.Diagnostics.Debug.Assert(bytes.EndsWith(new byte[2]), "The high 2 bytes should have been zero.");
			System.Diagnostics.Debug.Assert(this.Value >> (64 - 16) == 0UL, "The high 2 bytes should have been zero.");
		}

		/// <summary>
		/// Constructs a new instance that contains the given value.
		/// </summary>
		private RandomSequence6(ulong value)
		{
			if (value == 0UL || value >> (64 - 16) != 0UL)
				throw new ArgumentException("The value must be a randomized value with the high 2 bytes set to zero.");

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
		/// Returns a new instance that contains the current one's value with 4 bytes of random data from the given one added to it.
		/// On overflow, the result is wrapped around 6 bytes.
		/// </summary>
		public RandomSequence6 Add4RandomBytes(RandomSequence6 additionalRandomSource)
		{
			const ulong maxValueBeforeOverflow = UInt64.MaxValue >> 16;

			var value = this.Value;
			var randomIncrement = (uint)additionalRandomSource.Value; // Keep 4 of the 6 additional random bytes

			unchecked // Should not matter, but indicates that we prefer wraparound to exceptions
			{
				value += randomIncrement;
			}

			// If we overflow (extremely unlikely), we wrap around 6 bytes
			if (value > maxValueBeforeOverflow) value -= maxValueBeforeOverflow;

			System.Diagnostics.Debug.Assert(value <= maxValueBeforeOverflow, "Overflow compensation did not work. The high 2 input bytes should have been zero.");

			return new RandomSequence6(value);
		}

		/// <summary>
		/// Converts the struct to a ulong filled with pseudorandom data, except that the high 2 bytes are set to zero.
		/// </summary>
		public static implicit operator ulong(RandomSequence6 sequence) => sequence.Value;
	}
}
