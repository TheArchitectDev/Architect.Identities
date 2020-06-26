using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static partial class IdEncoder
	{
		public static bool TryGetLong(ReadOnlySpan<char> chars, out long id)
		{
			if (!TryGetUlong(chars, out var ulongId) || ulongId > Int64.MaxValue)
			{
				id = default;
				return false;
			}

			id = (long)ulongId;
			return true;
		}

		public static bool TryGetUlong(ReadOnlySpan<char> chars, out ulong id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(32, chars.Length)]; // Somewhat high maximum avoids large copies while ensuring a sufficient ceiling
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)chars[i];

			return TryGetUlong(bytes, out id);
		}

		public static bool TryGetDecimal(ReadOnlySpan<char> chars, out decimal id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(32, chars.Length)]; // Somewhat high maximum avoids large copies while ensuring a sufficient ceiling
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)chars[i];

			return TryGetDecimal(bytes, out id);
		}

		public static long? GetLongOrDefault(ReadOnlySpan<char> chars)
		{
			return TryGetLong(chars, out var id) ? id : (long?)null;
		}

		public static ulong? GetUlongOrDefault(ReadOnlySpan<char> chars)
		{
			return TryGetUlong(chars, out var id) ? id : (ulong?)null;
		}

		public static decimal? GetDecimalOrDefault(ReadOnlySpan<char> chars)
		{
			return TryGetDecimal(chars, out var id) ? id : (decimal?)null;
		}
	}
}
