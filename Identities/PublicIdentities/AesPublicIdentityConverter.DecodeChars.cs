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
			// If the length is invalid, we will already return false and output default at the end
			// However, to keep us from allocating oversized arrays, treat oversized input as empty input
			if (chars.Length > 32) chars = default;

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
