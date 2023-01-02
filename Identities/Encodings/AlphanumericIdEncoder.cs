using System;
using System.Buffers.Binary;
using Architect.Identities.Encodings;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Encodes and decodes ID values to and from alphanumeric string representations, i.e. case-sensitive [0-9A-Za-z].
	/// </para>
	/// <para>
	/// All methods on this type output fixed-length values that have the same (ordinal) sort order as their respective input values.
	/// </para>
	/// </summary>
	public static class AlphanumericIdEncoder
	{
		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric UTF-8 representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void Encode(long id, Span<byte> bytes)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			Encode((ulong)id, bytes);
		}

		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		public static string Encode(long id)
		{
			return String.Create(11, id, (charSpan, theId) =>
			{
				Span<byte> bytes = stackalloc byte[11];
				Encode(id, bytes);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)bytes[i];
			});
		}

		/// <summary>
		/// <para>
		/// Outputs an 11-character alphanumeric UTF-8 representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		/// <param name="bytes">At least 11 bytes, to write the alphanumeric representation to.</param>
		public static void Encode(ulong id, Span<byte> bytes)
		{
			if (bytes.Length < 11) throw new IndexOutOfRangeException("At least 11 output bytes are required.");

			// Abuse the caller's output span as input space
			BinaryIdEncoder.Encode(id, bytes);

			Base62Encoder.ToBase62Chars8(bytes, bytes);
		}

		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
		public static string Encode(ulong id)
		{
			return String.Create(11, id, (charSpan, theId) =>
			{
				Span<byte> bytes = stackalloc byte[11];
				Encode(id, bytes);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)bytes[i];
			});
		}

		/// <summary>
		/// <para>
		/// Outputs a 16-character alphanumeric UTF-8 representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value or if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the alphanumeric representation to.</param>
		public static void Encode(decimal id, Span<byte> bytes)
		{
			// Abuse the caller's output span as input space
			BinaryIdEncoder.Encode(id, bytes);

			// Use temporary space for easier base62 encoding
			Span<byte> charBytes = stackalloc byte[22];

			Base62Encoder.ToBase62Chars16(bytes, charBytes);

			System.Diagnostics.Debug.Assert(charBytes[..6].TrimStart((byte)'0').Length == 0, "The first 6 characters should have each represented zero. Did the input range validation break?");

			// Copy the relevant output into the caller's output span
			charBytes[^16..].CopyTo(bytes);
		}

		/// <summary>
		/// <para>
		/// Returns a 16-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
		public static string Encode(decimal id)
		{
			return String.Create(16, id, (charSpan, theId) =>
			{
				Span<byte> bytes = stackalloc byte[16];
				Encode(id, bytes);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)bytes[i];
			});
		}

		/// <summary>
		/// <para>
		/// Outputs a 22-character alphanumeric UTF-8 representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		/// <param name="bytes">At least 22 bytes, to write the alphanumeric representation to.</param>
		public static void Encode(Guid id, Span<byte> bytes)
		{
			if (bytes.Length < 22) throw new IndexOutOfRangeException("At least 22 output bytes are required.");

			Span<byte> inputBytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, inputBytes);

			Base62Encoder.ToBase62Chars16(inputBytes, bytes);
		}

		/// <summary>
		/// <para>
		/// Returns a 22-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		public static string Encode(Guid id)
		{
			return String.Create(22, id, (charSpan, theId) =>
			{
				Span<byte> bytes = stackalloc byte[22];
				Encode(id, bytes);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)bytes[i];
			});
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 11 of which will be read if possible.</param>
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
		/// Outputs an ID decoded from the given alphanumeric string representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 11 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeLong(ReadOnlySpan<char> chars, out long id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(11 + 1, chars.Length)]; // +1 space to detect oversized inputs

			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)chars[i];

			return TryDecodeLong(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 11 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeUlong(ReadOnlySpan<byte> bytes, out ulong id)
		{
			// Alphanumeric encodings are exactly 11 characters long
			if (bytes.Length != 11)
			{
				id = default;
				return false;
			}

			Span<byte> outputBytes = stackalloc byte[8];

			try
			{
				Base62Encoder.FromBase62Chars11(bytes, outputBytes);
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
		/// Outputs an ID decoded from the given alphanumeric string representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 11 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeUlong(ReadOnlySpan<char> chars, out ulong id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(11 + 1, chars.Length)]; // +1 space to detect oversized inputs

			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)chars[i];

			return TryDecodeUlong(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 16 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeDecimal(ReadOnlySpan<byte> bytes, out decimal id)
		{
			// Alphanumeric encodings are exactly 16 characters long
			if (bytes.Length != 16)
			{
				id = default;
				return false;
			}

			Span<byte> paddedInputBytes = stackalloc byte[22];
			paddedInputBytes[..^16].Fill((byte)'0'); // Fill with leading '0' characters
			bytes[..16].CopyTo(paddedInputBytes[^16..]);

			Span<byte> outputBytes = stackalloc byte[16];

			try
			{
				Base62Encoder.FromBase62Chars22(paddedInputBytes, outputBytes);
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
		/// Outputs an ID decoded from the given alphanumeric string representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 16 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeDecimal(ReadOnlySpan<char> chars, out decimal id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(16 + 1, chars.Length)]; // +1 space to detect oversized inputs

			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)chars[i];

			return TryDecodeDecimal(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 22 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeGuid(ReadOnlySpan<byte> bytes, out Guid id)
		{
			// Alphanumeric encodings are exactly 22 characters long
			if (bytes.Length != 22)
			{
				id = default;
				return false;
			}

			Span<byte> outputBytes = stackalloc byte[16];

			try
			{
				Base62Encoder.FromBase62Chars22(bytes, outputBytes);
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
		/// Outputs an ID decoded from the given alphanumeric string representation.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 22 of which will be read if possible.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryDecodeGuid(ReadOnlySpan<char> chars, out Guid id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(22 + 1, chars.Length)]; // +1 space to detect oversized inputs

			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)chars[i];

			return TryDecodeGuid(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 11 of which will be read if possible.</param>
		public static long? DecodeLongOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeLong(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric string representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 11 of which will be read if possible.</param>
		public static long? DecodeLongOrDefault(ReadOnlySpan<char> chars)
		{
			return TryDecodeLong(chars, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 11 of which will be read if possible.</param>
		public static ulong? DecodeUlongOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeUlong(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric string representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 11 of which will be read if possible.</param>
		public static ulong? DecodeUlongOrDefault(ReadOnlySpan<char> chars)
		{
			return TryDecodeUlong(chars, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 16 of which will be read if possible.</param>
		public static decimal? DecodeDecimalOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeDecimal(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 16 of which will be read if possible.</param>
		public static decimal? DecodeDecimalOrDefault(ReadOnlySpan<char> chars)
		{
			return TryDecodeDecimal(chars, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="bytes">A sequence of input characters, the first 22 of which will be read if possible.</param>
		public static Guid? DecodeGuidOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryDecodeGuid(bytes, out var id) ? id : null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric UTF-8 representation.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of input characters, the first 22 of which will be read if possible.</param>
		public static Guid? DecodeGuidOrDefault(ReadOnlySpan<char> chars)
		{
			return TryDecodeGuid(chars, out var id) ? id : null;
		}
	}
}
