using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Architect.Identities.Encodings;
using Architect.Identities.Helpers;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static partial class IdEncoder
	{
		/// <summary>
		/// Validates that the given ID is valid, and returns its components.
		/// </summary>
		private static (int SignAndScale, int Hi, int Mid, int Lo) ExtractAndValidateIdComponents(decimal id)
		{
			if (id < 0m) throw new ArgumentOutOfRangeException();

			// Extract the components
			var decimals = MemoryMarshal.CreateReadOnlySpan(ref id, length: 1);
			var components = MemoryMarshal.Cast<decimal, int>(decimals);
			var signAndScale = DecimalStructure.GetSignAndScale(components);
			var hi = DecimalStructure.GetHi(components);
			var lo = DecimalStructure.GetLo(components);
			var mid = DecimalStructure.GetMid(components);

			// Validate format and range
			if (id > DistributedIdGenerator.MaxValue || signAndScale != 0)
				throw new ArgumentException($"The ID must be positive, have no decimal places, and consist of no more than 28 digits.", nameof(id));

			return (signAndScale, hi, mid, lo);
		}

		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void GetAlphanumeric(long id, Span<byte> bytes)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			GetAlphanumeric((ulong)id, bytes);
		}

		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void GetAlphanumeric(ulong id, Span<byte> bytes)
		{
			if (bytes.Length < 11) throw new IndexOutOfRangeException("At least 11 output bytes are required.");

			// Abuse the caller's output span as input space
			BinaryPrimitives.WriteUInt64BigEndian(bytes, id);

			Base62.ToBase62Chars8(bytes, bytes);
		}

		/// <summary>
		/// <para>
		/// Outputs a 16-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value or if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the alphanumeric representation to.</param>
		public static void GetAlphanumeric(decimal id, Span<byte> bytes)
		{
			if (bytes.Length < 16) throw new IndexOutOfRangeException("At least 16 output bytes are required.");

			var (signAndScale, hi, mid, lo) = ExtractAndValidateIdComponents(id);

			System.Diagnostics.Debug.Assert(signAndScale == 0); // Double check
			Unsafe.WriteUnaligned(ref bytes[0], 0); // Endianness for 0 is irrelevant, so use unaligned for performance
			BinaryPrimitives.WriteInt32BigEndian(bytes[4..], hi);
			BinaryPrimitives.WriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.WriteInt32BigEndian(bytes[12..], lo);

			Span<byte> charBytes = stackalloc byte[22];

			Base62.ToBase62Chars16(bytes, charBytes);

			System.Diagnostics.Debug.Assert(charBytes[..6].TrimStart((byte)'0').Length == 0, "The first 6 characters should have each represented zero. Did the input range validation break?");

			// Copy the relevant output into the caller's output span
			charBytes[^16..].CopyTo(bytes);
		}

		/// <summary>
		/// <para>
		/// Outputs a 22-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		/// <param name="bytes">At least 22 bytes, to write the alphanumeric representation to.</param>
		public static void GetAlphanumeric(Guid id, Span<byte> bytes)
		{
			if (bytes.Length < 22) throw new IndexOutOfRangeException("At least 22 output bytes are required.");

			var guids = MemoryMarshal.CreateSpan(ref id, length: 1);
			var uints = MemoryMarshal.Cast<Guid, uint>(guids);
			var ushorts = MemoryMarshal.Cast<Guid, ushort>(guids);

			// We need to order the GUID's bytes left-to-right from most significant to least significant
			uints[0] = BinaryPrimitives.ReverseEndianness(uints[0]); // Bytes 0-3 are the most significant, but are still litte-endian
			ushorts[2] = BinaryPrimitives.ReverseEndianness(ushorts[2]); // Bytes 4-5 are the next most significant, but are still little-endian
			ushorts[3] = BinaryPrimitives.ReverseEndianness(ushorts[3]); // Bytes 6-7 are the next most significant, but are still little-endian

			// Bytes 8-15 are the next most significant, and are already in big-endian

			var inputBytes = MemoryMarshal.AsBytes(guids);
			Base62.ToBase62Chars16(inputBytes, bytes);
		}
	}
}
