using System;
using System.Buffers.Binary;
using Architect.Identities.Encodings;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Encodes and decodes ID values to and from hexadecimal string representations, i.e. ignore-case [0-9A-Za-z]. The Encode methods output [0-9A-Z].
	/// </para>
	/// <para>
	/// All methods on this type output fixed-length values that have the same (ordinal) sort order as their respective input values.
	/// </para>
	/// </summary>
	public static class HexadecimalIdEncoder
	{
		/// <summary>
		/// <para>
		/// Outputs a 16-character hexadecimal UTF-8 representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 16 bytes, to write the hexadecimal representation to.</param>
		public static void Encode(long id, Span<byte> bytes)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			Encode((ulong)id, bytes);
		}

		/// <summary>
		/// <para>
		/// Returns a 16-character hexadecimal string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		public static string Encode(long id)
		{
			return String.Create(16, id, (charSpan, theId) =>
			{
				Span<byte> byteSpan = stackalloc byte[16];
				Encode(id, byteSpan);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)byteSpan[i];
			});
		}

		/// <summary>
		/// <para>
		/// Outputs a 16-character hexadecimal UTF-8 representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 16 bytes, to write the hexadecimal representation to.</param>
		public static void Encode(ulong id, Span<byte> bytes)
		{
			if (bytes.Length < 16) throw new IndexOutOfRangeException("At least 16 output bytes are required.");

			// Abuse the caller's output span as input space
			BinaryIdEncoder.Encode(id, bytes);

			HexadecimalEncoder.ToHexChars(bytes, bytes, inputByteCount: 8);

			System.Diagnostics.Debug.Assert(bytes[15] != 0, "The expected output space was not written to.");
		}

		/// <summary>
		/// <para>
		/// Returns a 16-character hexadecimal string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		public static string Encode(ulong id)
		{
			return String.Create(16, id, (charSpan, theId) =>
			{
				Span<byte> byteSpan = stackalloc byte[16];
				Encode(id, byteSpan);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)byteSpan[i];
			});
		}

		/// <summary>
		/// <para>
		/// Outputs a 26-character hexadecimal UTF-8 representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value or if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		/// <param name="bytes">At least 26 bytes, to write the hexadecimal representation to.</param>
		public static void Encode(decimal id, Span<byte> bytes)
		{
			if (bytes.Length < 26) throw new IndexOutOfRangeException("At least 26 output bytes are required.");

			// Abuse the caller's output span as input space
			BinaryIdEncoder.Encode(id, bytes);

			// Ignore the 3 leading zeros
			bytes[3..].CopyTo(bytes);

			HexadecimalEncoder.ToHexChars(bytes: bytes, chars: bytes, inputByteCount: 13);

			System.Diagnostics.Debug.Assert(bytes[25] != 0, "The expected output space was not written to.");
		}

		/// <summary>
		/// <para>
		/// Returns a 26-character hexadecimal string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value or if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		public static string Encode(decimal id)
		{
			return String.Create(26, id, (charSpan, theId) =>
			{
				Span<byte> bytes = stackalloc byte[26];
				Encode(id, bytes);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)bytes[i];
			});
		}

		/// <summary>
		/// <para>
		/// Outputs a 32-character hexadecimal UTF-8 representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		/// <param name="bytes">At least 32 bytes, to write the hexadecimal representation to.</param>
		public static void Encode(Guid id, Span<byte> bytes)
		{
			if (bytes.Length < 32) throw new IndexOutOfRangeException("At least 32 output bytes are required.");

			// Abuse the caller's output span as input space
			BinaryIdEncoder.Encode(id, bytes);

			HexadecimalEncoder.ToHexChars(bytes, bytes, inputByteCount: 16);

			System.Diagnostics.Debug.Assert(bytes[31] != 0, "The expected output space was not written to.");
		}

		/// <summary>
		/// <para>
		/// Returns a 32-character hexadecimal string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		public static string Encode(Guid id)
		{
			return String.Create(32, id, (charSpan, theId) =>
			{
				Span<byte> bytes = stackalloc byte[32];
				Encode(id, bytes);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)bytes[i];
			});
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 16 of which will be read if possible.</param>
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
		/// Outputs an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 16 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeLong(ReadOnlySpan<char> chars, out long id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(16, chars.Length)];

			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)chars[i];

			return TryDecodeLong(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 16 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeUlong(ReadOnlySpan<byte> bytes, out ulong id)
		{
			// Hexadecimal encodings are exactly 16 characters long
			if (bytes.Length < 16)
			{
				id = default;
				return false;
			}

			Span<byte> outputBytes = stackalloc byte[8];

			try
			{
				HexadecimalEncoder.FromHexChars(bytes, outputBytes, inputByteCount: 16);
			}
			catch (ArgumentException)
			{
				id = default;
				return false;
			}

			id = BinaryPrimitives.ReadUInt64BigEndian(outputBytes);
			return true;
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 16 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeUlong(ReadOnlySpan<char> chars, out ulong id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(16, chars.Length)];

			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)chars[i];

			return TryDecodeUlong(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 26 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeDecimal(ReadOnlySpan<byte> bytes, out decimal id)
		{
			// Hexadecimal encodings are exactly 26 characters long
			if (bytes.Length < 26)
			{
				id = default;
				return false;
			}

			Span<byte> outputBytes = stackalloc byte[16];

			System.Diagnostics.Debug.Assert(outputBytes[0] == 0, "The stackallocated memory was expected to be cleared.");
			System.Diagnostics.Debug.Assert(outputBytes[1] == 0, "The stackallocated memory was expected to be cleared.");
			System.Diagnostics.Debug.Assert(outputBytes[2] == 0, "The stackallocated memory was expected to be cleared.");

			try
			{
				HexadecimalEncoder.FromHexChars(bytes, outputBytes[3..], inputByteCount: 26);
			}
			catch (ArgumentException)
			{
				id = default;
				return false;
			}

			return BinaryIdEncoder.TryDecodeDecimal(outputBytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 26 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeDecimal(ReadOnlySpan<char> chars, out decimal id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(26, chars.Length)];

			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)chars[i];

			return TryDecodeDecimal(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 32 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeGuid(ReadOnlySpan<byte> bytes, out Guid id)
		{
			// Hexadecimal encodings are exactly 32 characters long
			if (bytes.Length < 32)
			{
				id = default;
				return false;
			}

			Span<byte> outputBytes = stackalloc byte[16];

			try
			{
				HexadecimalEncoder.FromHexChars(bytes, outputBytes, inputByteCount: 32);
			}
			catch (ArgumentException)
			{
				id = default;
				return false;
			}

			return BinaryIdEncoder.TryDecodeGuid(outputBytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 32 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeGuid(ReadOnlySpan<char> chars, out Guid id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(32, chars.Length)];

			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)chars[i];

			return TryDecodeGuid(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 16 of which will be read if possible.</param>
		public static long? DecodeLongOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeLong(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 16 of which will be read if possible.</param>
		public static long? DecodeLongOrDefault(ReadOnlySpan<char> bytes)
		{
			return TryDecodeLong(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 16 of which will be read if possible.</param>
		public static ulong? DecodeUlongOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeUlong(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 16 of which will be read if possible.</param>
		public static ulong? DecodeUlongOrDefault(ReadOnlySpan<char> bytes)
		{
			return TryDecodeUlong(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 26 of which will be read if possible.</param>
		public static decimal? DecodeDecimalOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeDecimal(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 26 of which will be read if possible.</param>
		public static decimal? DecodeDecimalOrDefault(ReadOnlySpan<char> bytes)
		{
			return TryDecodeDecimal(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 32 of which will be read if possible.</param>
		public static Guid? DecodeGuidOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeGuid(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given hexadecimal UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 32 of which will be read if possible.</param>
		public static Guid? DecodeGuidOrDefault(ReadOnlySpan<char> bytes)
		{
			return TryDecodeGuid(bytes, out var id) ? id : null;
		}
	}
}
