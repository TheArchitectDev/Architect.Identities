using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Architect.Identities.Encodings;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Encodes and decodes ID values to and from binary representations.
	/// </para>
	/// <para>
	/// All methods on this type output fixed-length values that have the same (ordinal) sort order as their respective input values.
	/// </para>
	/// </summary>
	public static class BinaryIdEncoder
	{
		static BinaryIdEncoder()
		{
			if (!BitConverter.IsLittleEndian)
				throw new PlatformNotSupportedException($"{nameof(BinaryIdEncoder)} is not supported on big-endian architectures. The conversions have not been tested.");
		}

		/// <summary>
		/// Validates that the given ID is valid, and returns its components.
		/// </summary>
		private static (int SignAndScale, int Hi, int Mid, int Lo) ExtractAndValidateIdComponents(decimal id)
		{
			if (id < 0m) throw new ArgumentOutOfRangeException(nameof(id));

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
		/// Outputs the 8-byte big-endian binary representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 8 bytes, to write the big-endian binary representation to.</param>
		public static void Encode(long id, Span<byte> bytes)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			Encode((ulong)id, bytes);
		}

		/// <summary>
		/// <para>
		/// Returns the 8-byte big-endian binary representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		public static byte[] Encode(long id)
		{
			var bytes = new byte[8];
			Encode(id, bytes);
			return bytes;
		}

		/// <summary>
		/// <para>
		/// Outputs the 8-byte big-endian binary representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 8 bytes, to write the big-endian binary representation to.</param>
		public static void Encode(ulong id, Span<byte> bytes)
		{
			if (bytes.Length < 8) throw new IndexOutOfRangeException("At least 8 output bytes are required.");

			BinaryPrimitives.WriteUInt64BigEndian(bytes, id);
		}

		/// <summary>
		/// <para>
		/// Returns the 8-byte big-endian binary representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		public static byte[] Encode(ulong id)
		{
			var bytes = new byte[8];
			Encode(id, bytes);
			return bytes;
		}

		/// <summary>
		/// <para>
		/// Outputs the 16-byte big-endian binary representation of the given ID. The first 3 bytes are always zero.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value or if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the big-endian binary representation to.</param>
		public static void Encode(decimal id, Span<byte> bytes)
		{
			if (bytes.Length < 16) throw new IndexOutOfRangeException("At least 16 output bytes are required.");

			var (signAndScale, hi, mid, lo) = ExtractAndValidateIdComponents(id);

			System.Diagnostics.Debug.Assert(signAndScale == 0); // Double check
			Unsafe.WriteUnaligned(ref bytes[0], 0); // Endianness for 0 is irrelevant, so use unaligned for performance
			BinaryPrimitives.WriteInt32BigEndian(bytes[4..], hi);
			BinaryPrimitives.WriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.WriteInt32BigEndian(bytes[12..], lo);

			System.Diagnostics.Debug.Assert(bytes[0] == 0, "The first 3 bytes should have been zero.");
			System.Diagnostics.Debug.Assert(bytes[1] == 0, "The first 3 bytes should have been zero.");
			System.Diagnostics.Debug.Assert(bytes[2] == 0, "The first 3 bytes should have been zero.");
		}

		/// <summary>
		/// <para>
		/// Returns the 16-byte big-endian binary representation of the given ID. The first 3 bytes are always zero.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		public static byte[] Encode(decimal id)
		{
			var bytes = new byte[16];
			Encode(id, bytes);
			return bytes;
		}

		/// <summary>
		/// <para>
		/// Outputs the 16-byte big-endian binary representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the big-endian binary representation to.</param>
		public static void Encode(Guid id, Span<byte> bytes)
		{
			if (bytes.Length < 16) throw new IndexOutOfRangeException("At least 16 output bytes are required.");

			var guids = MemoryMarshal.Cast<byte, Guid>(bytes);
			guids[0] = id;

			var uints = MemoryMarshal.Cast<Guid, uint>(guids);
			var ushorts = MemoryMarshal.Cast<Guid, ushort>(guids);

			// We need to order the GUID's bytes left-to-right from most significant to least significant
			uints[0] = BinaryPrimitives.ReverseEndianness(uints[0]); // Bytes 0-3 are the most significant, but are still litte-endian
			ushorts[2] = BinaryPrimitives.ReverseEndianness(ushorts[2]); // Bytes 4-5 are the next most significant, but are still little-endian
			ushorts[3] = BinaryPrimitives.ReverseEndianness(ushorts[3]); // Bytes 6-7 are the next most significant, but are still little-endian

			// Bytes 8-15 are the next most significant, and are already in big-endian
		}

		/// <summary>
		/// <para>
		/// Returns the 16-byte big-endian binary representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		public static byte[] Encode(Guid id)
		{
			var bytes = new byte[16];
			Encode(id, bytes);
			return bytes;
		}

#if NET7_0_OR_GREATER
		/// <summary>
		/// <para>
		/// Outputs the 16-byte big-endian binary representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="UInt128"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the big-endian binary representation to.</param>
		public static void Encode(UInt128 id, Span<byte> bytes)
		{
			if (bytes.Length < 16) throw new IndexOutOfRangeException("At least 16 output bytes are required.");

			BinaryPrimitives.WriteUInt64BigEndian(bytes, (ulong)(id >> 64));
			BinaryPrimitives.WriteUInt64BigEndian(bytes[8..], (ulong)id);
		}

		/// <summary>
		/// <para>
		/// Returns the 16-byte big-endian binary representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="UInt128"/>.</param>
		public static byte[] Encode(UInt128 id)
		{
			var bytes = new byte[16];
			Encode(id, bytes);
			return bytes;
		}
#endif

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 8 input bytes, in big-endian order.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeLong(ReadOnlySpan<byte> bytes, out long id)
		{
			if (!TryDecodeUlong(bytes, out var ulongId) || ulongId > Int64.MaxValue)
			{
				id = default;
				return false;
			}

			id = (long)ulongId;
			return true;
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 8 input bytes, in big-endian order.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeUlong(ReadOnlySpan<byte> bytes, out ulong id)
		{
			// Binary encodings are exactly 8 bytes long
			if (bytes.Length != 8)
			{
				id = default;
				return false;
			}

			id = BinaryPrimitives.ReadUInt64BigEndian(bytes);
			return true;
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 16 input bytes, in big-endian order.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeDecimal(ReadOnlySpan<byte> bytes, out decimal id)
		{
			// Binary encodings are exactly 16 bytes long
			if (bytes.Length != 16)
			{
				id = default;
				return false;
			}

			var signAndScale = BinaryPrimitives.ReadInt32BigEndian(bytes);
			var hi = BinaryPrimitives.ReadInt32BigEndian(bytes[4..]);
			var mid = BinaryPrimitives.ReadInt32BigEndian(bytes[8..]);
			var lo = BinaryPrimitives.ReadInt32BigEndian(bytes[12..]);

			id = new decimal(lo: lo, mid: mid, hi: hi, isNegative: false, scale: 0);

			if (signAndScale != 0 || id > DistributedIdGenerator.MaxValue)
			{
				id = default;
				return false;
			}

			return true;
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 16 input bytes, in big-endian order.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeGuid(ReadOnlySpan<byte> bytes, out Guid id)
		{
			// Binary encodings are exactly 16 bytes long
			if (bytes.Length != 16)
			{
				id = default;
				return false;
			}

			Span<Guid> guids = stackalloc Guid[1];
			guids[0] = MemoryMarshal.Read<Guid>(bytes);

			var uints = MemoryMarshal.Cast<Guid, uint>(guids);
			var ushorts = MemoryMarshal.Cast<Guid, ushort>(guids);

			// Our entire input was big-endian
			// Correct the left half of the GUID: make little-endian, with byte group significance (most to least) 0-3, 4-5, 6-7
			uints[0] = BinaryPrimitives.ReverseEndianness(uints[0]);
			ushorts[2] = BinaryPrimitives.ReverseEndianness(ushorts[2]);
			ushorts[3] = BinaryPrimitives.ReverseEndianness(ushorts[3]);

			id = guids[0];
			return true;
		}

#if NET7_0_OR_GREATER
		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 16 input bytes, in big-endian order.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeUInt128(ReadOnlySpan<byte> bytes, out UInt128 id)
		{
			// Binary encodings are exactly 16 bytes long
			if (bytes.Length != 16)
			{
				id = default;
				return false;
			}

			var upper = BinaryPrimitives.ReadUInt64BigEndian(bytes);
			var lower = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);

			id = new UInt128(upper: upper, lower: lower);
			return true;
		}
#endif

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 8 input bytes, in big-endian order.</param>
		public static long? DecodeLongOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeLong(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 8 input bytes, in big-endian order.</param>
		public static ulong? DecodeUlongOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeUlong(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 16 input bytes, in big-endian order.</param>
		public static decimal? DecodeDecimalOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeDecimal(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 16 input bytes, in big-endian order.</param>
		public static Guid? DecodeGuidOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeGuid(bytes, out var id) ? id : null;
		}

#if NET7_0_OR_GREATER
		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given binary representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of at least 16 input bytes, in big-endian order.</param>
		public static UInt128? DecodeUInt128OrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeUInt128(bytes, out var id) ? id : null;
		}
#endif
	}
}
