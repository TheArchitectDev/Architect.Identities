using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed partial class AesPublicIdentityConverter
	{
		public ulong GetUlongOrDefault(ReadOnlySpan<char> chars) => this.TryGetUlong(chars, out var value) ? value : default;
		public long GetLongOrDefault(ReadOnlySpan<char> chars) => this.TryGetLong(chars, out var value) ? value : default;

		public bool TryGetUlong(ReadOnlySpan<char> chars, out ulong value)
		{
			// Allow only valid textual input lengths
			// This also protects us against allocating oversized arrays
			if (chars.Length != 32 && chars.Length != 22)
			{
				value = default;
				return false;
			}

			System.Diagnostics.Debug.Assert(chars.Length <= 32, "The above protection against allocating oversized arrays was removed.");

			// Convert the chars to bytes, into a temporary byte[]
			Span<byte> bytes = stackalloc byte[chars.Length];
			var i = 0;
			foreach (var chr in chars) bytes[i++] = (byte)chr;

			return this.TryGetUlong(bytes, out value);
		}

		public bool TryGetLong(ReadOnlySpan<char> chars, out long value)
		{
			var didSucceed = this.TryGetUlong(chars, out var ulongValue);
			if (ulongValue > Int64.MaxValue) didSucceed = false;
			value = (long)ulongValue;
			return didSucceed;
		}
	}
}
