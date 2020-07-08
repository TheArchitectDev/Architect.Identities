using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static partial class IdEncoder
	{
		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(long)"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of at least 11 input characters.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
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

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(ulong)"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of at least 11 input characters.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryGetUlong(ReadOnlySpan<char> chars, out ulong id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(32, chars.Length)]; // Somewhat high maximum avoids large copies while ensuring a sufficient ceiling
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)chars[i];

			return TryGetUlong(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(decimal)"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of at least 16 input characters.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryGetDecimal(ReadOnlySpan<char> chars, out decimal id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(32, chars.Length)]; // Somewhat high maximum avoids large copies while ensuring a sufficient ceiling
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)chars[i];

			return TryGetDecimal(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(Guid)"/>.
		/// </para>
		/// <para>
		/// Returns false if the input is not a value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of at least 22 input characters.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryGetGuid(ReadOnlySpan<char> chars, out Guid id)
		{
			Span<byte> bytes = stackalloc byte[Math.Min(32, chars.Length)]; // Somewhat high maximum avoids large copies while ensuring a sufficient ceiling
			for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)chars[i];

			return TryGetGuid(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(long)"/>.
		/// </para>
		/// <para>
		/// Returns null if the input is not a positive value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of at least 11 input characters.</param>
		public static long? GetLongOrDefault(ReadOnlySpan<char> chars)
		{
			return TryGetLong(chars, out var id) ? id : (long?)null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(ulong)"/>.
		/// </para>
		/// <para>
		/// Returns null if the input is not a value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of at least 11 input characters.</param>
		public static ulong? GetUlongOrDefault(ReadOnlySpan<char> chars)
		{
			return TryGetUlong(chars, out var id) ? id : (ulong?)null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(decimal)"/>.
		/// </para>
		/// <para>
		/// Returns null if the input is not a proper ID value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of at least 16 input characters.</param>
		public static decimal? GetDecimalOrDefault(ReadOnlySpan<char> chars)
		{
			return TryGetDecimal(chars, out var id) ? id : (decimal?)null;
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric string representation, effectively inverting <see cref="GetAlphanumeric(Guid)"/>.
		/// </para>
		/// <para>
		/// Returns null if the input is not a value encoded using the expected encoding.
		/// </para>
		/// </summary>
		/// <param name="chars">A sequence of at least 22 input characters.</param>
		public static Guid? GetGuidOrDefault(ReadOnlySpan<char> chars)
		{
			return TryGetGuid(chars, out var id) ? id : (Guid?)null;
		}
	}
}
