using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A sequence of 6 pseudorandom bytes, represented as a ulong with the 2 least significant bytes set to 0.
	/// </para>
	/// <para>
	/// The random data originates from a cryptographically-secure pseudorandom number generator (CSPRNG).
	/// </para>
	/// <para>
	/// Although technically an instance can be created using the default constructor, the only available operations (i.e. casts) will throw for such an instance.
	/// </para>
	/// </summary>
	internal readonly struct RandomSequence6
	{
		private ulong Value { get; }
		private uint UintValue { get; }

		private RandomSequence6(byte _)
		{
			var value = 0UL;
			var ulongs = MemoryMarshal.CreateSpan(ref value, length: 1);
			var bytes = MemoryMarshal.AsBytes(ulongs);
			var first6Bytes = bytes[..6];
			RandomNumberGenerator.Fill(first6Bytes);

			// Use big endian to ensure that the 0 bytes on the right are considered the least significant
			this.Value = BinaryPrimitives.ReadUInt64BigEndian(bytes);
			this.UintValue = MemoryMarshal.Read<uint>(first6Bytes);

			System.Diagnostics.Debug.Assert(this.Value != 0UL);
			System.Diagnostics.Debug.Assert(this.Value << 48 >> 48 == 0UL);
		}

		/// <summary>
		/// Generates a new 6-byte pseudorandom value.
		/// </summary>
		public static RandomSequence6 Create()
		{
			return new RandomSequence6(_: default);
		}

		/// <summary>
		/// Converts the struct to a ulong, with the 2 least significant bytes set to 0.
		/// </summary>
		public static implicit operator ulong(RandomSequence6 sequence) => sequence.Value == 0UL
			? throw new InvalidOperationException($"Create this only through {nameof(RandomSequence6)}.{nameof(Create)}.")
			: sequence.Value;

		/// <summary>
		/// Converts the struct to a ulong, with the 2 least significant bytes set to 0.
		/// </summary>
		public static implicit operator uint(RandomSequence6 sequence) => sequence.Value == 0UL
			? throw new InvalidOperationException($"Create this only through {nameof(RandomSequence6)}.{nameof(Create)}.")
			: sequence.UintValue;
	}
}
