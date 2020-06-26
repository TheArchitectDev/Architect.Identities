using System;
using System.Buffers.Binary;
using System.Globalization;
using Architect.Identities.PublicIdentities.Encodings;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	// #TODO: Test entire class (all partials)
	// #TODO: Do we really want to have one method parse numeric AND alphanumeric? Prevents us from being used for auto-increment!!!!!!!!! Which I sometimes use...
	public static partial class IdEncoder
	{
		public static bool TryGetLong(ReadOnlySpan<byte> bytes, out long id)
		{
			if (!TryGetUlong(bytes, out var ulongId) || ulongId > Int64.MaxValue)
			{
				id = default;
				return false;
			}

			id = (long)ulongId;
			return true;
		}

		public static bool TryGetUlong(ReadOnlySpan<byte> bytes, out ulong id)
		{
			// Alphanumeric encodings are exactly 11 characters long
			// Ulong strings must always be longer than 11 characters (to avoid confusion with alphanumeric ones)
			// Ulong strings over 20 characters are not ulongs
			if (bytes.Length < 11 || bytes.Length > 20)
			{
				id = default;
				return false;
			}

			// Special-case the non-alphanumeric case
			if (bytes.Length != 11)
			{
				Span<char> chars = stackalloc char[bytes.Length];
				for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];
				return UInt64.TryParse(chars, NumberStyles.None, provider: null, out id);
			}

			Span<byte> paddedInputBytes = stackalloc byte[22];
			paddedInputBytes[..^11].Fill((byte)'0'); // Fill with '0' characters
			bytes[..11].CopyTo(paddedInputBytes[^11..]);

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

			id = BinaryPrimitives.ReadUInt64BigEndian(outputBytes[^8..]);
			return true;
		}

		public static bool TryGetDecimal(ReadOnlySpan<byte> bytes, out decimal id)
		{
			// Alphanumeric encodings are exactly 16 characters long
			// Decimal strings must always be longer than 16 characters (to avoid confusion with alphanumeric ones)
			// Decimal strings longer than 28 characters are not proper ID values
			if (bytes.Length < 16 || bytes.Length > 28)
			{
				id = default;
				return false;
			}

			// Special-case the non-alphanumeric case
			if (bytes.Length != 16)
			{
				Span<char> chars = stackalloc char[bytes.Length];
				for (var i = 0; i < chars.Length; i++) chars[i] = (char)bytes[i];
				return Decimal.TryParse(chars, NumberStyles.None, provider: null, out id);
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

			if (signAndScale != 0 || id > CompanyUniqueIdGenerator.MaxValue)
			{
				id = default;
				return false;
			}

			return true;
		}

		public static long? GetLongOrDefault(ReadOnlySpan<byte> bytes)
		{ 
			return TryGetLong(bytes, out var id) ? id : (long?)null;
		}

		public static ulong? GetUlongOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryGetUlong(bytes, out var id) ? id : (ulong?)null;
		}

		public static decimal? GetDecimalOrDefault(ReadOnlySpan<byte> bytes)
		{
			return TryGetDecimal(bytes, out var id) ? id : (decimal?)null;
		}
	}
}
