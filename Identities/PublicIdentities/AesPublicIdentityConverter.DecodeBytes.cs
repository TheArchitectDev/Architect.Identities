using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed partial class AesPublicIdentityConverter
	{
		public ulong GetUlongOrDefault(ReadOnlySpan<byte> bytes) => this.TryGetUlong(bytes, out var value) ? value : default;
		public long GetLongOrDefault(ReadOnlySpan<byte> bytes) => this.TryGetLong(bytes, out var value) ? value : default;

		public bool TryGetUlong(ReadOnlySpan<byte> bytes, out ulong value)
		{
			DecodingFunction decoder;
			switch (bytes.Length)
			{
				case 32:
					decoder = LongAsciiDecoder;
					break;
				case 22:
					decoder = ShortAsciiDecoder;
					break;
				case 16:
					decoder = CopyOnlyDecoder;
					break;
				default:
					value = default;
					return false;
			}

			// Decode text to bytes
			Span<byte> decodedBytes = stackalloc byte[16];
			try
			{
				decoder(bytes, decodedBytes);
			}
			catch (ArgumentException)
			{
				value = default;
				return false;
			}

			// Decrypt
			lock (this.Decryptor)
			{
				System.Diagnostics.Debug.Assert(this.DecryptorInputBlock.Length == 16);

				decodedBytes.CopyTo(this.DecryptorInputBlock);
				this.Decryptor.TransformBlock(this.DecryptorInputBlock, 0, 16, this.DecryptorOutputBlock, 0);

				var plaintextSpan = this.DecryptorOutputUlongSpan;

				// Extract value and confirm "checksum"
				if (plaintextSpan[0] != 0UL)
				{
					value = default;
					return false;
				}
				value = plaintextSpan[1];
			}

			return true;
		}

		public bool TryGetLong(ReadOnlySpan<byte> bytes, out long value)
		{
			var didSucceed = this.TryGetUlong(bytes, out var ulongValue);
			if (ulongValue > Int64.MaxValue)
			{
				value = default;
				return false;
			}
			value = (long)ulongValue;
			return didSucceed;
		}
	}
}
