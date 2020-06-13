using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed partial class AesPublicIdentityConverter
	{
		public ulong? GetUlongOrDefault(ReadOnlySpan<char> chars) => this.TryGetUlong(chars, out var value) ? value : (ulong?)null;
		public long? GetLongOrDefault(ReadOnlySpan<char> chars) => this.TryGetLong(chars, out var value) ? value : (long?)null;
		public decimal? GetDecimalOrDefault(ReadOnlySpan<char> chars) => this.TryGetDecimal(chars, out var value) ? value : (decimal?)null;

		/// <summary>
		/// Converts the given chars to bytes.
		/// Does not validate the input length, since that is done further down the chain.
		/// </summary>
		/// <param name="chars">The characters to convert to bytes. Characters beyond the 33rd are ignored.</param>
		/// <param name="bytes33">The output span, by ref. Should be 33 bytes long. Will be sliced down to the input span size.</param>
		private void GetBytes(ReadOnlySpan<char> chars, ref Span<byte> bytes33)
		{
			System.Diagnostics.Debug.Assert(bytes33.Length == 33);

			// Ignore (and have the caller ignore) unused bytes
			bytes33 = bytes33.Slice(0, Math.Min(bytes33.Length, chars.Length));

			// Convert the chars to bytes
			for (var i = 0; i < bytes33.Length; i++)
				bytes33[i] = (byte)chars[i];
		}

		public bool TryGetUlong(ReadOnlySpan<char> chars, out ulong value)
		{
			Span<byte> bytes = stackalloc byte[33];
			GetBytes(chars, ref bytes);
			return this.TryGetUlong(bytes, out value);
		}

		public bool TryGetLong(ReadOnlySpan<char> chars, out long value)
		{
			Span<byte> bytes = stackalloc byte[33];
			GetBytes(chars, ref bytes);
			return this.TryGetLong(bytes, out value);
		}

		public bool TryGetDecimal(ReadOnlySpan<char> chars, out decimal value)
		{
			Span<byte> bytes = stackalloc byte[33];
			GetBytes(chars, ref bytes);
			return this.TryGetDecimal(bytes, out value);
		}
	}
}
