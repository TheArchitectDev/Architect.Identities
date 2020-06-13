using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using Architect.Identities.Helpers;
using Architect.Identities.PublicIdentities.Encodings;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// Encodes and decodes <see cref="CompanyUniqueId"/> values.
	/// </summary>
	internal static class CompanyUniqueIdEncoder
	{
		internal const decimal MaxDecimalValue = 99999_99999_99999_99999_99999_999m; // 28 digits

		static CompanyUniqueIdEncoder()
		{
			// Ensure that decimals are still structured the same way
			// This prevents the application from generating incorrect string representations in this extremely unlikely scenario, allowing a fix to be created
			DecimalStructure.ThrowIfDecimalStructureIsUnexpected();
		}

		public static void ToShortString(decimal id, Span<byte> bytes)
		{
			if (bytes.Length < 16) throw new IndexOutOfRangeException("At least 16 output bytes are required.");
			bytes = bytes[..16];

			if (id < 0m) throw new ArgumentOutOfRangeException();

			// Extract the components (yes, decimal composition is weird)
			var decimals = MemoryMarshal.CreateReadOnlySpan(ref id, length: 1);
			var components = MemoryMarshal.Cast<decimal, int>(decimals);
			var signAndScale = DecimalStructure.GetSignAndScale(components);
			var hi = DecimalStructure.GetHi(components);
			var lo = DecimalStructure.GetLo(components);
			var mid = DecimalStructure.GetMid(components);

			// Validate format and range
			if (id > MaxDecimalValue || signAndScale != 0m)
				throw new ArgumentException($"Unexpected input value. Pass only values created by {nameof(CompanyUniqueId)}.{nameof(CompanyUniqueId.CreateId)}.", nameof(id));

			// Abuse the caller's output span as input space
			BinaryPrimitives.WriteInt32BigEndian(bytes, 0);
			BinaryPrimitives.WriteInt32BigEndian(bytes[4..], hi);
			BinaryPrimitives.WriteInt32BigEndian(bytes[8..], mid);
			BinaryPrimitives.WriteInt32BigEndian(bytes[12..], lo);

			Span<byte> charBytes = stackalloc byte[22];

			Base62.ToBase62Chars(bytes, charBytes);

			System.Diagnostics.Debug.Assert(charBytes[..6].TrimStart((byte)'0').Length == 0, "The first 6 characters should have each represented zero. Did the input range validation break?");

			// Copy the relevant output into the caller's output span
			charBytes[^16..].CopyTo(bytes);
		}

		public static string ToShortString(decimal id)
		{
			Span<byte> charBytes = stackalloc byte[16];

			ToShortString(id, charBytes);

			// Convert the ASCII bytes to chars
			Span<char> chars = stackalloc char[16];
			for (var i = 0; i < 16; i++) chars[i] = (char)charBytes[i];

			var result = new string(chars);
			return result;
		}

		public static bool TryFromShortString(ReadOnlySpan<byte> bytes, out decimal id)
		{
			if (bytes.Length < 16)
			{
				id = default;
				return false;
			}

			Span<byte> paddedInputBytes = stackalloc byte[22];
			paddedInputBytes[..^16].Fill((byte)'0'); // Fill with '0' characters
			bytes[..16].CopyTo(paddedInputBytes[^16..]);

			Span<byte> outputBytes = stackalloc byte[16];

			try
			{
				Base62.FromBase62Chars(paddedInputBytes, outputBytes);
			}
			catch (ArgumentException)
			{
				id = default;
				return false;
			}

			var signAndScale = BinaryPrimitives.ReadInt32BigEndian(outputBytes);
			var hi = BinaryPrimitives.ReadInt32BigEndian(outputBytes[4..]);
			var mid = BinaryPrimitives.ReadInt32BigEndian(outputBytes[8..]);
			var lo = BinaryPrimitives.ReadInt32BigEndian(outputBytes[12..]);

			id = new decimal(lo: lo, mid: mid, hi: hi, isNegative: false, scale: 0);

			if (signAndScale != 0 || id > MaxDecimalValue)
			{
				id = default;
				return false;
			}

			return true;
		}

		public static bool TryFromShortString(ReadOnlySpan<char> chars, out decimal id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(16, chars.Length)];
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)chars[i];

			return TryFromShortString(bytes, out id);
		}

		public static decimal? FromShortStringOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryFromShortString(bytes, out var id) ? id : (decimal?)null;
		}

		public static decimal? FromShortStringOrDefault(ReadOnlySpan<char> chars)
		{
			return TryFromShortString(chars, out var id) ? id : (decimal?)null;
		}

		public static bool TryFromString(ReadOnlySpan<byte> bytes, out decimal id)
		{
			if (bytes.Length == 16) return TryFromShortString(bytes, out id);

			Span<char> chars = stackalloc char[Math.Min(64, bytes.Length)];
			for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];

			return TryFromString(chars, out id);
		}

		public static bool TryFromString(ReadOnlySpan<char> chars, out decimal id)
		{
			// Short string encodings are exactly 16 characters
			// Decimal strings are always longer than 16 characters (since the epoch is more than a few milliseconds ago)
			// Decimal strings over 28 characters are not proper ID values
			if (chars.Length < 16 || chars.Length > 28)
			{
				id = default;
				return false;
			}

			if (chars.Length == 16) return TryFromShortString(chars, out id);

			return Decimal.TryParse(chars, NumberStyles.None, provider: null, out id);
		}

		public static decimal? FromStringOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryFromString(bytes, out var id) ? id : (decimal?)null;
		}

		public static decimal? FromStringOrDefault(ReadOnlySpan<char> chars)
		{
			return TryFromString(chars, out var id) ? id : (decimal?)null;
		}
	}
}
