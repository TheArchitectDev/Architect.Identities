using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Converts identities to public identities using AES-ECB.
	/// </para>
	/// <para>
	/// For best performance, singleton or pooled use is preferred.
	/// </para>
	/// </summary>
	internal sealed partial class AesPublicIdentityConverter : BasePublicIdentityConverter, IPublicIdentityConverter
	{
		private byte[] Key { get; }
		private Aes Aes { get; }
		private ICryptoTransform Encryptor { get; }
		private ICryptoTransform Decryptor { get; }

		#region Byte-arrays to store temporary state for ICryptoTransform parameters
		private byte[] EncryptorInputBlock { get; } = new byte[16];
		private byte[] EncryptorOutputBlock { get; } = new byte[16];
		private Span<ulong> EncryptorInputUlongSpan => MemoryMarshal.Cast<byte, ulong>(this.EncryptorInputBlock);
		private Span<decimal> EncryptorInputDecimalSpan => MemoryMarshal.Cast<byte, decimal>(this.EncryptorInputBlock);
		private Span<int> EncryptorInputDecimalComponentSpan => MemoryMarshal.Cast<byte, int>(this.EncryptorInputBlock);
		private byte[] DecryptorInputBlock { get; } = new byte[16];
		private byte[] DecryptorOutputBlock { get; } = new byte[16];
		private Span<ulong> DecryptorOutputUlongSpan => MemoryMarshal.Cast<byte, ulong>(this.DecryptorOutputBlock);
		private Span<decimal> DecryptorOutputDecimalSpan => MemoryMarshal.Cast<byte, decimal>(this.DecryptorOutputBlock);
		#endregion

		/// <summary>
		/// Converts the given long to ulong, or throws an <see cref="ArgumentOutOfRangeException"/> if it is negative.
		/// </summary>
		private static ulong LongToUlong(long id) => id >= 0 ? (ulong)id : throw new ArgumentOutOfRangeException();

		public AesPublicIdentityConverter(ReadOnlySpan<byte> aesKey)
		{
			if (aesKey.Length < 16) throw new ArgumentException("Expected at least a 128-bit key.");

			this.Key = aesKey.ToArray();
			this.Aes = new AesManaged() // Single-block crypto is faster managed than unmanaged
			{
				Key = Key,
				Mode = CipherMode.ECB,
				Padding = PaddingMode.None, // Required for correct results
				IV = new byte[16], // Not used with ECB, but set to zero anyway
			};
			this.Encryptor = this.Aes.CreateEncryptor();
			this.Decryptor = this.Aes.CreateDecryptor();
		}

		public void Dispose()
		{
			this.Encryptor.Dispose();
			this.Decryptor.Dispose();
			this.Aes.Dispose();
		}
	}
}
