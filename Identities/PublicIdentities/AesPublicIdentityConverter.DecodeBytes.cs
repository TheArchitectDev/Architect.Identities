using System;
using System.Runtime.InteropServices;
using Architect.Identities.Helpers;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed partial class AesPublicIdentityConverter
	{
		public ulong? GetUlongOrDefault(ReadOnlySpan<byte> bytes) => this.TryGetUlong(bytes, out var value) ? value : (ulong?)null;
		public long? GetLongOrDefault(ReadOnlySpan<byte> bytes) => this.TryGetLong(bytes, out var value) ? value : (long?)null;
		public decimal? GetDecimalOrDefault(ReadOnlySpan<byte> bytes) => this.TryGetDecimal(bytes, out var value) ? value : (decimal?)null;

		/// <summary>
		/// <para>
		/// Decodes and decrypts the given public identity, writing the result into the given output span, without checking if it is valid.
		/// </para>
		/// <para>
		/// Returns true if the input was of a valid length and encoding, regardless of the whether it contained a valid ID.
		/// Returns false otherwise.
		/// </para>
		/// </summary>
		private bool TryGetIdBytes(ReadOnlySpan<byte> publicIdentityBytes, Span<byte> outputBytes)
		{
			System.Diagnostics.Debug.Assert(outputBytes.Length == 16);

			DecodingFunction decoder;
			switch (publicIdentityBytes.Length)
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
					return false;
			}

			// Decode text (or bytes) to bytes
			try
			{
				// Abuse the caller's output span to store the intermediate output
				decoder(publicIdentityBytes, outputBytes);
			}
			catch (ArgumentException)
			{
				return false;
			}

			// Decrypt
			lock (this.Decryptor)
			{
				System.Diagnostics.Debug.Assert(this.DecryptorInputBlock.Length == 16);

				outputBytes.CopyTo(this.DecryptorInputBlock);
				this.Decryptor.TransformBlock(this.DecryptorInputBlock, 0, 16, this.DecryptorOutputBlock, 0);

				this.DecryptorOutputBlock.CopyTo(outputBytes);
			}

			return true;
		}

		public bool TryGetUlong(ReadOnlySpan<byte> bytes, out ulong value)
		{
			Span<byte> idBytes = stackalloc	byte[16];
			var ulongs = MemoryMarshal.Cast<byte, ulong>(idBytes);

			// If encoding or "checksum" is invalid, return false
			if (!TryGetIdBytes(bytes, idBytes) || ulongs[0] != 0UL)
			{
				value = default;
				return false;
			}

			value = ulongs[1];
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

		public bool TryGetDecimal(ReadOnlySpan<byte> bytes, out decimal value)
		{
			Span<byte> idBytes = stackalloc byte[16];
			var decimals = MemoryMarshal.Cast<byte, decimal>(idBytes);
			var ints = MemoryMarshal.Cast<byte, int>(idBytes);

			// If encoding or "checksum" is invalid, return false
			// First 32 bits: sign and scale
			if (!TryGetIdBytes(bytes, idBytes) || DecimalStructure.GetSignAndScale(ints) != 0 || (value = decimals[0]) > CompanyUniqueIdEncoder.MaxDecimalValue)
			{
				value = default;
				return false;
			}

			return true;
		}
	}
}
